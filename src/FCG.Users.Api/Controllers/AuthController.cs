using FCG.Users.Api.Application.DTOs;
using FCG.Users.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Users.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _users;

    public AuthController(IUsuarioService users) => _users = users;

    [HttpPost("register")]
    public async Task<ActionResult<UsuarioDTO>> Register(RegisterUsuarioDTO dto, CancellationToken ct)
    {
        var r = await _users.CreateUsuarioAsync(dto, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return CreatedAtAction(nameof(GetUsuario), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDTO>> Login(LoginDTO dto, CancellationToken ct)
    {
        var r = await _users.AuthenticateAsync(dto, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return Ok(r.Data);
    }

    [HttpGet("user/{id}")]
    public async Task<ActionResult<UsuarioDTO>> GetUsuario(Guid id, CancellationToken ct)
    {
        var r = await _users.GetUsuarioByIdAsync(id, callerRole: null, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return Ok(r.Data);
    }

    [HttpPost("recuperar-senha")]
    public async Task<ActionResult<ForgotPasswordResponseDTO>> RecuperarSenha(ForgotPasswordDTO dto, CancellationToken ct)
    {
        var r = await _users.ForgotPasswordAsync(dto, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return Ok(r.Data);
    }

    [HttpPost("resetar-senha")]
    public async Task<IActionResult> ResetarSenha(ResetPasswordDTO dto, CancellationToken ct)
    {
        var r = await _users.ResetPasswordAsync(dto, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return Ok(new { message = "Senha alterada com sucesso" });
    }
}
