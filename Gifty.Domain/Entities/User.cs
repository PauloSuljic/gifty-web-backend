using System.ComponentModel.DataAnnotations;

namespace Gifty.Domain.Entities
{
    public class User
    {
        [Key] public string Id { get; set; } = String.Empty;
        public string Username { get; set; } = String.Empty;
        [Required]
        public string Email { get; set; } = String.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Wishlist> Wishlists { get; set; } = new();
    }
}
