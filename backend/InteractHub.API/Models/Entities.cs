using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.Models
{
    public enum PostVisibility { Public, Friends, Private }

    public class Post
    {
        public int Id { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }

        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }

        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // FK
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? SharedPostId { get; set; }
        public Post? SharedPost { get; set; }

        // Navigation
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Like> Likes { get; set; } = [];
        public ICollection<PostHashtag> PostHashtags { get; set; } = [];
    }

    public class Comment
    {
        public int Id { get; set; }

        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = [];
    }

    public class Like
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }

    public class Follower
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int FollowerId { get; set; }
        public User FollowerUser { get; set; } = null!;

        public int FollowingId { get; set; }
        public User FollowingUser { get; set; } = null!;
    }

    public class Hashtag
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<PostHashtag> PostHashtags { get; set; } = [];
    }

    public class PostHashtag
    {
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int HashtagId { get; set; }
        public Hashtag Hashtag { get; set; } = null!;
    }

    public enum NotificationType
    {
        Like, Comment, Follow, Share, Mention
    }

    public class Notification
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? ActorId { get; set; }
        public User? Actor { get; set; }

        public int? PostId { get; set; }
        public Post? Post { get; set; }
    }

    public class Story
    {
        public int Id { get; set; }
        public string MediaUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
        public bool IsActive => DateTime.UtcNow < ExpiresAt;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<StoryView> Views { get; set; } = [];
    }

    public class StoryView
    {
        public int Id { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public int StoryId { get; set; }
        public Story Story { get; set; } = null!;

        public int ViewerId { get; set; }
        public User Viewer { get; set; } = null!;
    }
}
