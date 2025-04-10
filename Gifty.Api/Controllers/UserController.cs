using System.Security.Claims;
using gifty_web_backend.DTOs;
using Gifty.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gifty.Infrastructure;
using Gifty.Infrastructure.Services;

namespace gifty_web_backend.Controllers
{
    [Authorize]
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly GiftyDbContext _context;
        private readonly IRedisCacheService _cache;

        public UserController(GiftyDbContext context, IRedisCacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        // Get user by Firebase UID
        [HttpGet("{firebaseUid}")]
        public async Task<IActionResult> GetUserByFirebaseUid(string firebaseUid)
        {
            var cacheKey = $"user-profile:{firebaseUid}";
    
            var cached = await _cache.GetAsync<object>(cacheKey);
            if (cached != null) return Ok(cached);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == firebaseUid);

            if (user == null)
                return NotFound(new { message = "User not found" });

            var response = new
            {
                id = user.Id,
                username = user.Username,
                bio = user.Bio,
                email = user.Email,
                avatarUrl = user.AvatarUrl
            };

            await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return Ok(response);
        }
    
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
                return Unauthorized("User not authenticated.");

            if (firebaseUid != user.Id)
                return Forbid("You can only create a profile for your own Firebase account.");

            var exists = await _context.Users.AnyAsync(u => u.Id == firebaseUid);
            if (exists)
                return BadRequest(new { message = "User already exists" });

            // Add random avatar if needed
            var avatarOptions = new List<string>
            {
                "/avatars/avatar1.png", "/avatars/avatar2.png", "/avatars/avatar3.png",
                "/avatars/avatar4.png", "/avatars/avatar5.png", "/avatars/avatar6.png",
                "/avatars/avatar7.png", "/avatars/avatar8.png", "/avatars/avatar9.png",
                "/avatars/avatar10.png"
            };

            user.AvatarUrl = avatarOptions[new Random().Next(avatarOptions.Count)];

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserByFirebaseUid), new { firebaseUid = user.Id }, user);
        }
        
        [HttpPut("{firebaseUid}")]
        public async Task<IActionResult> UpdateUserProfile(string firebaseUid, [FromBody] UpdateUserDto model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == firebaseUid);
            if (user == null) return NotFound("User not found.");

            user.Username = model.Username;
            user.Bio = model.Bio;
            user.AvatarUrl = model.AvatarUrl;

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"user-profile:{firebaseUid}");

            return Ok(user);
        }
        
        // DELETE: api/users/{firebaseUid}
        [HttpDelete("{firebaseUid}")]
        public async Task<IActionResult> DeleteUser(string firebaseUid)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == firebaseUid);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"user-profile:{firebaseUid}");

            return NoContent();
        }
    }    
}
