using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gifty.Domain.Entities;
using Gifty.Infrastructure;
using Gifty.Infrastructure.Services;
using Gifty.Tests.DTOs;
using Microsoft.AspNetCore.Authorization;

[Route("api/shared-links")]
[ApiController]
public class SharedLinkController : ControllerBase
{
    private readonly GiftyDbContext _context;
    private readonly IRedisCacheService _cache;

    public SharedLinkController(GiftyDbContext context, IRedisCacheService cache)
    {
        _context = context;
        _cache = cache;
    }
    
    [Authorize]
    [HttpGet("shared-with-me")]
    public async Task<IActionResult> GetWishlistsSharedWithMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

        string cacheKey = $"shared-with-me:{userId}";

        // ✅ Try cache
        var cached = await _cache.GetAsync<object>(cacheKey);
        if (cached != null)
            return Ok(cached);

        // 🐢 DB fallback
        var visitedWishlists = await _context.SharedLinkVisits
            .Include(v => v.SharedLink)
            .ThenInclude(l => l.Wishlist)
            .ThenInclude(w => w.Items)
            .Include(v => v.SharedLink.Wishlist.User)
            .Where(v => v.UserId == userId && v.SharedLink.Wishlist.UserId != userId)
            .ToListAsync();

        if (!visitedWishlists.Any()) return Ok(new List<object>());

        var result = visitedWishlists
            .GroupBy(v => new
            {
                v.SharedLink.Wishlist.UserId,
                v.SharedLink.Wishlist.User?.Username,
                v.SharedLink.Wishlist.User?.AvatarUrl
            })
            .Select(group => new
            {
                OwnerId = group.Key.UserId,
                OwnerName = group.Key.Username,
                OwnerAvatar = group.Key.AvatarUrl,
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

        // ✅ Store in Redis
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

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
        if (wishlist.UserId != userId) return Forbid(); // Only the owner can generate a link

        var existingLink = await _context.SharedLinks.FirstOrDefaultAsync(l => l.WishlistId == wishlistId);
        if (existingLink != null)
        {
            return Ok(new ShareLinkResponseDto(existingLink.ShareCode));
        }

        var sharedLink = new SharedLink { WishlistId = wishlistId };
        _context.SharedLinks.Add(sharedLink);
        await _context.SaveChangesAsync();

        return Ok(new ShareLinkResponseDto(sharedLink.ShareCode));
    }


    // ✅ Retrieve a wishlist using a shareable link
    [AllowAnonymous] 
    [HttpGet("{shareCode}")]
    public async Task<IActionResult> GetSharedWishlist(string shareCode)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        string cacheKey = $"shared-link:{shareCode}";

        // ✅ Check cache first
        var cached = await _cache.GetAsync<object>(cacheKey);
        if (cached != null)
            return Ok(cached);

        // 🐢 DB fallback
        var sharedLink = await _context.SharedLinks
            .Include(l => l.Wishlist)
            .ThenInclude(w => w.Items)
            .Include(l => l.Wishlist.User)
            .FirstOrDefaultAsync(l => l.ShareCode == shareCode);

        if (sharedLink == null)
            return NotFound(new { error = "Invalid shared link." });

        var response = new
        {
            sharedLink.Wishlist.Id,
            sharedLink.Wishlist.Name,
            OwnerId = sharedLink.Wishlist.UserId,
            OwnerName = sharedLink.Wishlist.User?.Username,
            OwnerAvatar = sharedLink.Wishlist.User?.AvatarUrl,
            Items = sharedLink.Wishlist.Items.Select(i => new
            {
                i.Id,
                i.Name,
                i.Link,
                i.IsReserved,
                i.ReservedBy
            }).ToList()
        };

        // ✅ Store in Redis
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));

        // 🧠 Log shared visit (don't cache this part)
        if (!string.IsNullOrEmpty(userId) && userId != sharedLink.Wishlist.UserId)
        {
            var visited = await _context.SharedLinkVisits
                .FirstOrDefaultAsync(v => v.UserId == userId && v.SharedLinkId == sharedLink.Id);

            if (visited == null)
            {
                _context.SharedLinkVisits.Add(new SharedLinkVisit
                {
                    SharedLinkId = sharedLink.Id,
                    UserId = userId
                });
                await _context.SaveChangesAsync();
            }
        }

        return Ok(response);
    }
    
}
