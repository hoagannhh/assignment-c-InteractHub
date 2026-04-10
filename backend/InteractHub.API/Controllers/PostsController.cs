using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsController(IPostService postService, IAuthService authService) : ControllerBase
    {
        private readonly IPostService _posts = postService;
        private readonly IAuthService _auth = authService;

        private int CurrentUserId => _auth.GetCurrentUserId(User);

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _posts.GetFeedAsync(CurrentUserId, page, pageSize));

        [HttpGet("{postId:int}")]
        public async Task<IActionResult> GetPost(int postId)
        {
            try { return Ok(await _posts.GetPostAsync(postId, CurrentUserId)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetUserPosts(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _posts.GetUserPostsAsync(userId, page, pageSize, CurrentUserId));

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            try
            {
                var post = await _posts.CreatePostAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetPost), new { postId = post.Id }, post);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{postId:int}")]
        public async Task<IActionResult> UpdatePost(int postId, [FromBody] UpdatePostDto dto)
        {
            try { return Ok(await _posts.UpdatePostAsync(CurrentUserId, postId, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpDelete("{postId:int}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            try
            {
                await _posts.DeletePostAsync(CurrentUserId, postId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpPost("{postId:int}/like")]
        public async Task<IActionResult> LikePost(int postId)
        {
            try
            {
                var ok = await _posts.LikePostAsync(CurrentUserId, postId);
                return ok ? Ok(new { message = "Post liked." }) : BadRequest(new { error = "Already liked." });
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpDelete("{postId:int}/like")]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            var ok = await _posts.UnlikePostAsync(CurrentUserId, postId);
            return ok ? Ok(new { message = "Post unliked." }) : NotFound();
        }

        [HttpPost("{postId:int}/share")]
        public async Task<IActionResult> SharePost(int postId, [FromBody] string? content = null)
        {
            try
            {
                var post = await _posts.SharePostAsync(CurrentUserId, postId, content);
                return Ok(post);
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpGet("{postId:int}/comments")]
        public async Task<IActionResult> GetComments(int postId)
            => Ok(await _posts.GetCommentsAsync(postId, CurrentUserId));

        [HttpPost("{postId:int}/comments")]
        public async Task<IActionResult> AddComment(int postId, [FromBody] CreateCommentDto dto)
        {
            try { return Ok(await _posts.AddCommentAsync(CurrentUserId, postId, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpDelete("comments/{commentId:int}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                await _posts.DeleteCommentAsync(CurrentUserId, commentId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { error = "Query is required." });
            return Ok(await _posts.SearchPostsAsync(q, page, pageSize, CurrentUserId));
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrending([FromQuery] int count = 10)
            => Ok(await _posts.GetTrendingHashtagsAsync(count));

        [HttpGet("hashtag/{tag}")]
        public async Task<IActionResult> GetHashtagPosts(string tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _posts.GetHashtagPostsAsync(tag, page, pageSize, CurrentUserId));
    }
}
