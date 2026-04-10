using FluentAssertions;
using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Hubs;
using InteractHub.API.Models;
using InteractHub.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InteractHub.Tests;

public class PostServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IPostService _postService;
    private readonly Mock<INotificationService> _notifMock;

    public PostServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _notifMock = new Mock<INotificationService>();
        _postService = new PostService(_db, _notifMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        _db.Users.AddRange(
            new User { Id = 1, Username = "alice", Email = "alice@test.com", PasswordHash = "hash" },
            new User { Id = 2, Username = "bob",   Email = "bob@test.com",   PasswordHash = "hash" }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreatePost_WithContent_ReturnsPost()
    {
        // Arrange
        var dto = new CreatePostDto("Hello World!", null, null);

        // Act
        var result = await _postService.CreatePostAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello World!");
        result.Author.Id.Should().Be(1);
        result.Author.Username.Should().Be("alice");
    }

    [Fact]
    public async Task CreatePost_WithNoContentOrMedia_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreatePostDto(null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _postService.CreatePostAsync(1, dto));
    }

    [Fact]
    public async Task CreatePost_WithHashtags_StoresHashtags()
    {
        // Arrange
        var dto = new CreatePostDto("Post with hashtags", null, null, "Public", ["dotnet", "csharp"]);

        // Act
        var result = await _postService.CreatePostAsync(1, dto);

        // Assert
        result.Hashtags.Should().Contain("dotnet");
        result.Hashtags.Should().Contain("csharp");
    }

    [Fact]
    public async Task LikePost_FirstTime_ReturnsTrue()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(2, new CreatePostDto("Bob's post", null, null));

        // Act
        var result = await _postService.LikePostAsync(1, post.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LikePost_AlreadyLiked_ReturnsFalse()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(2, new CreatePostDto("Bob's post", null, null));
        await _postService.LikePostAsync(1, post.Id);

        // Act
        var result = await _postService.LikePostAsync(1, post.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnlikePost_WhenLiked_ReturnsTrue()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(2, new CreatePostDto("Post", null, null));
        await _postService.LikePostAsync(1, post.Id);

        // Act
        var result = await _postService.UnlikePostAsync(1, post.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetFeed_ReturnsPostsFromFollowingAndSelf()
    {
        // Arrange - alice follows bob
        _db.Followers.Add(new Follower { FollowerId = 1, FollowingId = 2 });
        await _db.SaveChangesAsync();

        await _postService.CreatePostAsync(1, new CreatePostDto("Alice post", null, null));
        await _postService.CreatePostAsync(2, new CreatePostDto("Bob post", null, null));

        // Act
        var feed = await _postService.GetFeedAsync(1, 1, 10);

        // Assert
        feed.Items.Should().HaveCount(2);
        feed.Items.Select(p => p.Author.Username).Should().Contain(["alice", "bob"]);
    }

    [Fact]
    public async Task DeletePost_ByOwner_SoftDeletesPost()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(1, new CreatePostDto("To delete", null, null));

        // Act
        var result = await _postService.DeletePostAsync(1, post.Id);

        // Assert
        result.Should().BeTrue();
        var dbPost = await _db.Posts.FindAsync(post.Id);
        dbPost!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePost_ByNonOwner_ThrowsKeyNotFoundException()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(1, new CreatePostDto("Alice's post", null, null));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _postService.DeletePostAsync(2, post.Id));
    }

    [Fact]
    public async Task AddComment_ToExistingPost_ReturnsComment()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(1, new CreatePostDto("Post with comment", null, null));

        // Act
        var comment = await _postService.AddCommentAsync(2, post.Id, new CreateCommentDto("Great post!"));

        // Assert
        comment.Should().NotBeNull();
        comment.Content.Should().Be("Great post!");
        comment.Author.Username.Should().Be("bob");
    }

    [Fact]
    public async Task GetTrendingHashtags_ReturnsCorrectOrder()
    {
        // Arrange
        await _postService.CreatePostAsync(1, new CreatePostDto("p1", null, null, "Public", ["hot", "tech"]));
        await _postService.CreatePostAsync(1, new CreatePostDto("p2", null, null, "Public", ["hot"]));
        await _postService.CreatePostAsync(2, new CreatePostDto("p3", null, null, "Public", ["cool"]));

        // Act
        var trending = await _postService.GetTrendingHashtagsAsync(10);

        // Assert
        trending.First().Name.Should().Be("hot");
        trending.First().PostCount.Should().Be(2);
    }

    [Fact]
    public async Task LikePost_SendsNotificationToPostOwner()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(2, new CreatePostDto("Bob's post", null, null));

        // Act
        await _postService.LikePostAsync(1, post.Id);

        // Assert - notification sent to post owner (bob = user 2), by actor (alice = user 1)
        _notifMock.Verify(n => n.CreateAsync(2, 1, NotificationType.Like, It.IsAny<string>(), post.Id), Times.Once);
    }

    public void Dispose() => _db.Dispose();
}
