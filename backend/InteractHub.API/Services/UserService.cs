using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services
{
    public interface IUserService
    {
        Task<UserDto> GetProfileAsync(int userId, int? currentUserId = null);
        Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<PagedResult<UserDto>> GetSuggestionsAsync(int currentUserId, int page, int pageSize);
        Task<bool> FollowAsync(int followerId, int followingId);
        Task<bool> UnfollowAsync(int followerId, int followingId);
        Task<PagedResult<UserDto>> GetFollowersAsync(int userId, int page, int pageSize, int? currentUserId = null);
        Task<PagedResult<UserDto>> GetFollowingAsync(int userId, int page, int pageSize, int? currentUserId = null);
        Task<PagedResult<UserDto>> SearchUsersAsync(string query, int page, int pageSize, int? currentUserId = null);
    }

    public class UserService(AppDbContext db) : IUserService
    {
        private readonly AppDbContext _db = db;

        public async Task<UserDto> GetProfileAsync(int userId, int? currentUserId = null)
        {
            var user = await _db.Users
                .Include(u => u.Posts)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive)
                ?? throw new KeyNotFoundException("User not found.");

            bool isFollowing = currentUserId.HasValue &&
                await _db.Followers.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

            return MapToDto(user, isFollowing);
        }

        public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (dto.FullName != null) user.FullName = dto.FullName;
            if (dto.Bio != null) user.Bio = dto.Bio;
            if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;
            if (dto.CoverUrl != null) user.CoverUrl = dto.CoverUrl;

            await _db.SaveChangesAsync();
            return await GetProfileAsync(userId, userId);
        }

        public async Task<PagedResult<UserDto>> GetSuggestionsAsync(int currentUserId, int page, int pageSize)
        {
            var followingIds = await _db.Followers
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            followingIds.Add(currentUserId);

            var query = _db.Users
                .Where(u => !followingIds.Contains(u.Id) && u.IsActive)
                .OrderByDescending(u => u.Followers.Count);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .Include(u => u.Posts)
                .Select(u => MapToDto(u, false))
                .ToListAsync();

            return new PagedResult<UserDto>(items, total, page, pageSize);
        }

        public async Task<bool> FollowAsync(int followerId, int followingId)
        {
            if (followerId == followingId) return false;
            if (await _db.Followers.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId))
                return false;

            _db.Followers.Add(new Follower { FollowerId = followerId, FollowingId = followingId });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowAsync(int followerId, int followingId)
        {
            var follow = await _db.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (follow == null) return false;
            _db.Followers.Remove(follow);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<UserDto>> GetFollowersAsync(int userId, int page, int pageSize, int? currentUserId = null)
        {
            var followerIds = await _db.Followers
                .Where(f => f.FollowingId == userId)
                .Select(f => f.FollowerId)
                .ToListAsync();

            var total = followerIds.Count;
            var items = await _db.Users
                .Where(u => followerIds.Contains(u.Id))
                .Include(u => u.Followers).Include(u => u.Following).Include(u => u.Posts)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var followingIds = currentUserId.HasValue
                ? await _db.Followers.Where(f => f.FollowerId == currentUserId).Select(f => f.FollowingId).ToListAsync()
                : [];

            return new PagedResult<UserDto>(
                items.Select(u => MapToDto(u, followingIds.Contains(u.Id))).ToList(),
                total, page, pageSize);
        }

        public async Task<PagedResult<UserDto>> GetFollowingAsync(int userId, int page, int pageSize, int? currentUserId = null)
        {
            var followingIds2 = await _db.Followers
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            var total = followingIds2.Count;
            var items = await _db.Users
                .Where(u => followingIds2.Contains(u.Id))
                .Include(u => u.Followers).Include(u => u.Following).Include(u => u.Posts)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var currentFollowingIds = currentUserId.HasValue
                ? await _db.Followers.Where(f => f.FollowerId == currentUserId).Select(f => f.FollowingId).ToListAsync()
                : [];

            return new PagedResult<UserDto>(
                items.Select(u => MapToDto(u, currentFollowingIds.Contains(u.Id))).ToList(),
                total, page, pageSize);
        }

        public async Task<PagedResult<UserDto>> SearchUsersAsync(string query, int page, int pageSize, int? currentUserId = null)
        {
            var q = _db.Users.Where(u => u.IsActive &&
                (u.Username.Contains(query) || (u.FullName != null && u.FullName.Contains(query))));

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(u => u.Followers).Include(u => u.Following).Include(u => u.Posts)
                .ToListAsync();

            var followingIds = currentUserId.HasValue
                ? await _db.Followers.Where(f => f.FollowerId == currentUserId).Select(f => f.FollowingId).ToListAsync()
                : [];

            return new PagedResult<UserDto>(
                items.Select(u => MapToDto(u, followingIds.Contains(u.Id))).ToList(),
                total, page, pageSize);
        }

        private static UserDto MapToDto(User u, bool isFollowing) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            Bio = u.Bio,
            AvatarUrl = u.AvatarUrl,
            CoverUrl = u.CoverUrl,
            CreatedAt = u.CreatedAt,
            FollowersCount = u.Followers?.Count ?? 0,
            FollowingCount = u.Following?.Count ?? 0,
            PostsCount = u.Posts?.Count(p => !p.IsDeleted) ?? 0,
            IsFollowing = isFollowing
        };
    }
}
