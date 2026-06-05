using FCG.Users.Api.Domain.Entities;

namespace FCG.Users.Api.Domain.Services;

public interface ITokenService
{
    string GenerateToken(Usuario usuario);
    DateTime GetExpiration();
}
