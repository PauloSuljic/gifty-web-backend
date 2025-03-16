using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gifty.Domain.Entities
{
    public class SharedLink
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string ShareCode { get; set; } = Guid.NewGuid().ToString(); 

        [ForeignKey("Wishlist")]
        public Guid WishlistId { get; set; }

        public Wishlist Wishlist { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
