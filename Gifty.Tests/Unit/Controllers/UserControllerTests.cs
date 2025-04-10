using System.Security.Claims;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gifty_web_backend.Controllers;
using Gifty.Infrastructure;
using Gifty.Domain.Entities;
using Gifty.Infrastructure.Services;
using Gifty.Tests.DTOs;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Gifty.Tests.Unit.Controllers
{
    public class UserControllerTests
    {
        
        private readonly UserController _controller;

        public UserControllerTests()
        {
            var options = new DbContextOptionsBuilder<GiftyDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var context = new GiftyDbContext(options);

            context.Users.Add(new User
            {
                Id = "test-user-id",
                Username = "TestUser",
                Bio = "Test Bio",
                Email = "test@example.com"
            });
            context.SaveChanges();

            // 👇 Mock RedisCacheService (we're not calling it in unit tests)
            var mockCache = new Mock<IRedisCacheService>();

            _controller = new UserController(context, mockCache.Object);
        }

        [Fact]
        public async Task GetUserByFirebaseUid_ShouldReturnUser_WhenUserExists()
        {
            // Act
            var result = await _controller.GetUserByFirebaseUid("test-user-id");

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonConvert.SerializeObject(okResult.Value);
            var userData = JsonConvert.DeserializeObject<UserDto>(json);

            userData.Should().NotBeNull();
            userData!.Username.Should().Be("TestUser");
        }


        [Fact]
        public async Task GetUserByFirebaseUid_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Act
            var result = await _controller.GetUserByFirebaseUid("nonexistent-id");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateUser_ShouldReturnCreated_WhenUserIsNew()
        {
            var newUser = new User
            {
                Id = "new-user-id",
                Username = "NewUser",
                Bio = "Bio",
                Email = "new@example.com"
            };

            // 👇 Mock ClaimsPrincipal with matching Firebase UID
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "new-user-id")
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.CreateUser(newUser);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task CreateUser_ShouldReturnBadRequest_WhenUserExists()
        {
            var existingUser = new User
            {
                Id = "test-user-id",
                Username = "TestUser",
                Bio = "Test Bio",
                Email = "test@example.com"
            };

            // 👇 Mock the ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
    
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var result = await _controller.CreateUser(existingUser);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNoContent_WhenUserExists()
        {
            // Act
            var result = await _controller.DeleteUser("test-user-id");

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Act
            var result = await _controller.DeleteUser("nonexistent-id");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
