namespace Trs.Sales;

// ADR-0011 — único ponto de contato do domínio com persistência.
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
