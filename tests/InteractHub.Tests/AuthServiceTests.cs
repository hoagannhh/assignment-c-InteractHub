using FluentAssertions;
using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InteractHub.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "InteractHubTestSecretKeyThatIsAtLeast256BitsLong!!",
                ["Jwt:Issuer"] = "InteractHub",
                ["Jwt:Audience"] = "InteractHubClient"
            })
            .Build();

        _authService = new AuthService(_db, config);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto("testuser", "test@example.com", "Password123!", "Test User");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto1 = new RegisterDto("user1", "dup@example.com", "Password123!", null);
        var dto2 = new RegisterDto("user2", "dup@example.com", "Password123!", null);
        await _authService.RegisterAsync(dto1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto2));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto1 = new RegisterDto("sameuser", "email1@example.com", "Password123!", null);
        var dto2 = new RegisterDto("sameuser", "email2@example.com", "Password123!", null);
        await _authService.RegisterAsync(dto1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto2));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var register = new RegisterDto("logintest", "login@example.com", "Password123!", "Login Test");
        await _authService.RegisterAsync(register);

        // Act
        var result = await _authService.LoginAsync(new LoginDto("login@example.com", "Password123!"));

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterDto("user", "u@example.com", "CorrectPass!", null));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginDto("u@example.com", "WrongPass!")));
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginDto("nobody@example.com", "AnyPass!")));
    }

    [Fact]
    public async Task Register_PasswordIsHashed_NotStoredAsPlainText()
    {
        // Arrange
        var dto = new RegisterDto("hashtest", "hash@example.com", "PlainPassword!", null);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        var user = await _db.Users.FirstAsync(u => u.Email == "hash@example.com");
        user.PasswordHash.Should().NotBe("PlainPassword!");
        user.PasswordHash.Should().StartWith("$2");  // BCrypt hash prefix
    }

    public void Dispose() => _db.Dispose();
}
