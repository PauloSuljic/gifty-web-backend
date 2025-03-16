using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gifty.Domain.Entities;
using Gifty.Infrastructure;
using Microsoft.AspNetCore.Authorization;

[Route("api/shared-links")]
[ApiController]
public class SharedLinkController : ControllerBase
{
    private readonly GiftyDbContext _context;

    public SharedLinkController(GiftyDbContext context)
    {
        _context = context;
    }

    // ✅ Generate a shareable link for a wishlist
    [Authorize]
    [HttpPost("{wishlistId}/generate")]
    public async Task<IActionResult> GenerateShareLink(Guid wishlistId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("User not authenticated.");

        var wishlist = await _context.Wishlists.FindAsync(wishlistId);
        if (wishlist == null) return NotFound("Wishlist not found.");
        if (wishlist.UserId != userId) return Forbid(); // ✅ Only the owner can generate a link

        // ✅ Check if a share link already exists
        var existingLink = await _context.SharedLinks.FirstOrDefaultAsync(l => l.WishlistId == wishlistId);
        if (existingLink != null) return Ok(new { shareCode = existingLink.ShareCode });

        // ✅ Create a new shared link
        var sharedLink = new SharedLink { WishlistId = wishlistId };
        _context.SharedLinks.Add(sharedLink);
        await _context.SaveChangesAsync();

        return Ok(new { shareCode = sharedLink.ShareCode });
    }

    // ✅ Retrieve a wishlist using a shareable link
    [HttpGet("{shareCode}")]
    public async Task<IActionResult> GetSharedWishlist(string shareCode)
    {
        var sharedLink = await _context.SharedLinks
            .Include(l => l.Wishlist)
            .ThenInclude(w => w.Items)
            .FirstOrDefaultAsync(l => l.ShareCode == shareCode);

        if (sharedLink == null) return NotFound("Invalid share link.");

        return Ok(sharedLink.Wishlist);
    }
}
