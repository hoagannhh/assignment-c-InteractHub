namespace InteractHub.API.DTOs
{
    // Auth
    public record RegisterDto(string Username, string Email, string Password, string? FullName);
    public record LoginDto(string Email, string Password);
    public record AuthResponseDto(string Token, string RefreshToken, UserDto User);

    // User
    public record UserDto
    {
        public int Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? FullName { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public string? CoverUrl { get; init; }
        public DateTime CreatedAt { get; init; }
        public int FollowersCount { get; init; }
        public int FollowingCount { get; init; }
        public int PostsCount { get; init; }
        public bool IsFollowing { get; init; }
    }

    public record UpdateProfileDto(string? FullName, string? Bio, string? AvatarUrl, string? CoverUrl);

    // Post
    public record CreatePostDto(string? Content, string? ImageUrl, string? VideoUrl, string Visibility = "Public", List<string>? Hashtags = null);
    public record UpdatePostDto(string? Content, string? Visibility);

    public record PostDto
    {
        public int Id { get; init; }
        public string? Content { get; init; }
        public string? ImageUrl { get; init; }
        public string? VideoUrl { get; init; }
        public string Visibility { get; init; } = "Public";
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public UserSummaryDto Author { get; init; } = null!;
        public int LikesCount { get; init; }
        public int CommentsCount { get; init; }
        public bool IsLiked { get; init; }
        public List<string> Hashtags { get; init; } = [];
        public PostDto? SharedPost { get; init; }
    }

    public record UserSummaryDto(int Id, string Username, string? FullName, string? AvatarUrl);

    // Comment
    public record CreateCommentDto(string Content, int? ParentCommentId = null);
    public record CommentDto
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public UserSummaryDto Author { get; init; } = null!;
        public int? ParentCommentId { get; init; }
        public List<CommentDto> Replies { get; init; } = [];
    }

    // Story
    public record CreateStoryDto(string MediaUrl, string? Caption);
    public record StoryDto
    {
        public int Id { get; init; }
        public string MediaUrl { get; init; } = string.Empty;
        public string? Caption { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public UserSummaryDto Author { get; init; } = null!;
        public int ViewsCount { get; init; }
        public bool IsViewed { get; init; }
    }

    // Notification
    public record NotificationDto
    {
        public int Id { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public bool IsRead { get; init; }
        public DateTime CreatedAt { get; init; }
        public UserSummaryDto? Actor { get; init; }
        public int? PostId { get; init; }
    }

    // Pagination
    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNext => Page < TotalPages;
        public bool HasPrev => Page > 1;
    }

    // Trending
    public record TrendingHashtagDto(string Name, int PostCount);
}
