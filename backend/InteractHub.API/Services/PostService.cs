using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services
{
    public interface IPostService
    {
        Task<PagedResult<PostDto>> GetFeedAsync(int userId, int page, int pageSize);
        Task<PagedResult<PostDto>> GetUserPostsAsync(int userId, int page, int pageSize, int? currentUserId = null);
        Task<PostDto> GetPostAsync(int postId, int? currentUserId = null);
        Task<PostDto> CreatePostAsync(int userId, CreatePostDto dto);
        Task<PostDto> UpdatePostAsync(int userId, int postId, UpdatePostDto dto);
        Task<bool> DeletePostAsync(int userId, int postId);
        Task<bool> LikePostAsync(int userId, int postId);
        Task<bool> UnlikePostAsync(int userId, int postId);
        Task<PostDto> SharePostAsync(int userId, int postId, string? content);
        Task<CommentDto> AddCommentAsync(int userId, int postId, CreateCommentDto dto);
        Task<bool> DeleteCommentAsync(int userId, int commentId);
        Task<List<CommentDto>> GetCommentsAsync(int postId, int? currentUserId = null);
        Task<PagedResult<PostDto>> SearchPostsAsync(string query, int page, int pageSize, int? currentUserId = null);
        Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(int count = 10);
        Task<PagedResult<PostDto>> GetHashtagPostsAsync(string tag, int page, int pageSize, int? currentUserId = null);
    }

    public class PostService(AppDbContext db, INotificationService notifService) : IPostService
    {
        private readonly AppDbContext _db = db;
        private readonly INotificationService _notifService = notifService;

        public async Task<PagedResult<PostDto>> GetFeedAsync(int userId, int page, int pageSize)
        {
            var followingIds = await _db.Followers
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            followingIds.Add(userId);

            var query = _db.Posts
                .Where(p => !p.IsDeleted && followingIds.Contains(p.UserId))
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var posts = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
                .Include(p => p.SharedPost).ThenInclude(sp => sp!.User)
                .ToListAsync();

            var likedPostIds = await _db.Likes
                .Where(l => l.UserId == userId && posts.Select(p => p.Id).Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync();

            return new PagedResult<PostDto>(
                posts.Select(p => MapToDto(p, likedPostIds.Contains(p.Id))).ToList(),
                total, page, pageSize);
        }

        public async Task<PagedResult<PostDto>> GetUserPostsAsync(int userId, int page, int pageSize, int? currentUserId = null)
        {
            var query = _db.Posts
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var posts = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
                .Include(p => p.SharedPost).ThenInclude(sp => sp!.User)
                .ToListAsync();

            var likedPostIds = currentUserId.HasValue
                ? await _db.Likes.Where(l => l.UserId == currentUserId && posts.Select(p => p.Id).Contains(l.PostId))
                    .Select(l => l.PostId).ToListAsync()
                : [];

            return new PagedResult<PostDto>(
                posts.Select(p => MapToDto(p, likedPostIds.Contains(p.Id))).ToList(),
                total, page, pageSize);
        }

        public async Task<PostDto> GetPostAsync(int postId, int? currentUserId = null)
        {
            var post = await _db.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
                .Include(p => p.SharedPost).ThenInclude(sp => sp!.User)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted)
                ?? throw new KeyNotFoundException("Post not found.");

            bool isLiked = currentUserId.HasValue &&
                await _db.Likes.AnyAsync(l => l.UserId == currentUserId && l.PostId == postId);

            return MapToDto(post, isLiked);
        }

        public async Task<PostDto> CreatePostAsync(int userId, CreatePostDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.ImageUrl)
                && string.IsNullOrWhiteSpace(dto.VideoUrl))
                throw new InvalidOperationException("Post must have content, image, or video.");

            var post = new Post
            {
                UserId = userId,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                VideoUrl = dto.VideoUrl,
                Visibility = Enum.Parse<PostVisibility>(dto.Visibility, true)
            };

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            // Hashtags
            if (dto.Hashtags?.Count > 0)
                await AddHashtagsAsync(post.Id, dto.Hashtags);

            return await GetPostAsync(post.Id, userId);
        }

        public async Task<PostDto> UpdatePostAsync(int userId, int postId, UpdatePostDto dto)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId && !p.IsDeleted)
                ?? throw new KeyNotFoundException("Post not found or unauthorized.");

            if (dto.Content != null) post.Content = dto.Content;
            if (dto.Visibility != null) post.Visibility = Enum.Parse<PostVisibility>(dto.Visibility, true);
            post.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return await GetPostAsync(postId, userId);
        }

        public async Task<bool> DeletePostAsync(int userId, int postId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId && !p.IsDeleted)
                ?? throw new KeyNotFoundException("Post not found or unauthorized.");

            post.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LikePostAsync(int userId, int postId)
        {
            if (await _db.Likes.AnyAsync(l => l.UserId == userId && l.PostId == postId))
                return false;

            var post = await _db.Posts.FindAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            _db.Likes.Add(new Like { UserId = userId, PostId = postId });
            await _db.SaveChangesAsync();

            // Notify post owner
            if (post.UserId != userId)
                await _notifService.CreateAsync(post.UserId, userId, NotificationType.Like,
                    "liked your post", postId);

            return true;
        }

        public async Task<bool> UnlikePostAsync(int userId, int postId)
        {
            var like = await _db.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
            if (like == null) return false;

            _db.Likes.Remove(like);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PostDto> SharePostAsync(int userId, int postId, string? content)
        {
            var original = await _db.Posts.FindAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            var post = new Post
            {
                UserId = userId,
                Content = content,
                SharedPostId = postId,
                Visibility = PostVisibility.Public
            };

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            if (original.UserId != userId)
                await _notifService.CreateAsync(original.UserId, userId, NotificationType.Share,
                    "shared your post", postId);

            return await GetPostAsync(post.Id, userId);
        }

        public async Task<CommentDto> AddCommentAsync(int userId, int postId, CreateCommentDto dto)
        {
            var post = await _db.Posts.FindAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = dto.Content,
                ParentCommentId = dto.ParentCommentId
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            if (post.UserId != userId)
                await _notifService.CreateAsync(post.UserId, userId, NotificationType.Comment,
                    "commented on your post", postId);

            var loaded = await _db.Comments.Include(c => c.User).FirstAsync(c => c.Id == comment.Id);
            return MapCommentToDto(loaded);
        }

        public async Task<bool> DeleteCommentAsync(int userId, int commentId)
        {
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId && !c.IsDeleted)
                ?? throw new KeyNotFoundException("Comment not found or unauthorized.");

            comment.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<CommentDto>> GetCommentsAsync(int postId, int? currentUserId = null)
        {
            var comments = await _db.Comments
                .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => !r.IsDeleted)).ThenInclude(r => r.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(MapCommentToDto).ToList();
        }

        public async Task<PagedResult<PostDto>> SearchPostsAsync(string query, int page, int pageSize, int? currentUserId = null)
        {
            var q = _db.Posts.Where(p => !p.IsDeleted && p.Visibility == PostVisibility.Public
                && p.Content != null && p.Content.Contains(query))
                .OrderByDescending(p => p.CreatedAt);

            var total = await q.CountAsync();
            var posts = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
                .Include(p => p.SharedPost).ThenInclude(sp => sp!.User)
                .ToListAsync();

            var likedIds = currentUserId.HasValue
                ? await _db.Likes.Where(l => l.UserId == currentUserId && posts.Select(p => p.Id).Contains(l.PostId))
                    .Select(l => l.PostId).ToListAsync()
                : [];

            return new PagedResult<PostDto>(
                posts.Select(p => MapToDto(p, likedIds.Contains(p.Id))).ToList(),
                total, page, pageSize);
        }

        public async Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(int count = 10)
        {
            var data = await _db.PostHashtags
                .Include(ph => ph.Hashtag)
                .ToListAsync();

            return data
                .GroupBy(ph => ph.Hashtag.Name)
                .Select(g => new TrendingHashtagDto(g.Key, g.Count()))
                .OrderByDescending(h => h.PostCount)
                .Take(count)
                .ToList();
        }

        public async Task<PagedResult<PostDto>> GetHashtagPostsAsync(string tag, int page, int pageSize, int? currentUserId = null)
        {
            var q = _db.PostHashtags
                .Where(ph => ph.Hashtag.Name == tag && !ph.Post.IsDeleted)
                .Select(ph => ph.Post)
                .OrderByDescending(p => p.CreatedAt);

            var total = await q.CountAsync();
            var posts = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
                .Include(p => p.SharedPost).ThenInclude(sp => sp!.User)
                .ToListAsync();

            var likedIds = currentUserId.HasValue
                ? await _db.Likes.Where(l => l.UserId == currentUserId && posts.Select(p => p.Id).Contains(l.PostId))
                    .Select(l => l.PostId).ToListAsync()
                : [];

            return new PagedResult<PostDto>(
                posts.Select(p => MapToDto(p, likedIds.Contains(p.Id))).ToList(),
                total, page, pageSize);
        }

        private async Task AddHashtagsAsync(int postId, List<string> tags)
        {
            foreach (var tag in tags.Select(t => t.ToLower().TrimStart('#')).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var hashtag = await _db.Hashtags.FirstOrDefaultAsync(h => h.Name == tag);
                if (hashtag == null)
                {
                    hashtag = new Hashtag { Name = tag };
                    _db.Hashtags.Add(hashtag);
                    await _db.SaveChangesAsync();
                }
                if (!await _db.PostHashtags.AnyAsync(ph => ph.PostId == postId && ph.HashtagId == hashtag.Id))
                    _db.PostHashtags.Add(new PostHashtag { PostId = postId, HashtagId = hashtag.Id });
            }
            await _db.SaveChangesAsync();
        }

        private static PostDto MapToDto(Post p, bool isLiked) => new()
        {
            Id = p.Id,
            Content = p.Content,
            ImageUrl = p.ImageUrl,
            VideoUrl = p.VideoUrl,
            Visibility = p.Visibility.ToString(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Author = new UserSummaryDto(p.User.Id, p.User.Username, p.User.FullName, p.User.AvatarUrl),
            LikesCount = p.Likes?.Count ?? 0,
            CommentsCount = p.Comments?.Count(c => !c.IsDeleted) ?? 0,
            IsLiked = isLiked,
            Hashtags = p.PostHashtags?.Select(ph => ph.Hashtag.Name).ToList() ?? [],
            SharedPost = p.SharedPost != null ? new PostDto
            {
                Id = p.SharedPost.Id,
                Content = p.SharedPost.Content,
                ImageUrl = p.SharedPost.ImageUrl,
                CreatedAt = p.SharedPost.CreatedAt,
                Author = new UserSummaryDto(p.SharedPost.User.Id, p.SharedPost.User.Username,
                    p.SharedPost.User.FullName, p.SharedPost.User.AvatarUrl),
                Visibility = p.SharedPost.Visibility.ToString()
            } : null
        };

        private static CommentDto MapCommentToDto(Comment c) => new()
        {
            Id = c.Id,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            Author = new UserSummaryDto(c.User.Id, c.User.Username, c.User.FullName, c.User.AvatarUrl),
            ParentCommentId = c.ParentCommentId,
            Replies = c.Replies?.Where(r => !r.IsDeleted).Select(MapCommentToDto).ToList() ?? []
        };
    }
}
