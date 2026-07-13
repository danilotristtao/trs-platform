namespace Trs.Sales;

public enum SalesOrderStatus
{
    Draft,
    Active
}

public sealed class SalesOrder
{
    private readonly List<SalesOrderLine> _lines = new();

    public Guid Id { get; }
    public Guid TenantId { get; }
    public Guid CustomerId { get; }
    public IReadOnlyList<SalesOrderLine> Lines => _lines;
    public Money Total { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    // Rationale (ADR-0009).
    public ReasonCode ReasonCode { get; }
    public string? HumanStatement { get; }
    public string? SourceReference { get; }
    public Guid? Author { get; }
    public DateTimeOffset? Validity { get; }
    public string ConfidentialityLevel { get; }

    private SalesOrder(
        Guid id,
        Guid tenantId,
        Guid customerId,
        SalesOrderStatus status,
        DateTimeOffset createdAt,
        ReasonCode reasonCode,
        string? humanStatement,
        string? sourceReference,
        Guid? author,
        DateTimeOffset? validity,
        string confidentialityLevel)
    {
        Id = id;
        TenantId = tenantId;
        CustomerId = customerId;
        Status = status;
        CreatedAt = createdAt;
        ReasonCode = reasonCode;
        HumanStatement = humanStatement;
        SourceReference = sourceReference;
        Author = author;
        Validity = validity;
        ConfidentialityLevel = confidentialityLevel;

        // Placeholder sempre sobrescrito por RecalculateTotal() antes do
        // Aggregate ser exposto fora de Create/Restore (nenhum dos dois
        // caminhos retorna sem ao menos uma linha adicionada).
        Total = Money.Zero("XXX");
    }

    public static SalesOrder Create(
        Guid tenantId,
        Guid customerId,
        IReadOnlyList<(string Description, decimal Quantity, Money UnitPrice)> lines,
        ReasonCode reasonCode = ReasonCode.RoutineCreation,
        string? humanStatement = null,
        string? sourceReference = null,
        Guid? author = null,
        string confidentialityLevel = "standard")
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("SalesOrder deve pertencer a exatamente um Tenant.", nameof(tenantId));
        }

        if (customerId == Guid.Empty)
        {
            // AR-TXN-001 — referência por ID, nunca objeto embutido.
            throw new ArgumentException("customer_id é obrigatório.", nameof(customerId));
        }

        if (lines is null || lines.Count == 0)
        {
            // Invariante 1 (ADR-0009).
            throw new ArgumentException(
                "SalesOrder precisa de ao menos uma SalesOrderLine para ser válido.", nameof(lines));
        }

        if (reasonCode != ReasonCode.RoutineCreation && string.IsNullOrWhiteSpace(humanStatement))
        {
            throw new ArgumentException(
                $"human_statement é obrigatório quando reason_code é '{reasonCode}'.",
                nameof(humanStatement));
        }

        var order = new SalesOrder(
            Guid.NewGuid(), tenantId, customerId, SalesOrderStatus.Draft, DateTimeOffset.UtcNow,
            reasonCode, humanStatement, sourceReference, author, validity: null, confidentialityLevel);

        foreach (var line in lines)
        {
            order.AddLine(line.Description, line.Quantity, line.UnitPrice);
        }

        return order;
    }

    // Usado pelo Repository para reidratar a partir do banco (ADR-0011)
    // — assume que as linhas fornecidas já satisfazem os invariantes
    // (foram validadas quando escritas).
    public static SalesOrder Restore(
        Guid id,
        Guid tenantId,
        Guid customerId,
        SalesOrderStatus status,
        DateTimeOffset createdAt,
        ReasonCode reasonCode,
        string? humanStatement,
        string? sourceReference,
        Guid? author,
        DateTimeOffset? validity,
        string confidentialityLevel,
        IReadOnlyList<SalesOrderLine> lines)
    {
        var order = new SalesOrder(
            id, tenantId, customerId, status, createdAt,
            reasonCode, humanStatement, sourceReference, author, validity, confidentialityLevel);

        order._lines.AddRange(lines);
        if (order._lines.Count > 0)
        {
            order.RecalculateTotal();
        }

        return order;
    }

    public void AddLine(string description, decimal quantity, Money unitPrice)
    {
        // Invariante 4 (ADR-0009, ajuste 3) — mesma moeda em toda linha.
        if (_lines.Count > 0 && _lines[0].UnitPrice.Currency != unitPrice.Currency)
        {
            throw new InvalidOperationException(
                $"Todas as linhas de um SalesOrder devem compartilhar a mesma moeda " +
                $"(esperado '{_lines[0].UnitPrice.Currency}', recebido '{unitPrice.Currency}').");
        }

        // Invariante 3 (ADR-0009) — tenant_id da linha é sempre o do
        // próprio pedido; não existe caminho para divergir, por construção.
        var line = new SalesOrderLine(Guid.NewGuid(), TenantId, _lines.Count + 1, description, quantity, unitPrice);
        _lines.Add(line);
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        // Invariante 2 (ADR-0008/0009) — cálculo decisório no próprio
        // Aggregate, nunca aceito como input externo nem delegado à
        // Projection Layer.
        var currency = _lines[0].UnitPrice.Currency;
        Total = _lines.Aggregate(Money.Zero(currency), (acc, line) => acc.Add(line.LineTotal));
    }
}
