using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Gifty.Domain.Entities
{
    public class Wishlist
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; }
        public bool IsPublic { get; set; } = false; // Default: Private
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        [JsonIgnore]
        public User? User { get; set; }
        public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Order { get; set; }
    }
}
