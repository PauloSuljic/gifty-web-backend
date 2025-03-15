using Gifty.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gifty.Infrastructure;

namespace Gifty.Api.Controllers
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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserByFirebaseUid), new { firebaseUid = user.Id }, user);
        }

    }    
}
