using eyewearshop_data.Entities;

namespace eyewearshop_service.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, string roleName);
}

