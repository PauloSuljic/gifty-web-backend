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
    
    [Authorize]
    [HttpGet("shared-with-me")]
    public async Task<IActionResult> GetWishlistsSharedWithMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

        // ✅ Get wishlists where this user has visited the shared link
        var visitedWishlists = await _context.SharedLinkVisits
            .Include(v => v.SharedLink)
            .ThenInclude(l => l.Wishlist)
            .ThenInclude(w => w.Items)
            .Include(v => v.SharedLink.Wishlist.User)
            .Where(v => v.UserId == userId && v.SharedLink.Wishlist.UserId != userId)
            .ToListAsync();

        if (!visitedWishlists.Any()) return Ok(new List<object>());

        // ✅ Format response
        var result = visitedWishlists
            .GroupBy(v => new { v.SharedLink.Wishlist.UserId, v.SharedLink.Wishlist.User.Username })
            .Select(group => new
            {
                OwnerId = group.Key.UserId,
                OwnerName = group.Key.Username,
                Wishlists = group.Select(v => new
                {
                    v.SharedLink.Wishlist.Id,
                    v.SharedLink.Wishlist.Name,
                    Items = v.SharedLink.Wishlist.Items.Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Link,
                        i.IsReserved,
                        i.ReservedBy
                    }).ToList()
                }).ToList()
            }).ToList();

        return Ok(result);
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
    [AllowAnonymous] 
    [HttpGet("{shareCode}")]
    public async Task<IActionResult> GetSharedWishlist(string shareCode)
    {
        // ✅ Extract userId, but don't fail if null (guest user)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // ✅ Safe extraction

        var sharedLink = await _context.SharedLinks
            .Include(l => l.Wishlist)
            .ThenInclude(w => w.Items)
            .Include(l => l.Wishlist.User)
            .FirstOrDefaultAsync(l => l.ShareCode == shareCode);

        if (sharedLink == null) return NotFound(new { error = "Invalid shared link." });

        // ✅ Only store a visit if the user is logged in
        if (!string.IsNullOrEmpty(userId) && userId != sharedLink.Wishlist.UserId)
        {
            var existingVisit = await _context.SharedLinkVisits
                .FirstOrDefaultAsync(v => v.UserId == userId && v.SharedLinkId == sharedLink.Id);

            if (existingVisit == null)
            {
                var newVisit = new SharedLinkVisit
                {
                    SharedLinkId = sharedLink.Id,
                    UserId = userId
                };
                _context.SharedLinkVisits.Add(newVisit);
                await _context.SaveChangesAsync();
            }
        }

        // ✅ Return wishlist details (even for guests)
        return Ok(new
        {
            sharedLink.Wishlist.Id,
            sharedLink.Wishlist.Name,
            OwnerId = sharedLink.Wishlist.UserId,
            OwnerName = sharedLink.Wishlist.User?.Username, 
            Items = sharedLink.Wishlist.Items.Select(i => new
            {
                i.Id,
                i.Name,
                i.Link,
                i.IsReserved,
                i.ReservedBy
            }).ToList()
        });
    }


}
