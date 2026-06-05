using FCG.Users.Api.Domain.Entities;

namespace FCG.Users.Api.Domain.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Usuario>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Usuario entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Usuario entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> GetByEmailAndPasswordAsync(string email, string passwordHash, CancellationToken ct = default);
}
