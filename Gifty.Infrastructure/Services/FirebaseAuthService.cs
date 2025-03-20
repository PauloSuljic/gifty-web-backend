using Gifty.Domain.Entities;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace Gifty.Infrastructure.Services
{
    public class FirebaseAuthService
    {
        private readonly GiftyDbContext _dbContext;

        public FirebaseAuthService(GiftyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> AuthenticateUserAsync(string token)
        {
            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var firebaseUid = decodedToken.Uid;

                // Check if user exists in database
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == firebaseUid);

                if (user == null)
                {
                    // Fetch user details from Firebase
                    var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(firebaseUid);

                    user = new User
                    {
                        Id = firebaseUid,
                        Username = firebaseUser.DisplayName ?? $"user_{firebaseUid.Substring(0, 6)}",
                        AvatarUrl = firebaseUser.PhotoUrl,  // Optional: store Firebase avatar
                        Bio = "",
                        Email = firebaseUser.Email,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();
                }

                return user;
            }
            catch
            {
                return null;
            }
        }
    }
}
