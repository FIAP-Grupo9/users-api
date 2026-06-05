using System.Security.Claims;
using FCG.Users.Api.Application.DTOs;
using FCG.Users.Api.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Users.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _users;

    public UsuariosController(IUsuarioService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDTO>>> Todos(CancellationToken ct)
    {
        var callerRole = User.IsInRole("Admin") ? "Admin" : "User";
        var r = await _users.GetAllUsuariosAsync(callerRole, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return Ok(r.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioDTO>> PorId(Guid id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentId)) return Unauthorized();
        if (!User.IsInRole("Admin") && currentId != id) return Forbid();

        var callerRole = User.IsInRole("Admin") ? "Admin" : "User";
        var r = await _users.GetUsuarioByIdAsync(id, callerRole, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return Ok(r.Data);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, RegisterUsuarioDTO dto, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentId)) return Unauthorized();
        if (!User.IsInRole("Admin") && currentId != id) return Forbid();

        var r = await _users.UpdateUsuarioAsync(id, dto, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deletar(Guid id, CancellationToken ct)
    {
        var r = await _users.DeleteUsuarioAsync(id, ct);
        if (!r.IsSuccess) return StatusCode(r.StatusCode, new { message = r.ErrorMessage });
        return NoContent();
    }

    private bool TryGetCurrentUserId(out Guid id)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out id);
    }
}
