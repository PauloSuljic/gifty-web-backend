using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gifty.Domain.Entities;
using Gifty.Infrastructure;
using Gifty.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[Route("api/wishlist-items")]
[ApiController]
public class WishlistItemController : ControllerBase
{
    private readonly GiftyDbContext _context;
    private readonly IRedisCacheService _cache;

    public WishlistItemController(GiftyDbContext context, IRedisCacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ✅ Add a new item to a wishlist
    [HttpPost]
    public async Task<IActionResult> AddWishlistItem([FromBody] WishlistItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            return BadRequest(new { error = "The item field is required." });

        var wishlist = await _context.Wishlists.FindAsync(item.WishlistId);
        if (wishlist == null) return NotFound(new { error = "Wishlist not found." });

        _context.WishlistItems.Add(item);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"wishlist-items:{item.WishlistId}");

        return Ok(item);
    }

    // ✅ Get items for a specific wishlist
    [HttpGet("{wishlistId}")]
    public async Task<IActionResult> GetWishlistItems(Guid wishlistId)
    {
        var cacheKey = $"wishlist-items:{wishlistId}";

        // ✅ Try Redis first
        var cached = await _cache.GetAsync<List<WishlistItem>>(cacheKey);
        if (cached != null)
            return Ok(cached);

        // 🐢 Fallback to DB
        var items = await _context.WishlistItems
            .Where(i => i.WishlistId == wishlistId)
            .ToListAsync();

        // ✅ Save in cache
        await _cache.SetAsync(cacheKey, items, TimeSpan.FromMinutes(10));

        return Ok(items);
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteWishlistItem(Guid itemId)
    {
        var item = await _context.WishlistItems.FindAsync(itemId);
        if (item == null) return NotFound("Item not found.");

        var wishlistId = item.WishlistId;

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync($"wishlist-items:{wishlistId}");

        return NoContent();
    }

    // ✅ Toggle reservation (PATCH)
    [HttpPatch("{itemId}/reserve")]
    public async Task<IActionResult> ToggleReserveItem(Guid itemId)
    {
        var item = await _context.WishlistItems.FindAsync(itemId);
        if (item == null) return NotFound(new { error = "Item not found." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

        if (item.IsReserved)
        {
            if (item.ReservedBy != userId)
                return Forbid("You cannot unreserve an item reserved by someone else.");

            item.IsReserved = false;
            item.ReservedBy = null;
        }
        else
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == item.WishlistId);

            if (wishlist == null) return NotFound("Wishlist not found.");

            bool hasReservedItem = wishlist.Items.Any(i => i.IsReserved && i.ReservedBy == userId);
            if (hasReservedItem)
                return BadRequest(new { error = "You can only reserve 1 item per wishlist." });

            item.IsReserved = true;
            item.ReservedBy = userId;
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"wishlist-items:{item.WishlistId}");

        return Ok(item);
    }
    
    // ✅ Update an existing wishlist item (name and/or link)
    [HttpPatch("{itemId}")]
    public async Task<IActionResult> UpdateWishlistItem(Guid itemId, [FromBody] WishlistItem updated)
    {
        var item = await _context.WishlistItems.FindAsync(itemId);
        if (item == null) return NotFound(new { error = "Item not found." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

        var wishlist = await _context.Wishlists.FindAsync(item.WishlistId);
        if (wishlist == null || wishlist.UserId != userId)
            return Forbid("You are not allowed to edit this item.");

        if (!string.IsNullOrWhiteSpace(updated.Name))
            item.Name = updated.Name;

        if (!string.IsNullOrWhiteSpace(updated.Link))
            item.Link = updated.Link;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"wishlist-items:{item.WishlistId}");

        return Ok(item);
    }
    
}
