namespace Trs.Sales;

// Value Object (ADR-0006), record conforme ADR-0010. Só é somável
// dentro da mesma moeda (ADR-0009, ajuste 3) — a própria API impede
// somar valores de moedas diferentes, em vez de confiar em quem chama.
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("currency é obrigatória.", nameof(currency));
        }

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    public static Money Zero(string currency) => Create(0m, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Não é possível somar valores em moedas diferentes ({Currency} e {other.Currency}).");
        }

        return Create(Amount + other.Amount, Currency);
    }
}
