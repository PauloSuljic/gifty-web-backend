using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gifty.Domain.Entities
{
    public class WishlistItem
    {
        public Guid Id { get; set; }
        public Guid WishlistId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public Guid? ReservedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Wishlist Wishlist { get; set; }
    }
}
