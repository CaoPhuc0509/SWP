using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_service.Auth;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<(User user, string roleName, string? error, int? statusCode)> RegisterCustomerAsync(
        string email,
        string password,
        string? fullName,
        string? phoneNumber,
        CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
        {
            return (null!, null!, "Email and password are required.", 400);
        }

        var exists = await _userRepository
            .Query()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);
        if (exists)
        {
            return (null!, null!, "Email already registered.", 409);
        }

        var customerRole = await _roleRepository
            .Query()
            .FirstOrDefaultAsync(r => r.RoleName == RoleNames.Customer, ct);
        if (customerRole == null)
        {
            return (null!, null!, "Default role 'Customer' is missing in roles table.", 500);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password),
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            RoleId = customerRole.RoleId,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return (user, customerRole.RoleName, null, 200);
    }

    public async Task<(User user, string roleName, string? error, int? statusCode)> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _userRepository
            .Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return (null!, null!, "Invalid credentials.", 401);
        }

        if (user.Status != 1)
        {
            return (null!, null!, "User is not active.", 401);
        }

        var roleName = user.Role?.RoleName ?? string.Empty;
        return (user, roleName, null, 200);
    }

    public async Task<(User? user, string? error)> GetUserByIdAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepository
            .Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null)
        {
            return (null, "User not found.");
        }

        if (user.Status != 1)
        {
            return (null, "User is not active.");
        }

        return (user, null);
    }
}

