using eyewearshop_data.Entities;

namespace eyewearshop_service.Interfaces;

public interface IAuthService
{
    Task<(User user, string roleName, string? error, int? statusCode)> RegisterCustomerAsync(
        string email,
        string password,
        string? fullName,
        string? phoneNumber,
        CancellationToken ct = default);

    Task<(User user, string roleName, string? error, int? statusCode)> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default);

    Task<(User? user, string? error)> GetUserByIdAsync(long userId, CancellationToken ct = default);
}

