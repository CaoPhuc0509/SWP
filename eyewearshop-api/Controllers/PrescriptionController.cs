using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
public class PrescriptionController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public PrescriptionController(EyewearShopDbContext db)
    {
        _db = db;
    }

    public record CreatePrescriptionRequest(
        decimal? RightSphere,
        decimal? RightCylinder,
        decimal? RightAxis,
        decimal? RightAdd,
        decimal? RightPD,
        decimal? LeftSphere,
        decimal? LeftCylinder,
        decimal? LeftAxis,
        decimal? LeftAdd,
        decimal? LeftPD,
        string? Notes,
        DateTime? PrescriptionDate,
        string? PrescribedBy);

    public record UpdatePrescriptionRequest(
        decimal? RightSphere,
        decimal? RightCylinder,
        decimal? RightAxis,
        decimal? RightAdd,
        decimal? RightPD,
        decimal? LeftSphere,
        decimal? LeftCylinder,
        decimal? LeftAxis,
        decimal? LeftAdd,
        decimal? LeftPD,
        string? Notes,
        DateTime? PrescriptionDate,
        string? PrescribedBy);

    [HttpGet]
    public async Task<ActionResult> GetMyPrescriptions(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var prescriptions = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.CustomerId == userId && p.Status == 1)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PrescriptionId,
                p.RightSphere,
                p.RightCylinder,
                p.RightAxis,
                p.RightAdd,
                p.RightPD,
                p.LeftSphere,
                p.LeftCylinder,
                p.LeftAxis,
                p.LeftAdd,
                p.LeftPD,
                p.Notes,
                p.PrescriptionDate,
                p.PrescribedBy,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(prescriptions);
    }

    [HttpGet("{prescriptionId:long}")]
    public async Task<ActionResult> GetPrescriptionDetail([FromRoute] long prescriptionId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var prescription = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.PrescriptionId == prescriptionId && p.CustomerId == userId && p.Status == 1)
            .Select(p => new
            {
                p.PrescriptionId,
                p.RightSphere,
                p.RightCylinder,
                p.RightAxis,
                p.RightAdd,
                p.RightPD,
                p.LeftSphere,
                p.LeftCylinder,
                p.LeftAxis,
                p.LeftAdd,
                p.LeftPD,
                p.Notes,
                p.PrescriptionDate,
                p.PrescribedBy,
                p.CreatedAt,
                p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (prescription == null) return NotFound();

        return Ok(prescription);
    }

    [HttpPost]
    public async Task<ActionResult> CreatePrescription([FromBody] CreatePrescriptionRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var now = DateTime.UtcNow;
        var prescription = new Prescription
        {
            CustomerId = userId,
            RightSphere = request.RightSphere,
            RightCylinder = request.RightCylinder,
            RightAxis = request.RightAxis,
            RightAdd = request.RightAdd,
            RightPD = request.RightPD,
            LeftSphere = request.LeftSphere,
            LeftCylinder = request.LeftCylinder,
            LeftAxis = request.LeftAxis,
            LeftAdd = request.LeftAdd,
            LeftPD = request.LeftPD,
            Notes = request.Notes,
            PrescriptionDate = request.PrescriptionDate,
            PrescribedBy = request.PrescribedBy,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            prescription.PrescriptionId,
            prescription.CreatedAt
        });
    }

    [HttpPut("{prescriptionId:long}")]
    public async Task<ActionResult> UpdatePrescription(
        [FromRoute] long prescriptionId,
        [FromBody] UpdatePrescriptionRequest request,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId && p.CustomerId == userId && p.Status == 1, ct);

        if (prescription == null) return NotFound();

        prescription.RightSphere = request.RightSphere;
        prescription.RightCylinder = request.RightCylinder;
        prescription.RightAxis = request.RightAxis;
        prescription.RightAdd = request.RightAdd;
        prescription.RightPD = request.RightPD;
        prescription.LeftSphere = request.LeftSphere;
        prescription.LeftCylinder = request.LeftCylinder;
        prescription.LeftAxis = request.LeftAxis;
        prescription.LeftAdd = request.LeftAdd;
        prescription.LeftPD = request.LeftPD;
        prescription.Notes = request.Notes;
        prescription.PrescriptionDate = request.PrescriptionDate;
        prescription.PrescribedBy = request.PrescribedBy;
        prescription.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            prescription.PrescriptionId,
            prescription.UpdatedAt
        });
    }

    [HttpDelete("{prescriptionId:long}")]
    public async Task<ActionResult> DeletePrescription([FromRoute] long prescriptionId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId && p.CustomerId == userId && p.Status == 1, ct);

        if (prescription == null) return NotFound();

        // Orders store a snapshot (OrderPrescription) and do NOT reference editable prescriptions.
        // So users can freely delete their saved prescriptions.
        _db.Prescriptions.Remove(prescription);

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
}