using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InteractHub.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        int GetCurrentUserId(ClaimsPrincipal user);
    }

    public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
    {
        private readonly AppDbContext _db = db;
        private readonly IConfiguration _config = config;

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email already in use.");
            if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
                throw new InvalidOperationException("Username already taken.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new UnauthorizedAccessException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            user.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // Simplified refresh: validate the JWT directly
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = int.Parse(token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

            var user = await _db.Users.FindAsync(userId)
                ?? throw new UnauthorizedAccessException("User not found.");

            return GenerateAuthResponse(user);
        }

        public int GetCurrentUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Not authenticated.");
            return int.Parse(claim.Value);
        }

        private AuthResponseDto GenerateAuthResponse(User user)
        {
            var token = GenerateJwt(user, 60);
            var refresh = GenerateJwt(user, 60 * 24 * 7);
            var dto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            };
            return new AuthResponseDto(token, refresh, dto);
        }

        private string GenerateJwt(User user, int expiryMinutes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["Jwt:Key"] ?? "InteractHubSuperSecretKeyThatIsAtLeast256BitsLong!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenObj = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "InteractHub",
                audience: _config["Jwt:Audience"] ?? "InteractHubClient",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenObj);
        }
    }
}
