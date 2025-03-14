using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gifty.Domain.Entities
{
    public class Wishlist
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; }
        public List<WishlistItem> Items { get; set; } = new();
    }
}
