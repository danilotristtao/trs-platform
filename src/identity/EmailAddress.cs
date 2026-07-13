using System.Text.RegularExpressions;

namespace Trs.Identity;

// Value Object (ADR-0006) — sem identidade própria, imutável,
// igualdade por valor (record, conforme ADR-0010). Mesma regra de
// formato do CHECK constraint em migrations/0001_init.sql.
public sealed partial record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !EmailFormatRegex().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' não é um endereço de e-mail estruturalmente válido.", nameof(value));
        }

        return new EmailAddress(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormatRegex();
}
