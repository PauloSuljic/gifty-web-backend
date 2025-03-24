using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gifty.Infrastructure;
using Gifty.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[Route("api/wishlists")]
[ApiController]
public class WishlistController : ControllerBase
{
    private readonly GiftyDbContext _context;

    public WishlistController(GiftyDbContext context)
    {
        _context = context;
    }

    // ✅ Create a Wishlist
    [HttpPost]
    public async Task<IActionResult> CreateWishlist([FromBody] Wishlist wishlist)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("User not authenticated.");
        
        _context.Wishlists.Add(wishlist);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserWishlists), new { userId }, wishlist);
    }

    // ✅ Get All Wishlists for Logged-in User
    [HttpGet]
    public async Task<IActionResult> GetUserWishlists()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("User not authenticated.");

        var wishlists = await _context.Wishlists
            .Where(w => w.UserId == userId)
            .Include(w => w.Items)
            .ToListAsync();

        return Ok(wishlists);
    }

    // ✅ Delete a Wishlist
    [HttpDelete("{wishlistId}")]
    public async Task<IActionResult> DeleteWishlist(Guid wishlistId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("User not authenticated.");
        
        var wishlist = await _context.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == wishlistId && w.UserId == userId);

        if (wishlist == null) return NotFound("Wishlist not found or you don't have permission to delete it.");

        _context.Wishlists.Remove(wishlist);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // ✅ PATCH: Rename Wishlist
    [HttpPatch("{wishlistId}")]
    public async Task<IActionResult> RenameWishlist(Guid wishlistId, [FromBody] string newName)
    {
        var wishlist = await _context.Wishlists.FindAsync(wishlistId);
        if (wishlist == null)
            return NotFound(new { error = "Wishlist not found." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (wishlist.UserId != userId)
            return Forbid();

        wishlist.Name = newName;
        await _context.SaveChangesAsync();
        return Ok(wishlist);
    }

}