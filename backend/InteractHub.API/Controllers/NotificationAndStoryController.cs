using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController(INotificationService notifService, IAuthService authService) : ControllerBase
    {
        private readonly INotificationService _notifs = notifService;
        private readonly IAuthService _auth = authService;

        private int CurrentUserId => _auth.GetCurrentUserId(User);

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _notifs.GetNotificationsAsync(CurrentUserId, page, pageSize));

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
            => Ok(new { count = await _notifs.GetUnreadCountAsync(CurrentUserId) });

        [HttpPut("read")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notifs.MarkAsReadAsync(CurrentUserId);
            return Ok(new { message = "All notifications marked as read." });
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notifs.MarkAsReadAsync(CurrentUserId, id);
            return Ok(new { message = "Notification marked as read." });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StoriesController(IStoryService storyService, IAuthService authService) : ControllerBase
    {
        private readonly IStoryService _stories = storyService;
        private readonly IAuthService _auth = authService;

        private int CurrentUserId => _auth.GetCurrentUserId(User);

        [HttpGet]
        public async Task<IActionResult> GetActiveStories()
            => Ok(await _stories.GetActiveStoriesAsync(CurrentUserId));

        [HttpPost]
        public async Task<IActionResult> CreateStory([FromBody] CreateStoryDto dto)
        {
            var story = await _stories.CreateStoryAsync(CurrentUserId, dto);
            return Ok(story);
        }

        [HttpPost("{storyId:int}/view")]
        public async Task<IActionResult> ViewStory(int storyId)
        {
            await _stories.ViewStoryAsync(CurrentUserId, storyId);
            return Ok(new { message = "Story viewed." });
        }

        [HttpDelete("{storyId:int}")]
        public async Task<IActionResult> DeleteStory(int storyId)
        {
            try
            {
                await _stories.DeleteStoryAsync(CurrentUserId, storyId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }
    }
}
