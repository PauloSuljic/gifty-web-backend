using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Gifty.Domain.Entities;
using FluentAssertions;

namespace Gifty.Tests.Integration.Users
{
    [Collection("IntegrationTestCollection")]
    public class UserControllerTests
    {
        private readonly TestApiFactory _factory;

        public UserControllerTests(TestApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateUser_ShouldReturnCreated()
        {
            var userId = "firebase-test-id";
            var client = _factory.CreateClientWithTestAuth(userId);

            var user = new User
            {
                Id = userId,
                Username = "TestUser",
                Email = "test@example.com",
                Bio = "Just testing"
            };

            var response = await client.PostAsJsonAsync("/api/users", user);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<User>();
            created.Should().NotBeNull();
            created!.Username.Should().Be("TestUser");
        }

        [Fact]
        public async Task CreateUser_ShouldFail_WhenUserAlreadyExists()
        {
            var userId = "duplicate-user";
            var client = _factory.CreateClientWithTestAuth(userId);

            var user = new User
            {
                Id = userId,
                Username = "User1",
                Email = "email@domain.com",
                Bio = "First"
            };

            // First create succeeds
            var response1 = await client.PostAsJsonAsync("/api/users", user);
            response1.StatusCode.Should().Be(HttpStatusCode.Created);

            // Second create should fail (user already exists)
            var response2 = await client.PostAsJsonAsync("/api/users", user);
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUserByFirebaseUid_ShouldReturnUser_WhenExists()
        {
            var userId = "fetch-me";
            var client = _factory.CreateClientWithTestAuth(userId);

            var user = new User
            {
                Id = userId,
                Username = "Fetcher",
                Email = "fetch@example.com",
                Bio = "Here to be fetched"
            };

            await client.PostAsJsonAsync("/api/users", user);

            var response = await client.GetAsync($"/api/users/{userId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var returned = await response.Content.ReadFromJsonAsync<User>();
            returned.Should().NotBeNull();
            returned!.Username.Should().Be("Fetcher");
        }

        [Fact]
        public async Task GetUserByFirebaseUid_ShouldReturnNotFound_WhenUserMissing()
        {
            var client = _factory.CreateClientWithTestAuth("someone");
            var response = await client.GetAsync("/api/users/non-existent-user");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateUser_ShouldUpdate_WhenUserExists()
        {
            var userId = "update-me";
            var client = _factory.CreateClientWithTestAuth(userId);

            var user = new User
            {
                Id = userId,
                Username = "BeforeUpdate",
                Email = "before@update.com",
                Bio = "Old bio"
            };

            await client.PostAsJsonAsync("/api/users", user);

            var updatePayload = new
            {
                Username = "UpdatedUser",
                Bio = "Updated bio",
                AvatarUrl = "/avatars/avatar1.png"
            };

            var response = await client.PutAsJsonAsync($"/api/users/{userId}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updated = await response.Content.ReadFromJsonAsync<User>();
            updated.Should().NotBeNull();
            updated!.Username.Should().Be("UpdatedUser");
            updated.Bio.Should().Be("Updated bio");
            updated.AvatarUrl.Should().Be("/avatars/avatar1.png");
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNoContent_WhenUserExists()
        {
            var userId = "delete-me";
            var client = _factory.CreateClientWithTestAuth(userId);

            var user = new User
            {
                Id = userId,
                Username = "ToBeDeleted",
                Email = "delete@me.com",
                Bio = "I'm doomed"
            };

            await client.PostAsJsonAsync("/api/users", user);

            var response = await client.DeleteAsync($"/api/users/{userId}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var followUp = await client.GetAsync($"/api/users/{userId}");
            followUp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            var client = _factory.CreateClientWithTestAuth("some-user");
            var response = await client.DeleteAsync("/api/users/ghost-user");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateUser_ShouldReturnNotFound_IfUserDoesNotExist()
        {
            var userId = "missing-id";
            var client = _factory.CreateClientWithTestAuth(userId);

            var update = new
            {
                Username = "Updated Name",
                Bio = "Updated bio",
                AvatarUrl = "/avatars/avatar3.png"
            };

            var response = await client.PutAsJsonAsync($"/api/users/{userId}", update);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
