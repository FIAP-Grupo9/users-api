using System.Security.Cryptography;
using System.Text;
using FCG.Users.Api.Domain.Entities;
using FCG.Users.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FCG.Users.Api.Infrastructure.Data;

/// <summary>
/// Cria o usuário administrador inicial. Sem ele, um ambiente recém-criado sobe
/// sem ninguém capaz de cadastrar jogos, já que não existe rota para promover
/// alguém a administrador — o único caminho seria alterar o banco na mão.
///
/// Desligado por padrão: só age quando SeedData:Enabled está habilitado, o que
/// os manifestos locais fazem.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(
        UsersDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("SeedData:Enabled"))
            return;

        var email = configuration["SeedData:AdminEmail"] ?? "admin@fcg.com";

        // O serviço roda com mais de uma réplica, e todas executam este código na
        // subida. Sem a trava, todas encontram a tabela vazia ao mesmo tempo e o
        // administrador acabaria duplicado. A trava é liberada no fim da transação.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(72302)", ct);

        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
        {
            logger.LogInformation("Carga inicial: administrador {Email} já existe.", email);
            await tx.CommitAsync(ct);
            return;
        }

        var senha = configuration["SeedData:AdminPassword"] ?? "Admin@1234";

        db.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Name = "Administrador FCG",
            Email = email,
            PasswordHash = HashPassword(senha),
            Role = "Admin",
            Perfil = PerfilUsuario.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Carga inicial: administrador {Email} criado.", email);
    }

    // Mesmo algoritmo do UsuarioService, para que o login funcione com a senha semeada.
    private static string HashPassword(string password)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
}
