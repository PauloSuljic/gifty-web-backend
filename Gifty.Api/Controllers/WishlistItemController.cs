using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gifty.Domain.Entities;
using Gifty.Infrastructure;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[Route("api/wishlist-items")]
[ApiController]
public class WishlistItemController : ControllerBase
{
    private readonly GiftyDbContext _context;

    public WishlistItemController(GiftyDbContext context)
    {
        _context = context;
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

        return Ok(item);
    }

    // ✅ Get items for a specific wishlist
    [HttpGet("{wishlistId}")]
    public async Task<IActionResult> GetWishlistItems(Guid wishlistId)
    {
        var items = await _context.WishlistItems.Where(i => i.WishlistId == wishlistId).ToListAsync();
        return Ok(items);
    }

    // ✅ Delete an item from a wishlist
    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteWishlistItem(Guid itemId)
    {
        var item = await _context.WishlistItems.FindAsync(itemId);
        if (item == null) return NotFound("Item not found.");

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ✅ Toggle reservation (PATCH)
    [HttpPatch("{itemId}/reserve")]
    public async Task<IActionResult> ToggleReserveItem(Guid itemId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

        var item = await _context.WishlistItems
            .Include(i => i.Wishlist)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null) return NotFound(new { error = "Item not found." });

        // ✅ Prevent the wishlist owner from reserving their own items
        if (item.Wishlist.UserId == userId)
        {
            return BadRequest(new { error = "You cannot reserve items from your own wishlist." });
        }
        
        if (item.IsReserved)
        {
            if (item.ReservedBy.ToString() != userId)
            {
                return Forbid();
            }

            item.IsReserved = false;
            item.ReservedBy = null;
        }
        else
        {
            item.IsReserved = true;
            item.ReservedBy = userId;
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }
}
