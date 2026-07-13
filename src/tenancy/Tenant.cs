namespace Trs.Tenancy;

// ADR-0009 — Tenant é a raiz da fronteira de isolamento (ADR-0007),
// por isso não carrega tenant_id próprio nem campos de Rationale
// (reason_code/human_statement vivem nos Aggregates de dado de negócio,
// não na raiz da fronteira).
public enum TenantStatus
{
    Active,
    Suspended
}

public sealed class Tenant
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private Tenant(Guid id, string name, TenantStatus status, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Tenant Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tenant name é obrigatório.", nameof(name));
        }

        return new Tenant(Guid.NewGuid(), name, TenantStatus.Active, DateTimeOffset.UtcNow);
    }

    // Usado pelo Repository para reidratar a partir do banco, sem
    // reaplicar as validações de criação (ADR-0011 — invariantes de
    // criação e reidratação não são a mesma operação).
    public static Tenant Restore(Guid id, string name, TenantStatus status, DateTimeOffset createdAt)
        => new(id, name, status, createdAt);

    public void Suspend()
    {
        if (Status == TenantStatus.Suspended)
        {
            throw new InvalidOperationException("Tenant já está suspenso.");
        }

        Status = TenantStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status == TenantStatus.Active)
        {
            throw new InvalidOperationException("Tenant já está ativo.");
        }

        Status = TenantStatus.Active;
    }
}
