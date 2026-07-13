using Microsoft.EntityFrameworkCore;
using Trs.Tenancy;

namespace Trs.Infrastructure.Postgres;

public sealed class TenantRepository : ITenantRepository
{
    private readonly TrsDbContext _dbContext;

    public TenantRepository(TrsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        => await _dbContext.Tenants.AddAsync(tenant, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
