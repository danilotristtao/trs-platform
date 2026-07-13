namespace Trs.Sales;

// Value Object (ADR-0006). Unicidade por tenant é responsabilidade do
// Repository/banco (UNIQUE(tenant_id, tax_id) em migrations/0001_init.sql)
// — o Aggregate só garante formato não vazio.
public sealed record TaxId
{
    public string Value { get; }

    private TaxId(string value)
    {
        Value = value;
    }

    public static TaxId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("tax_id é obrigatório.", nameof(value));
        }

        return new TaxId(value.Trim());
    }

    public override string ToString() => Value;
}
