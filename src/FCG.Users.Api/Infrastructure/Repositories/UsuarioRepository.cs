using FCG.Users.Api.Domain.Entities;
using FCG.Users.Api.Domain.Repositories;
using FCG.Users.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FCG.Users.Api.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly UsersDbContext _db;

    public UsuarioRepository(UsersDbContext db) => _db = db;

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

    public async Task<IEnumerable<Usuario>> GetAllAsync(CancellationToken ct = default)
        => await _db.Usuarios.Where(u => u.IsActive).ToListAsync(ct);

    public async Task AddAsync(Usuario entity, CancellationToken ct = default)
    {
        await _db.Usuarios.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Usuario entity, CancellationToken ct = default)
    {
        _db.Usuarios.Update(entity);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var u = await _db.Usuarios.FindAsync(new object[] { id }, ct);
        if (u is null) return false;
        u.IsActive = false;
        u.UpdatedAt = DateTime.UtcNow;
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

    public Task<Usuario?> GetByEmailAndPasswordAsync(string email, string passwordHash, CancellationToken ct = default)
        => _db.Usuarios.FirstOrDefaultAsync(u =>
                u.Email == email && u.PasswordHash == passwordHash && u.IsActive, ct);
}
