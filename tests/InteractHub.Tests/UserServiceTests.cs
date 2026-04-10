using FluentAssertions;
using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Services;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests;

public class UserServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IUserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _userService = new UserService(_db);
        SeedData();
    }

    private void SeedData()
    {
        _db.Users.AddRange(
            new User { Id = 1, Username = "alice", Email = "alice@test.com", PasswordHash = "h", FullName = "Alice A" },
            new User { Id = 2, Username = "bob",   Email = "bob@test.com",   PasswordHash = "h", FullName = "Bob B" },
            new User { Id = 3, Username = "carol", Email = "carol@test.com", PasswordHash = "h" }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetProfile_ExistingUser_ReturnsCorrectProfile()
    {
        var profile = await _userService.GetProfileAsync(1);

        profile.Should().NotBeNull();
        profile.Username.Should().Be("alice");
        profile.FullName.Should().Be("Alice A");
    }

    [Fact]
    public async Task GetProfile_NonExistentUser_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.GetProfileAsync(999));
    }

    [Fact]
    public async Task Follow_ValidUsers_ReturnsTrue()
    {
        var result = await _userService.FollowAsync(1, 2);

        result.Should().BeTrue();
        var follower = await _db.Followers.FirstOrDefaultAsync(f => f.FollowerId == 1 && f.FollowingId == 2);
        follower.Should().NotBeNull();
    }

    [Fact]
    public async Task Follow_AlreadyFollowing_ReturnsFalse()
    {
        await _userService.FollowAsync(1, 2);

        var result = await _userService.FollowAsync(1, 2);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Follow_SameUser_ReturnsFalse()
    {
        var result = await _userService.FollowAsync(1, 1);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Unfollow_ExistingFollow_ReturnsTrue()
    {
        await _userService.FollowAsync(1, 2);

        var result = await _userService.UnfollowAsync(1, 2);

        result.Should().BeTrue();
        var follower = await _db.Followers.FirstOrDefaultAsync(f => f.FollowerId == 1 && f.FollowingId == 2);
        follower.Should().BeNull();
    }

    [Fact]
    public async Task Unfollow_NotFollowing_ReturnsFalse()
    {
        var result = await _userService.UnfollowAsync(1, 3);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetProfile_ShowsCorrectFollowerCount()
    {
        await _userService.FollowAsync(2, 1);
        await _userService.FollowAsync(3, 1);

        var profile = await _userService.GetProfileAsync(1);

        profile.FollowersCount.Should().Be(2);
    }

    [Fact]
    public async Task GetProfile_WithCurrentUser_ShowsIsFollowing()
    {
        await _userService.FollowAsync(1, 2);

        var profile = await _userService.GetProfileAsync(2, currentUserId: 1);

        profile.IsFollowing.Should().BeTrue();
    }

    [Fact]
    public async Task GetSuggestions_ExcludesAlreadyFollowing()
    {
        await _userService.FollowAsync(1, 2);

        var suggestions = await _userService.GetSuggestionsAsync(1, 1, 10);

        suggestions.Items.Should().NotContain(u => u.Id == 2);
        suggestions.Items.Should().NotContain(u => u.Id == 1); // exclude self
    }

    [Fact]
    public async Task SearchUsers_ByUsername_ReturnsMatchingUsers()
    {
        var result = await _userService.SearchUsersAsync("bob", 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Username.Should().Be("bob");
    }

    [Fact]
    public async Task UpdateProfile_ChangesFieldsCorrectly()
    {
        var dto = new UpdateProfileDto("Updated Name", "New bio", null, null);

        var result = await _userService.UpdateProfileAsync(1, dto);

        result.FullName.Should().Be("Updated Name");
        result.Bio.Should().Be("New bio");
    }

    [Fact]
    public async Task GetFollowers_ReturnsUsersWhoFollow()
    {
        await _userService.FollowAsync(2, 1);
        await _userService.FollowAsync(3, 1);

        var followers = await _userService.GetFollowersAsync(1, 1, 10);

        followers.TotalCount.Should().Be(2);
        followers.Items.Select(u => u.Id).Should().Contain([2, 3]);
    }

    public void Dispose() => _db.Dispose();
}
