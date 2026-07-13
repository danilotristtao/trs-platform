namespace Trs.Sales;

// ADR-0011 — único ponto de contato do domínio com persistência.
public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
