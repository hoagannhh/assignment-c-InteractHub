using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController(IUserService userService, IAuthService authService) : ControllerBase
    {
        private readonly IUserService _users = userService;
        private readonly IAuthService _auth = authService;

        private int CurrentUserId => _auth.GetCurrentUserId(User);

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            try
            {
                var profile = await _users.GetProfileAsync(userId, CurrentUserId);
                return Ok(profile);
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
            => Ok(await _users.GetProfileAsync(CurrentUserId, CurrentUserId));

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            var updated = await _users.UpdateProfileAsync(CurrentUserId, dto);
            return Ok(updated);
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _users.GetSuggestionsAsync(CurrentUserId, page, pageSize));

        [HttpPost("{userId:int}/follow")]
        public async Task<IActionResult> Follow(int userId)
        {
            var ok = await _users.FollowAsync(CurrentUserId, userId);
            return ok ? Ok(new { message = "Followed successfully." }) : BadRequest(new { error = "Already following or invalid." });
        }

        [HttpDelete("{userId:int}/follow")]
        public async Task<IActionResult> Unfollow(int userId)
        {
            var ok = await _users.UnfollowAsync(CurrentUserId, userId);
            return ok ? Ok(new { message = "Unfollowed successfully." }) : NotFound();
        }

        [HttpGet("{userId:int}/followers")]
        public async Task<IActionResult> GetFollowers(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _users.GetFollowersAsync(userId, page, pageSize, CurrentUserId));

        [HttpGet("{userId:int}/following")]
        public async Task<IActionResult> GetFollowing(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _users.GetFollowingAsync(userId, page, pageSize, CurrentUserId));

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { error = "Query is required." });
            return Ok(await _users.SearchUsersAsync(q, page, pageSize, CurrentUserId));
        }
    }
}
