using System.Text.Json;
using eyewearshop_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace eyewearshop_api.Controllers;

/// <summary>
/// Proxies GHN master-data APIs (Province / District / Ward) to the frontend.
/// No authentication required — these are public reference data.
/// </summary>
[ApiController]
[Route("api/ghn")]
public class GhnController : ControllerBase
{
    private readonly IGhnShippingService _ghn;

    public GhnController(IGhnShippingService ghn)
    {
        _ghn = ghn;
    }

    /// <summary>
    /// Get all GHN provinces / cities.
    /// </summary>
    [HttpGet("provinces")]
    public async Task<ActionResult> GetProvinces(CancellationToken ct)
    {
        try
        {
            var data = await _ghn.GetProvincesAsync(ct);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Could not retrieve provinces from GHN.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Get GHN districts for a given province.
    /// </summary>
    /// <param name="provinceId">GHN province ID (from GET /api/ghn/provinces).</param>
    [HttpGet("districts")]
    public async Task<ActionResult> GetDistricts([FromQuery] int provinceId, CancellationToken ct)
    {
        if (provinceId <= 0)
            return BadRequest("provinceId is required.");

        try
        {
            var data = await _ghn.GetDistrictsAsync(provinceId, ct);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Could not retrieve districts from GHN.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Get GHN wards for a given district.
    /// </summary>
    /// <param name="districtId">GHN district ID (from GET /api/ghn/districts).</param>
    [HttpGet("wards")]
    public async Task<ActionResult> GetWards([FromQuery] int districtId, CancellationToken ct)
    {
        if (districtId <= 0)
            return BadRequest("districtId is required.");

        try
        {
            var data = await _ghn.GetWardsAsync(districtId, ct);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Could not retrieve wards from GHN.", detail = ex.Message });
        }
    }
}
