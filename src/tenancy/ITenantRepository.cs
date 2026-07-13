namespace Trs.Tenancy;

// ADR-0011 — único ponto de contato do domínio com persistência.
// Nenhum motor específico (Postgres/SQL Server) é referenciado aqui.
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
