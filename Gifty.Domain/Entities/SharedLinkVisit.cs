using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gifty.Domain.Entities
{
    public class SharedLinkVisit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("SharedLink")]
        public Guid SharedLinkId { get; set; }
        public SharedLink SharedLink { get; set; }

        [Required]
        public string UserId { get; set; } 

        public DateTime VisitedAt { get; set; } = DateTime.UtcNow; 
    }
}