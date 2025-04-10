using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Gifty.Domain.Entities;
using Xunit;

namespace Gifty.Tests.Integration.Wishlists;

[Collection("IntegrationTestCollection")]
public class WishlistItemControllerTests
{
    private readonly HttpClient _client;
    private readonly string _userId;

    public WishlistItemControllerTests()
    {
        _userId = Guid.NewGuid().ToString(); // isolate data
        _client = new TestApiFactory().CreateClientWithTestAuth(_userId);
    }

    private async Task<Wishlist> CreateWishlistAsync()
    {
        var wishlist = new Wishlist
        {
            Name = "Wishlist with items",
            UserId = _userId
        };

        var res = await _client.PostAsJsonAsync("/api/wishlists", wishlist);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        return await res.Content.ReadFromJsonAsync<Wishlist>() ?? throw new Exception("Failed to create wishlist");
    }

    [Fact]
    public async Task AddWishlistItem_ShouldSucceed_WhenValid()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem
        {
            Name = "Cool Item",
            Link = "https://example.com",
            WishlistId = wishlist.Id
        };

        var response = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<WishlistItem>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Cool Item");
    }

    [Fact]
    public async Task AddWishlistItem_ShouldFail_IfWishlistMissing()
    {
        var item = new WishlistItem
        {
            Name = "Invalid",
            Link = "https://nope.com",
            WishlistId = Guid.NewGuid() // non-existent
        };

        var response = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddWishlistItem_ShouldFail_IfNameMissing()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem
        {
            Name = "",
            Link = "https://something.com",
            WishlistId = wishlist.Id
        };

        var response = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWishlistItems_ShouldReturnItems()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem
        {
            Name = "Item 1",
            Link = "https://1.com",
            WishlistId = wishlist.Id
        };

        await _client.PostAsJsonAsync("/api/wishlist-items", item);

        var response = await _client.GetAsync($"/api/wishlist-items/{wishlist.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<WishlistItem>>();
        items.Should().HaveCount(1);
        items![0].Name.Should().Be("Item 1");
    }
    
        [Fact]
    public async Task ToggleReservation_ShouldReserve_IfNoneReservedYet()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem
        {
            Name = "Reserve me",
            Link = "https://reserve.com",
            WishlistId = wishlist.Id
        };

        var createRes = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        var created = await createRes.Content.ReadFromJsonAsync<WishlistItem>();

        var reserveRes = await _client.PatchAsync($"/api/wishlist-items/{created!.Id}/reserve", null);
        reserveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var reserved = await reserveRes.Content.ReadFromJsonAsync<WishlistItem>();
        reserved!.IsReserved.Should().BeTrue();
        reserved.ReservedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task ToggleReservation_ShouldFail_IfUserAlreadyReservedAnother()
    {
        var wishlist = await CreateWishlistAsync();

        var item1 = new WishlistItem { Name = "Item 1", Link = "x", WishlistId = wishlist.Id };
        var item2 = new WishlistItem { Name = "Item 2", Link = "y", WishlistId = wishlist.Id };

        var res1 = await _client.PostAsJsonAsync("/api/wishlist-items", item1);
        var res2 = await _client.PostAsJsonAsync("/api/wishlist-items", item2);

        var i1 = await res1.Content.ReadFromJsonAsync<WishlistItem>();
        var i2 = await res2.Content.ReadFromJsonAsync<WishlistItem>();

        await _client.PatchAsync($"/api/wishlist-items/{i1!.Id}/reserve", null);

        var conflict = await _client.PatchAsync($"/api/wishlist-items/{i2!.Id}/reserve", null);
        conflict.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ToggleReservation_ShouldUnreserve_IfReserverMatches()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem { Name = "To unreserve", Link = "z", WishlistId = wishlist.Id };
        var res = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        var created = await res.Content.ReadFromJsonAsync<WishlistItem>();

        await _client.PatchAsync($"/api/wishlist-items/{created!.Id}/reserve", null);

        var unreserve = await _client.PatchAsync($"/api/wishlist-items/{created.Id}/reserve", null);
        unreserve.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await unreserve.Content.ReadFromJsonAsync<WishlistItem>();
        result!.IsReserved.Should().BeFalse();
        result.ReservedBy.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWishlistItem_ShouldChangeNameAndLink()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem { Name = "Old Name", Link = "http://old.com", WishlistId = wishlist.Id };
        var res = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        var created = await res.Content.ReadFromJsonAsync<WishlistItem>();

        var update = new
        {
            Name = "New Name",
            Link = "https://new.com"
        };

        var updateRes = await _client.PatchAsJsonAsync($"/api/wishlist-items/{created!.Id}", update);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateRes.Content.ReadFromJsonAsync<WishlistItem>();
        updated!.Name.Should().Be("New Name");
        updated.Link.Should().Be("https://new.com");
    }

    [Fact]
    public async Task DeleteWishlistItem_ShouldRemoveItem()
    {
        var wishlist = await CreateWishlistAsync();

        var item = new WishlistItem { Name = "Remove me", Link = "https://rip.com", WishlistId = wishlist.Id };
        var res = await _client.PostAsJsonAsync("/api/wishlist-items", item);
        var created = await res.Content.ReadFromJsonAsync<WishlistItem>();

        var deleteRes = await _client.DeleteAsync($"/api/wishlist-items/{created!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getItems = await _client.GetAsync($"/api/wishlist-items/{wishlist.Id}");
        var items = await getItems.Content.ReadFromJsonAsync<List<WishlistItem>>();

        items.Should().NotContain(i => i.Id == created.Id);
    }

}
