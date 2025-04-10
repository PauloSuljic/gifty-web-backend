using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gifty.Infrastructure;
using Gifty.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using gifty_web_backend.Controllers;
using Gifty.Infrastructure.Services;
using Gifty.Tests.DTOs;
using Moq;

namespace Gifty.Tests.Unit.Controllers
{
    public class SharedLinkControllerTests
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

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        private SharedLinkController GetControllerWithUser(GiftyDbContext db, string userId)
        {
            var mockCache = new Mock<IRedisCacheService>();
            var controller = new SharedLinkController(db, mockCache.Object);
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
        public async Task GenerateShareLink_ShouldReturnExisting_IfAlreadyExists()
        {
            var db = GetDbContext();
            var userId = "test-user";
            var wishlist = new Wishlist { Id = Guid.NewGuid(), Name = "Test", UserId = userId };
            var sharedLink = new SharedLink { WishlistId = wishlist.Id };

            db.Wishlists.Add(wishlist);
            db.SharedLinks.Add(sharedLink);
            await db.SaveChangesAsync();

            var controller = GetControllerWithUser(db, userId);

            // Act
            var result = await controller.GenerateShareLink(wishlist.Id);

            // Assert
            var ok = result as OkObjectResult;
            var response = ok?.Value as ShareLinkResponseDto;

            response.Should().NotBeNull();
            response!.ShareCode.Should().Be(sharedLink.ShareCode);
        }

        [Fact]
        public async Task GenerateShareLink_ShouldCreateNewLink_IfNotExists()
        {
            var db = GetDbContext();
            var userId = "test-user";
            var wishlist = new Wishlist { Id = Guid.NewGuid(), Name = "Wishlist 1", UserId = userId };
            db.Wishlists.Add(wishlist);
            await db.SaveChangesAsync();

            var controller = GetControllerWithUser(db, userId);

            // Act
            var result = await controller.GenerateShareLink(wishlist.Id);

            // Assert
            var ok = result as OkObjectResult;
            var response = ok?.Value as ShareLinkResponseDto;

            response.Should().NotBeNull();
            response!.ShareCode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetWishlistsSharedWithMe_ShouldReturnEmpty_IfNone()
        {
            var db = GetDbContext();
            var controller = GetControllerWithUser(db, "new-user");

            var result = await controller.GetWishlistsSharedWithMe();

            var ok = result as OkObjectResult;
            var data = ok?.Value as List<object>;

            data.Should().NotBeNull();
            data!.Should().BeEmpty();
        }
    }
}
