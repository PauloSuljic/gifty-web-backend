using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Gifty.Domain.Entities;
using Xunit;

namespace Gifty.Tests.Integration.SharedLinks;

[Collection("IntegrationTestCollection")]
public class SharedLinkControllerTests
{
    private readonly HttpClient _client;
    private readonly TestApiFactory _factory;
    private readonly string _userId = "shared-user-id";

    public SharedLinkControllerTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithTestAuth(_userId);
    }

    private async Task<Wishlist> CreateWishlistAsync(string name = "Shared Wishlist")
    {
        // ✅ Step 1: Make sure user exists
        await _client.PostAsJsonAsync("/api/users", new User
        {
            Id = _userId,
            Username = "Test",
            Email = "test@example.com",
            Bio = "integration test"
        });

        // ✅ Step 2: Send DTO (no custom ID)
        var dto = new { Name = name, IsPublic = false };
        var res = await _client.PostAsJsonAsync("/api/wishlists", dto);
        res.EnsureSuccessStatusCode();

        // ✅ Step 3: Parse actual wishlist with generated ID
        var wishlist = await res.Content.ReadFromJsonAsync<Wishlist>();
        return wishlist!;
    }

    [Fact]
    public async Task GenerateShareLink_ShouldReturnNewCode_IfNoneExists()
    {
        var wishlist = await CreateWishlistAsync();

        var response = await _client.PostAsync($"/api/shared-links/{wishlist.Id}/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        data.Should().ContainKey("shareCode");
    }

    [Fact]
    public async Task GenerateShareLink_ShouldReturnSameCode_IfExists()
    {
        var wishlist = await CreateWishlistAsync();

        var first = await _client.PostAsync($"/api/shared-links/{wishlist.Id}/generate", null);
        first.EnsureSuccessStatusCode(); // ✅

        var firstCode = (await first.Content.ReadFromJsonAsync<Dictionary<string, string>>())["shareCode"];

        var second = await _client.PostAsync($"/api/shared-links/{wishlist.Id}/generate", null);
        second.EnsureSuccessStatusCode(); // ✅

        var secondCode = (await second.Content.ReadFromJsonAsync<Dictionary<string, string>>())["shareCode"];

        firstCode.Should().Be(secondCode);
    }

    [Fact]
    public async Task GetSharedWishlist_ShouldReturn404_IfInvalidCode()
    {
        var anonClient = _factory.CreateClient();

        // Act
        var response = await anonClient.GetAsync("/api/shared-links/invalid-code");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}
