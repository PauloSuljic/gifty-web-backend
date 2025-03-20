using Gifty.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gifty.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly FirebaseAuthService _firebaseAuthService;

        public AuthController(FirebaseAuthService firebaseAuthService)
        {
            _firebaseAuthService = firebaseAuthService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TokenRequest request)
        {
            var user = await _firebaseAuthService.AuthenticateUserAsync(request.Token);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Bio,
                user.AvatarUrl
            });
        }
    }
    

    public class TokenRequest
    {
        public string Token { get; set; }
    }
}
