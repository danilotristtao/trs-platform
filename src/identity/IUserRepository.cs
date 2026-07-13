namespace Trs.Identity;

// ADR-0011 — único ponto de contato do domínio com persistência.
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
