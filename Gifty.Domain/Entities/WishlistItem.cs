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
    public class WishlistItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; }
        public string? Link { get; set; } // Optional: Product link
        public bool IsReserved { get; set; } = false;
        public string? ReservedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("Wishlist")]
        public Guid WishlistId { get; set; }
        [JsonIgnore]
        public Wishlist? Wishlist { get; set; }
    }
}
