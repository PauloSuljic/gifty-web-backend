using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Gifty.Domain.Entities;
using Gifty.Infrastructure;
using Gifty.Infrastructure.Services;
using Moq;

namespace Gifty.Tests.Unit.Controllers
{
    public class WishlistItemControllerTests
    {
        private GiftyDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<GiftyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new GiftyDbContext(options);
        }

        private ClaimsPrincipal GetFakeUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private WishlistItemController GetControllerWithUser(GiftyDbContext db, string userId)
        {
            var mockCache = new Mock<IRedisCacheService>();
            var controller = new WishlistItemController(db, mockCache.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetFakeUser(userId)
                }
            };
            return controller;
        }

        [Fact]
        public async Task AddWishlistItem_ShouldAdd_WhenDataIsValid()
        {
            var db = GetDbContext();
            var wishlist = new Wishlist
            {
                Id = Guid.NewGuid(),
                Name = "My Wishlist",
                UserId = "user-123"
            };
            db.Wishlists.Add(wishlist);
            await db.SaveChangesAsync();

            var controller = GetControllerWithUser(db, "user-123");

            var item = new WishlistItem
            {
                Name = "Headphones",
                Link = "https://example.com",
                WishlistId = wishlist.Id
            };

            var result = await controller.AddWishlistItem(item);

            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            var addedItem = okResult!.Value as WishlistItem;
            addedItem!.Name.Should().Be("Headphones");
        }

        [Fact]
        public async Task DeleteWishlistItem_ShouldRemove_WhenItemExists()
        {
            var db = GetDbContext();
            var item = new WishlistItem
            {
                Id = Guid.NewGuid(),
                Name = "Keyboard",
                WishlistId = Guid.NewGuid()
            };
            db.WishlistItems.Add(item);
            await db.SaveChangesAsync();

            var controller = GetControllerWithUser(db, "user-1");

            var result = await controller.DeleteWishlistItem(item.Id);

            result.Should().BeOfType<NoContentResult>();
            db.WishlistItems.Any(i => i.Id == item.Id).Should().BeFalse();
        }

        [Fact]
        public async Task ToggleReserveItem_ShouldReserve_IfValid()
        {
            var db = GetDbContext();
            var userId = "user-123";

            var wishlist = new Wishlist
            {
                Id = Guid.NewGuid(),
                UserId = "owner-id",
                Name = "ReserveTest"
            };

            var item = new WishlistItem
            {
                Id = Guid.NewGuid(),
                Name = "Shoes",
                WishlistId = wishlist.Id,
                IsReserved = false
            };

            wishlist.Items = new List<WishlistItem> { item };

            db.Wishlists.Add(wishlist);
            db.WishlistItems.Add(item);
            await db.SaveChangesAsync();

            var controller = GetControllerWithUser(db, userId);

            var result = await controller.ToggleReserveItem(item.Id);

            var okResult = result as OkObjectResult;
            var reservedItem = okResult?.Value as WishlistItem;

            reservedItem.Should().NotBeNull();
            reservedItem!.IsReserved.Should().BeTrue();
            reservedItem.ReservedBy.Should().Be(userId);
        }

        [Fact]
        public async Task UpdateWishlistItem_ShouldUpdateNameAndLink()
        {
            var db = GetDbContext();
            var userId = "user-789";
            var wishlist = new Wishlist
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Test Wishlist"
            };

            var item = new WishlistItem
            {
                Id = Guid.NewGuid(),
                Name = "Old Name",
                Link = "old-link",
                WishlistId = wishlist.Id
            };

            db.Wishlists.Add(wishlist);
            db.WishlistItems.Add(item);
            await db.SaveChangesAsync();

            var controller = GetControllerWithUser(db, userId);

            var updated = new WishlistItem
            {
                Name = "New Name",
                Link = "https://new-link"
            };

            var result = await controller.UpdateWishlistItem(item.Id, updated);
            var okResult = result as OkObjectResult;
            var updatedItem = okResult?.Value as WishlistItem;

            updatedItem!.Name.Should().Be("New Name");
            updatedItem.Link.Should().Be("https://new-link");
        }
    }
}
