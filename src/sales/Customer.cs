namespace Trs.Sales;

public enum CustomerStatus
{
    Active,
    Inactive
}

// Aggregate Root escopado ao Bounded Context Sales — não é um
// "cliente universal" cross-context (ADR-0006, ADR-0009).
public sealed class Customer
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public string Name { get; private set; }
    public TaxId TaxId { get; private set; }
    public CustomerStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public ReasonCode ReasonCode { get; }
    public string? HumanStatement { get; }
    public Guid? Author { get; }

    private Customer(
        Guid id,
        Guid tenantId,
        string name,
        TaxId taxId,
        CustomerStatus status,
        DateTimeOffset createdAt,
        ReasonCode reasonCode,
        string? humanStatement,
        Guid? author)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        TaxId = taxId;
        Status = status;
        CreatedAt = createdAt;
        ReasonCode = reasonCode;
        HumanStatement = humanStatement;
        Author = author;
    }

    public static Customer Create(
        Guid tenantId,
        string name,
        TaxId taxId,
        ReasonCode reasonCode = ReasonCode.RoutineCreation,
        string? humanStatement = null,
        Guid? author = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Customer deve pertencer a exatamente um Tenant.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("name é obrigatório.", nameof(name));
        }

        if (reasonCode != ReasonCode.RoutineCreation && string.IsNullOrWhiteSpace(humanStatement))
        {
            throw new ArgumentException(
                $"human_statement é obrigatório quando reason_code é '{reasonCode}'.",
                nameof(humanStatement));
        }

        return new Customer(
            Guid.NewGuid(), tenantId, name, taxId, CustomerStatus.Active, DateTimeOffset.UtcNow,
            reasonCode, humanStatement, author);
    }

    public static Customer Restore(
        Guid id,
        Guid tenantId,
        string name,
        TaxId taxId,
        CustomerStatus status,
        DateTimeOffset createdAt,
        ReasonCode reasonCode,
        string? humanStatement,
        Guid? author)
        => new(id, tenantId, name, taxId, status, createdAt, reasonCode, humanStatement, author);

    public void Deactivate()
    {
        if (Status == CustomerStatus.Inactive)
        {
            throw new InvalidOperationException("Customer já está inativo.");
        }

        Status = CustomerStatus.Inactive;
    }
}
