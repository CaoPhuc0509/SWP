using eyewearshop_data;

namespace eyewearshop_service.Policy;

public class PolicyManager : IPolicyManager
{
    private readonly EyewearShopDbContext _db;

    public PolicyManager(EyewearShopDbContext db)
    {
        _db = db;
    }

    public async Task<PurchasePolicyDto> GetPurchasePolicyAsync(CancellationToken ct = default)
    {
        return new PurchasePolicyDto("Default Purchase Policy", 0, true, true);
    }

    public async Task<PurchasePolicyDto> UpdatePurchasePolicyAsync(UpdatePurchasePolicyRequest request, CancellationToken ct = default)
    {
        return new PurchasePolicyDto(request.Description, request.MinOrderValue, request.AllowPreOrder, request.AllowComboPurchase);
    }

    public async Task<ReturnPolicyDto> GetReturnPolicyAsync(CancellationToken ct = default)
    {
        return new ReturnPolicyDto("Default Return Policy", 30, 10);
    }

    public async Task<ReturnPolicyDto> UpdateReturnPolicyAsync(UpdateReturnPolicyRequest request, CancellationToken ct = default)
    {
        return new ReturnPolicyDto(request.Description, request.ReturnWindowDays, request.RestockingFeePercentage);
    }

    public async Task<WarrantyPolicyDto> GetWarrantyPolicyAsync(CancellationToken ct = default)
    {
        return new WarrantyPolicyDto("Default Warranty Policy", 12, true);
    }

    public async Task<WarrantyPolicyDto> UpdateWarrantyPolicyAsync(UpdateWarrantyPolicyRequest request, CancellationToken ct = default)
    {
        return new WarrantyPolicyDto(request.Description, request.WarrantyMonths, request.CoverManufacturingDefects);
    }
}
