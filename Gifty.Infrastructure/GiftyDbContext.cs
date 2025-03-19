using Microsoft.EntityFrameworkCore;
using Gifty.Domain.Entities;

namespace Gifty.Infrastructure
{
    public class GiftyDbContext : DbContext
    {
        public GiftyDbContext(DbContextOptions<GiftyDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<SharedLink> SharedLinks { get; set; }
        public DbSet<SharedLinkVisit> SharedLinkVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(wi => wi.Wishlist)
                .WithMany(w => w.Items)
                .HasForeignKey(wi => wi.WishlistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
