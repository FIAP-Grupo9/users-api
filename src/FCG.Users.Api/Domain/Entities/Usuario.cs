using FCG.BuildingBlocks.Common;
using FCG.Users.Api.Domain.Enums;

namespace FCG.Users.Api.Domain.Entities;

public class Usuario : BaseEntity
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? Role { get; set; }
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.User;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiration { get; set; }
}
