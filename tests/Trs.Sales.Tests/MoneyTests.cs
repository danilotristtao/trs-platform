using Trs.Sales;

namespace Trs.Sales.Tests;

public class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_Sums()
    {
        var a = Money.Create(10.50m, "BRL");
        var b = Money.Create(5.25m, "BRL");

        var result = a.Add(b);

        Assert.Equal(15.75m, result.Amount);
        Assert.Equal("BRL", result.Currency);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var a = Money.Create(10m, "BRL");
        var b = Money.Create(10m, "USD");

        Assert.Throws<InvalidOperationException>(() => a.Add(b));
    }

    [Fact]
    public void Create_NormalizesCurrencyToUpperInvariant()
    {
        var money = Money.Create(1m, "brl");

        Assert.Equal("BRL", money.Currency);
    }
}
