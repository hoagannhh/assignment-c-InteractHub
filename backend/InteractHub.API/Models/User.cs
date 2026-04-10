using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(300)]
        public string? Bio { get; set; }

        public string? AvatarUrl { get; set; }
        public string? CoverUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeenAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Post> Posts { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Like> Likes { get; set; } = [];
        public ICollection<Follower> Followers { get; set; } = [];
        public ICollection<Follower> Following { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
        public ICollection<Story> Stories { get; set; } = [];
    }
}
