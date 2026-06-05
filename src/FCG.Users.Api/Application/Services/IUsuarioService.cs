using FCG.BuildingBlocks.Common;
using FCG.Users.Api.Application.DTOs;

namespace FCG.Users.Api.Application.Services;

public interface IUsuarioService
{
    Task<Result<IEnumerable<UsuarioDTO>>> GetAllUsuariosAsync(string? callerRole, CancellationToken ct = default);
    Task<Result<UsuarioDTO>> GetUsuarioByIdAsync(Guid id, string? callerRole, CancellationToken ct = default);
    Task<Result<UsuarioDTO>> CreateUsuarioAsync(RegisterUsuarioDTO dto, CancellationToken ct = default);
    Task<Result<LoginResponseDTO>> AuthenticateAsync(LoginDTO dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateUsuarioAsync(Guid id, RegisterUsuarioDTO dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteUsuarioAsync(Guid id, CancellationToken ct = default);
    Task<Result<ForgotPasswordResponseDTO>> ForgotPasswordAsync(ForgotPasswordDTO dto, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordDTO dto, CancellationToken ct = default);
}
