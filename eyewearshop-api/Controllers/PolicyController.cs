using eyewearshop_service.Policy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/manager/policies")]
[Authorize(Roles = "Manager")]
public class PolicyController : ControllerBase
{
    private readonly IPolicyManager _policyManager;

    public PolicyController(IPolicyManager policyManager)
    {
        _policyManager = policyManager;
    }

    /// <summary>
    /// Get purchase policy
    /// </summary>
    [HttpGet("purchase")]
    public async Task<ActionResult> GetPurchasePolicy(CancellationToken ct)
    {
        var policy = await _policyManager.GetPurchasePolicyAsync(ct);
        return Ok(policy);
    }

    /// <summary>
    /// update purchase policy
    /// </summary>
    [HttpPut("purchase")]
    public async Task<ActionResult> UpdatePurchasePolicy([FromBody] UpdatePurchasePolicyRequest request, CancellationToken ct)
    {
        var policy = await _policyManager.UpdatePurchasePolicyAsync(request, ct);
        return Ok(policy);
    }

    /// <summary>
    /// get return policy
    /// </summary>
    [HttpGet("return")]
    public async Task<ActionResult> GetReturnPolicy(CancellationToken ct)
    {
        var policy = await _policyManager.GetReturnPolicyAsync(ct);
        return Ok(policy);
    }

    /// <summary>
    /// get return policy
    /// </summary>
    [HttpPut("return")]
    public async Task<ActionResult> UpdateReturnPolicy([FromBody] UpdateReturnPolicyRequest request, CancellationToken ct)
    {
        var policy = await _policyManager.UpdateReturnPolicyAsync(request, ct);
        return Ok(policy);
    }

    /// <summary>
    /// get warranty policy
    /// </summary>
    [HttpGet("warranty")]
    public async Task<ActionResult> GetWarrantyPolicy(CancellationToken ct)
    {
        var policy = await _policyManager.GetWarrantyPolicyAsync(ct);
        return Ok(policy);
    }

    /// <summary>
    /// update warranty policy
    /// </summary>
    [HttpPut("warranty")]
    public async Task<ActionResult> UpdateWarrantyPolicy([FromBody] UpdateWarrantyPolicyRequest request, CancellationToken ct)
    {
        var policy = await _policyManager.UpdateWarrantyPolicyAsync(request, ct);
        return Ok(policy);
    }
}
