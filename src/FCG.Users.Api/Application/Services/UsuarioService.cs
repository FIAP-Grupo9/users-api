using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FCG.BuildingBlocks.Common;
using FCG.Contracts.Events;
using FCG.Users.Api.Application.DTOs;
using FCG.Users.Api.Domain.Entities;
using FCG.Users.Api.Domain.Enums;
using FCG.Users.Api.Domain.Repositories;
using FCG.Users.Api.Domain.Services;
using MassTransit;

namespace FCG.Users.Api.Application.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$", RegexOptions.Compiled);

    private readonly IUsuarioRepository _repo;
    private readonly ITokenService _tokens;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(
        IUsuarioRepository repo,
        ITokenService tokens,
        IPublishEndpoint publisher,
        ILogger<UsuarioService> logger)
    {
        _repo = repo;
        _tokens = tokens;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UsuarioDTO>>> GetAllUsuariosAsync(string? callerRole, CancellationToken ct = default)
    {
        var usuarios = await _repo.GetAllAsync(ct);
        if (callerRole != "Admin")
            usuarios = usuarios.Where(u => u.Role != "Admin");

        var result = usuarios.Select(Map);
        return Result<IEnumerable<UsuarioDTO>>.Success(result);
    }

    public async Task<Result<UsuarioDTO>> GetUsuarioByIdAsync(Guid id, string? callerRole, CancellationToken ct = default)
    {
        var u = await _repo.GetByIdAsync(id, ct);
        if (u is null)
            return Result<UsuarioDTO>.Failure(Messages.UsuarioNaoEncontrado, 404);

        if (callerRole != "Admin" && u.Role == "Admin")
            return Result<UsuarioDTO>.Failure(Messages.UsuarioSemPermissao, 403);

        return Result<UsuarioDTO>.Success(Map(u));
    }

    public async Task<Result<UsuarioDTO>> CreateUsuarioAsync(RegisterUsuarioDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex.IsMatch(dto.Email))
            return Result<UsuarioDTO>.Failure(Messages.EmailFormatoInvalido, 400);

        if (string.IsNullOrWhiteSpace(dto.Password) || !PasswordRegex.IsMatch(dto.Password))
            return Result<UsuarioDTO>.Failure(Messages.SenhaInsegura, 400);

        var existing = await _repo.GetByEmailAsync(dto.Email, ct);
        if (existing is not null)
            return Result<UsuarioDTO>.Failure(Messages.EmailJaExiste, 409);

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password!),
            Role = "User",
            Perfil = PerfilUsuario.User,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _repo.AddAsync(usuario, ct);

        var evt = new UserCreatedEvent(
            MessageId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            UserId: usuario.Id,
            Name: usuario.Name ?? string.Empty,
            Email: usuario.Email ?? string.Empty,
            Role: usuario.Role ?? "User");

        try
        {
            await _publisher.Publish(evt, ct);
        }
        catch (Exception ex)
        {
            // O usuário foi criado; falha de publish é registrada mas não derruba o request.
            // (Trade-off documentado: ver README — outbox transacional não implementado.)
            _logger.LogError(ex, "Falha ao publicar UserCreatedEvent para {UserId}", usuario.Id);
        }

        return Result<UsuarioDTO>.Success(Map(usuario), 201);
    }

    public async Task<Result<LoginResponseDTO>> AuthenticateAsync(LoginDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResponseDTO>.Failure(Messages.EmailOuSenhaInvalidos, 401);

        var hash = HashPassword(dto.Password);
        var usuario = await _repo.GetByEmailAndPasswordAsync(dto.Email, hash, ct);
        if (usuario is null)
            return Result<LoginResponseDTO>.Failure(Messages.EmailOuSenhaInvalidos, 401);

        var token = _tokens.GenerateToken(usuario);
        return Result<LoginResponseDTO>.Success(new LoginResponseDTO
        {
            Token = token,
            Usuario = Map(usuario),
            ExpiresAt = _tokens.GetExpiration()
        });
    }

    public async Task<Result<bool>> UpdateUsuarioAsync(Guid id, RegisterUsuarioDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex.IsMatch(dto.Email))
            return Result<bool>.Failure(Messages.EmailFormatoInvalido, 400);

        if (!string.IsNullOrWhiteSpace(dto.Password) && !PasswordRegex.IsMatch(dto.Password))
            return Result<bool>.Failure(Messages.SenhaInsegura, 400);

        var u = await _repo.GetByIdAsync(id, ct);
        if (u is null)
            return Result<bool>.Failure(Messages.UsuarioNaoEncontrado, 404);

        u.Name = dto.Name;
        u.Email = dto.Email;
        u.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            u.PasswordHash = HashPassword(dto.Password);

        await _repo.UpdateAsync(u, ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteUsuarioAsync(Guid id, CancellationToken ct = default)
    {
        var ok = await _repo.DeleteAsync(id, ct);
        return ok
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(Messages.UsuarioNaoEncontrado, 404);
    }

    public async Task<Result<ForgotPasswordResponseDTO>> ForgotPasswordAsync(ForgotPasswordDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex.IsMatch(dto.Email))
            return Result<ForgotPasswordResponseDTO>.Failure(Messages.EmailFormatoInvalido, 400);

        var u = await _repo.GetByEmailAsync(dto.Email, ct);
        if (u is null)
            return Result<ForgotPasswordResponseDTO>.Failure(Messages.UsuarioNaoEncontrado, 404);

        var token = GenerateResetToken();
        u.PasswordResetToken = token;
        u.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1);
        u.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(u, ct);

        return Result<ForgotPasswordResponseDTO>.Success(new ForgotPasswordResponseDTO
        {
            Message = Messages.TokenRecuperacaoEnviado,
            ResetToken = token
        });
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex.IsMatch(dto.Email))
            return Result<bool>.Failure(Messages.EmailFormatoInvalido, 400);

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || !PasswordRegex.IsMatch(dto.NewPassword))
            return Result<bool>.Failure(Messages.SenhaInsegura, 400);

        var u = await _repo.GetByEmailAsync(dto.Email, ct);
        if (u is null)
            return Result<bool>.Failure(Messages.UsuarioNaoEncontrado, 404);

        if (u.PasswordResetToken != dto.Token ||
            u.PasswordResetTokenExpiration is null ||
            u.PasswordResetTokenExpiration < DateTime.UtcNow)
        {
            return Result<bool>.Failure(Messages.TokenInvalidoOuExpirado, 400);
        }

        u.PasswordHash = HashPassword(dto.NewPassword);
        u.PasswordResetToken = null;
        u.PasswordResetTokenExpiration = null;
        u.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(u, ct);
        return Result<bool>.Success(true);
    }

    private static UsuarioDTO Map(Usuario u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role
    };

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateResetToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
