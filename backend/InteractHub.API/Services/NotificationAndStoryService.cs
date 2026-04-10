using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Hubs;
using InteractHub.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services
{
    public interface INotificationService
    {
        Task CreateAsync(int userId, int actorId, NotificationType type, string message, int? postId = null);
        Task<PagedResult<NotificationDto>> GetNotificationsAsync(int userId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int userId, int? notificationId = null);
    }

    public class NotificationService(AppDbContext db, IHubContext<NotificationHub> hub) : INotificationService
    {
        private readonly AppDbContext _db = db;
        private readonly IHubContext<NotificationHub> _hub = hub;

        public async Task CreateAsync(int userId, int actorId, NotificationType type, string message, int? postId = null)
        {
            var notif = new Notification
            {
                UserId = userId,
                ActorId = actorId,
                Type = type,
                Message = message,
                PostId = postId
            };

            _db.Notifications.Add(notif);
            await _db.SaveChangesAsync();

            // Push via SignalR
            await _hub.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", new NotificationDto
                {
                    Id = notif.Id,
                    Type = type.ToString(),
                    Message = message,
                    IsRead = false,
                    CreatedAt = notif.CreatedAt,
                    PostId = postId
                });
        }

        public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(int userId, int page, int pageSize)
        {
            var query = _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(n => n.Actor)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type.ToString(),
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    PostId = n.PostId,
                    Actor = n.Actor != null
                        ? new UserSummaryDto(n.Actor.Id, n.Actor.Username, n.Actor.FullName, n.Actor.AvatarUrl)
                        : null
                })
                .ToListAsync();

            return new PagedResult<NotificationDto>(items, total, page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
            => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkAsReadAsync(int userId, int? notificationId = null)
        {
            var query = _db.Notifications.Where(n => n.UserId == userId && !n.IsRead);
            if (notificationId.HasValue)
                query = query.Where(n => n.Id == notificationId);

            await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
    }

    public interface IStoryService
    {
        Task<StoryDto> CreateStoryAsync(int userId, CreateStoryDto dto);
        Task<List<StoryDto>> GetActiveStoriesAsync(int userId);
        Task<bool> ViewStoryAsync(int viewerId, int storyId);
        Task<bool> DeleteStoryAsync(int userId, int storyId);
    }

    public class StoryService(AppDbContext db) : IStoryService
    {
        private readonly AppDbContext _db = db;

        public async Task<StoryDto> CreateStoryAsync(int userId, CreateStoryDto dto)
        {
            var story = new Story
            {
                UserId = userId,
                MediaUrl = dto.MediaUrl,
                Caption = dto.Caption
            };

            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            return await GetStoryDtoAsync(story.Id, userId);
        }

        public async Task<List<StoryDto>> GetActiveStoriesAsync(int userId)
        {
            var followingIds = await _db.Followers
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            followingIds.Add(userId);

            var stories = await _db.Stories
                .Where(s => followingIds.Contains(s.UserId) && s.ExpiresAt > DateTime.UtcNow)
                .Include(s => s.User)
                .Include(s => s.Views)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return stories.Select(s => MapToDto(s, userId)).ToList();
        }

        public async Task<bool> ViewStoryAsync(int viewerId, int storyId)
        {
            if (await _db.StoryViews.AnyAsync(sv => sv.StoryId == storyId && sv.ViewerId == viewerId))
                return false;

            _db.StoryViews.Add(new StoryView { StoryId = storyId, ViewerId = viewerId });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStoryAsync(int userId, int storyId)
        {
            var story = await _db.Stories.FirstOrDefaultAsync(s => s.Id == storyId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Story not found.");

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<StoryDto> GetStoryDtoAsync(int storyId, int viewerId)
        {
            var story = await _db.Stories
                .Include(s => s.User).Include(s => s.Views)
                .FirstAsync(s => s.Id == storyId);
            return MapToDto(story, viewerId);
        }

        private static StoryDto MapToDto(Story s, int viewerId) => new()
        {
            Id = s.Id,
            MediaUrl = s.MediaUrl,
            Caption = s.Caption,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            Author = new UserSummaryDto(s.User.Id, s.User.Username, s.User.FullName, s.User.AvatarUrl),
            ViewsCount = s.Views?.Count ?? 0,
            IsViewed = s.Views?.Any(v => v.ViewerId == viewerId) ?? false
        };
    }
}
