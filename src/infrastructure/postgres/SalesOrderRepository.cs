using Microsoft.EntityFrameworkCore;
using Trs.Sales;

namespace Trs.Infrastructure.Postgres;

public sealed class SalesOrderRepository : ISalesOrderRepository
{
    private readonly TrsDbContext _dbContext;

    public SalesOrderRepository(TrsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Filtro explícito por tenant_id além do RLS da sessão (defesa em
    // profundidade — ADR-0007/ADR-0011 exigem os dois, não um ou outro).
    public Task<SalesOrder?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => _dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, cancellationToken);

    public async Task AddAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default)
        => await _dbContext.SalesOrders.AddAsync(salesOrder, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
