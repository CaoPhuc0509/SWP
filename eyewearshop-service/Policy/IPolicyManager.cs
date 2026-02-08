namespace eyewearshop_service.Policy;

/// <summary>
/// Quản lý quy định nghiệp vụ, chính sách mua/đổi trả/bảo hành
/// </summary>
public interface IPolicyManager
{
    Task<PurchasePolicyDto> GetPurchasePolicyAsync(CancellationToken ct = default);
    Task<PurchasePolicyDto> UpdatePurchasePolicyAsync(UpdatePurchasePolicyRequest request, CancellationToken ct = default);
    
    Task<ReturnPolicyDto> GetReturnPolicyAsync(CancellationToken ct = default);
    Task<ReturnPolicyDto> UpdateReturnPolicyAsync(UpdateReturnPolicyRequest request, CancellationToken ct = default);
    
    Task<WarrantyPolicyDto> GetWarrantyPolicyAsync(CancellationToken ct = default);
    Task<WarrantyPolicyDto> UpdateWarrantyPolicyAsync(UpdateWarrantyPolicyRequest request, CancellationToken ct = default);
}

public record PurchasePolicyDto(string Description, int MinOrderValue, bool AllowPreOrder, bool AllowComboPurchase);
public record UpdatePurchasePolicyRequest(string Description, int MinOrderValue, bool AllowPreOrder, bool AllowComboPurchase);
public record ReturnPolicyDto(string Description, int ReturnWindowDays, decimal RestockingFeePercentage);
public record UpdateReturnPolicyRequest(string Description, int ReturnWindowDays, decimal RestockingFeePercentage);
public record WarrantyPolicyDto(string Description, int WarrantyMonths, bool CoverManufacturingDefects);
public record UpdateWarrantyPolicyRequest(string Description, int WarrantyMonths, bool CoverManufacturingDefects);
