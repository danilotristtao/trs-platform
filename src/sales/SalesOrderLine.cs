namespace Trs.Sales;

// Entity interna do Aggregate SalesOrder (ADR-0006) — sem Repository
// próprio; construída apenas através de SalesOrder (construtor
// `internal`), nunca diretamente por código fora do Aggregate.
public sealed class SalesOrderLine
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public int LineNumber { get; }
    public string Description { get; }
    public decimal Quantity { get; }
    public Money UnitPrice { get; }

    // Construtor único e explícito (id incluído) para que o EF Core
    // (ADR-0011) tenha exatamente um candidato de "constructor binding"
    // ao reidratar a partir do banco — duas sobrecargas geraria
    // ambiguidade na materialização.
    internal SalesOrderLine(Guid id, Guid tenantId, int lineNumber, string description, decimal quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("quantity deve ser maior que zero.", nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("description é obrigatória.", nameof(description));
        }

        Id = id;
        TenantId = tenantId;
        LineNumber = lineNumber;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money LineTotal => Money.Create(Quantity * UnitPrice.Amount, UnitPrice.Currency);
}
