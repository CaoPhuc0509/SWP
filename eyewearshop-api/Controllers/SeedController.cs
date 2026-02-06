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