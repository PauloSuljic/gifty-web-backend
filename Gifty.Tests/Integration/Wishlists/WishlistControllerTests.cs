using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;
using FluentAssertions;
using Gifty.Domain.Entities;
using Gifty.Tests.Integration;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit.Abstractions;

namespace Gifty.Tests.Integration.Wishlists
{
    [Collection("IntegrationTestCollection")]
    public class WishlistControllerTests
    {
        private readonly TestApiFactory _factory;
        private readonly HttpClient _client;
        private readonly string _userId = "wishlist-user-id";
        private readonly ITestOutputHelper _output;

        public WishlistControllerTests(TestApiFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = _factory.CreateClientWithTestAuth(_userId);
            _output = output;
        }

        private async Task CreateTestUser(string userId, HttpClient client)
        {
            var user = new User
            {
                Id = userId,
                Username = "Test User",
                Email = $"{userId}@test.com",
                Bio = "Test Bio"
            };

            var response = await client.PostAsJsonAsync("/api/users", user);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.BadRequest && body.Contains("User already exists"))
                    return; // ✅ Ignore duplicate users for test runs

                throw new Exception($"❌ Failed to create user ({response.StatusCode}):\n{body}");
            }
        }

        [Fact]
        public async Task CreateWishlist_ShouldReturnCreated()
        {
            var userId = "wishlist-user-id";
            var client = _factory.CreateClientWithTestAuth(userId); // ✅ Ensure auth matches

            // ✅ User must be created with the same ID as the one in the header
            await CreateTestUser(userId, client);

            var dto = new { Name = "Integration Wishlist", IsPublic = false };
            var response = await client.PostAsJsonAsync("/api/wishlists", dto);

            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<Wishlist>();
            created!.Name.Should().Be("Integration Wishlist");
            created.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task GetUserWishlists_ShouldReturnOnlyCurrentUserWishlists()
        {
            var userId = Guid.NewGuid().ToString();
            var client = _factory.CreateClientWithTestAuth(userId);
            var otherClient = _factory.CreateClientWithTestAuth("other-user");

            await CreateTestUser(userId, client);
            await CreateTestUser("other-user", otherClient);

            for (int i = 0; i < 2; i++)
            {
                var dto = new { Name = $"My Wishlist {i}", IsPublic = false };
                var res = await client.PostAsJsonAsync("/api/wishlists", dto);
                res.EnsureSuccessStatusCode();
            }

            var otherDto = new { Name = "Other User List", IsPublic = true };
            var otherRes = await otherClient.PostAsJsonAsync("/api/wishlists", otherDto);
            otherRes.EnsureSuccessStatusCode();

            var response = await client.GetAsync("/api/wishlists");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<List<Wishlist>>();
            result.Should().NotBeNull();
            result!.Count.Should().Be(2);
            result.All(w => w.UserId == userId).Should().BeTrue();
        }

        [Fact]
        public async Task RenameWishlist_ShouldUpdateName()
        {
            await CreateTestUser(_userId, _client);

            var dto = new { Name = "Before Rename", IsPublic = false };
            var response = await _client.PostAsJsonAsync("/api/wishlists", dto);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<Wishlist>();
            var content = new StringContent("\"Renamed List\"", Encoding.UTF8, "application/json");

            var renameResponse = await _client.PatchAsync($"/api/wishlists/{created!.Id}", content);
            renameResponse.EnsureSuccessStatusCode();

            var renamed = await renameResponse.Content.ReadFromJsonAsync<Wishlist>();
            renamed!.Name.Should().Be("Renamed List");
        }

        [Fact]
        public async Task DeleteWishlist_ShouldRemoveSuccessfully()
        {
            var userId = "wishlist-user-id";
            var client = _factory.CreateClientWithTestAuth(userId); // ✅ matches the payload's ID
            await CreateTestUser(userId, client);

            var dto = new { Name = "To Delete", IsPublic = false };
            var response = await _client.PostAsJsonAsync("/api/wishlists", dto);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"❌ POST failed: {response.StatusCode}\n{body}");
            }

            var created = await response.Content.ReadFromJsonAsync<Wishlist>();

            var delete = await _client.DeleteAsync($"/api/wishlists/{created!.Id}");
            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var check = await _client.GetAsync("/api/wishlists");
            check.EnsureSuccessStatusCode();

            var wishlists = await check.Content.ReadFromJsonAsync<List<Wishlist>>();
            wishlists!.Any(w => w.Id == created.Id).Should().BeFalse();
        }

        [Fact]
        public async Task ReorderWishlists_ShouldRearrangeOrder()
        {
            var userId = Guid.NewGuid().ToString();
            var client = _factory.CreateClientWithTestAuth(userId);
            await CreateTestUser(userId, client);

            var res1 = await client.PostAsJsonAsync("/api/wishlists", new { Name = "List A", IsPublic = false });
            var created1 = await res1.Content.ReadFromJsonAsync<Wishlist>();

            var res2 = await client.PostAsJsonAsync("/api/wishlists", new { Name = "List B", IsPublic = false });
            var created2 = await res2.Content.ReadFromJsonAsync<Wishlist>();

            var reorderPayload = new[]
            {
                new { Id = created2!.Id, Order = 0 },
                new { Id = created1!.Id, Order = 1 }
            };

            var reorderRes = await client.PutAsJsonAsync("/api/wishlists/reorder", reorderPayload);
            reorderRes.EnsureSuccessStatusCode();

            var finalRes = await client.GetAsync("/api/wishlists");
            finalRes.EnsureSuccessStatusCode();

            var wishlists = await finalRes.Content.ReadFromJsonAsync<List<Wishlist>>();
            wishlists.Should().NotBeNull().And.HaveCount(2);

            var sorted = wishlists!.OrderBy(w => w.Order).ToList();
            sorted[0].Id.Should().Be(created2.Id);
            sorted[1].Id.Should().Be(created1.Id);
        }
    }
}
