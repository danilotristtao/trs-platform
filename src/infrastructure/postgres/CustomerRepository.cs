using Microsoft.EntityFrameworkCore;
using Trs.Sales;

namespace Trs.Infrastructure.Postgres;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly TrsDbContext _dbContext;

    public CustomerRepository(TrsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Filtro explícito por tenant_id além do RLS da sessão (defesa em
    // profundidade — ADR-0007/ADR-0011 exigem os dois, não um ou outro).
    public Task<Customer?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => await _dbContext.Customers.AddAsync(customer, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
