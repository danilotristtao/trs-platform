using Microsoft.EntityFrameworkCore;
using Trs.Identity;

namespace Trs.Infrastructure.Postgres;

public sealed class UserRepository : IUserRepository
{
    private readonly TrsDbContext _dbContext;

    public UserRepository(TrsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Filtro explícito por tenant_id além do RLS da sessão (defesa em
    // profundidade — ADR-0007/ADR-0011 exigem os dois, não um ou outro).
    public Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _dbContext.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
