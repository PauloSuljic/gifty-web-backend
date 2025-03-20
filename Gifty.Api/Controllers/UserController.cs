using gifty_web_backend.DTOs;
using Gifty.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gifty.Infrastructure;

namespace gifty_web_backend.Controllers
{
    [Authorize]
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly GiftyDbContext _context;

        public UserController(GiftyDbContext context)
        {
            _context = context;
        }

        // Get user by Firebase UID
        [HttpGet("{firebaseUid}")]
        public async Task<IActionResult> GetUserByFirebaseUid(string firebaseUid)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == firebaseUid);
        
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                bio = user.Bio,
                email = user.Email,
                avatarUrl = user.AvatarUrl
            });
        }
    
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (await _context.Users.AnyAsync(u => u.Id == user.Id))
            {
                return BadRequest(new { message = "User already exists" });
            }
            
            var avatarOptions = new List<string>
            {
                "/avatars/avatar1.png",
                "/avatars/avatar2.png",
                "/avatars/avatar3.png",
                "/avatars/avatar4.png",
                "/avatars/avatar5.png",
                "/avatars/avatar6.png",
                "/avatars/avatar7.png",
                "/avatars/avatar8.png",
                "/avatars/avatar9.png",
                "/avatars/avatar10.png"
            };

            var random = new Random();
            
            int randomIndex = random.Next(avatarOptions.Count);
            user.AvatarUrl = avatarOptions[randomIndex];

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
            return Ok(user);
        }
    
    }    
}
