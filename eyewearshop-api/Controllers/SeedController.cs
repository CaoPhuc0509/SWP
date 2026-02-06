using eyewearshop_data;
using Microsoft.AspNetCore.Mvc;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public SeedController(EyewearShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Seed the database with sample catalog data (brands/categories/products/variants/etc.).
    /// Intended for local/dev environments.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Seed(CancellationToken ct)
    {
        try
        {
            await eyewearshop_data.SeedData.SeedAsync(_db);
            return Ok(new { Message = "Database seeded successfully!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }
}