namespace Trs.Identity;

public enum UserStatus
{
    Active,
    Deactivated
}

// ADR-0009, ajuste 2 — exatamente dois papéis fixos nesta fase.
// Authorization Layer ("pode tentar?"), não Policy Layer (ADR-0008).
public enum UserRole
{
    TenantAdmin,
    Member
}

public sealed class User
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public string ExternalIdentityReference { get; }
    public EmailAddress Email { get; private set; }
    public string Name { get; private set; }
    public UserStatus Status { get; private set; }
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    // Rationale mínimo (ADR-0009, ajuste 1 / AR-KNW-001, AR-KNW-006).
    public ReasonCode ReasonCode { get; }
    public string? HumanStatement { get; }
    public Guid? Author { get; }

    private User(
        Guid id,
        Guid tenantId,
        string externalIdentityReference,
        EmailAddress email,
        string name,
        UserStatus status,
        UserRole role,
        DateTimeOffset createdAt,
        ReasonCode reasonCode,
        string? humanStatement,
        Guid? author)
    {
        Id = id;
        TenantId = tenantId;
        ExternalIdentityReference = externalIdentityReference;
        Email = email;
        Name = name;
        Status = status;
        Role = role;
        CreatedAt = createdAt;
        ReasonCode = reasonCode;
        HumanStatement = humanStatement;
        Author = author;
    }

    public static User Create(
        Guid tenantId,
        string externalIdentityReference,
        EmailAddress email,
        string name,
        UserRole role,
        ReasonCode reasonCode = ReasonCode.RoutineCreation,
        string? humanStatement = null,
        Guid? author = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("User deve pertencer a exatamente um Tenant.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(externalIdentityReference))
        {
            throw new ArgumentException("external_identity_reference é obrigatório.", nameof(externalIdentityReference));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("name é obrigatório.", nameof(name));
        }

        // ADR-0009, ajuste 1 — human_statement só é dispensado quando o
        // reason_code é o fluxo padrão.
        if (reasonCode != ReasonCode.RoutineCreation && string.IsNullOrWhiteSpace(humanStatement))
        {
            throw new ArgumentException(
                $"human_statement é obrigatório quando reason_code é '{reasonCode}'.",
                nameof(humanStatement));
        }

        return new User(
            Guid.NewGuid(),
            tenantId,
            externalIdentityReference,
            email,
            name,
            UserStatus.Active,
            role,
            DateTimeOffset.UtcNow,
            reasonCode,
            humanStatement,
            author);
    }

    // Usado pelo Repository para reidratar a partir do banco (ADR-0011)
    // — não reaplica as validações de criação.
    public static User Restore(
        Guid id,
        Guid tenantId,
        string externalIdentityReference,
        EmailAddress email,
        string name,
        UserStatus status,
        UserRole role,
        DateTimeOffset createdAt,
        ReasonCode reasonCode,
        string? humanStatement,
        Guid? author)
        => new(id, tenantId, externalIdentityReference, email, name, status, role, createdAt, reasonCode, humanStatement, author);

    public void Deactivate()
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new InvalidOperationException("User já está desativado.");
        }

        Status = UserStatus.Deactivated;
    }
}
