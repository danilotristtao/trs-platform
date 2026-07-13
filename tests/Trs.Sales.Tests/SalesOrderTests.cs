using Trs.Sales;

namespace Trs.Sales.Tests;

public class SalesOrderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static SalesOrder CreateOrderWithOneLine() =>
        SalesOrder.Create(
            TenantId,
            CustomerId,
            new[] { ("Item A", 2m, Money.Create(10m, "BRL")) });

    [Fact]
    public void Create_WithoutLines_Throws()
    {
        // Invariante 1 (ADR-0009) — ao menos uma SalesOrderLine para ser válido.
        Assert.Throws<ArgumentException>(() =>
            SalesOrder.Create(TenantId, CustomerId, Array.Empty<(string, decimal, Money)>()));
    }

    [Fact]
    public void Create_WithOneLine_CalculatesTotalFromLines()
    {
        // Invariante 2 (ADR-0008/0009) — total é cálculo decisório do
        // Aggregate, nunca aceito como input externo.
        var order = SalesOrder.Create(
            TenantId,
            CustomerId,
            new[]
            {
                ("Item A", 2m, Money.Create(10m, "BRL")),
                ("Item B", 1m, Money.Create(5m, "BRL")),
            });

        Assert.Equal(25m, order.Total.Amount);
        Assert.Equal("BRL", order.Total.Currency);
    }

    [Fact]
    public void AddLine_AllLinesBelongToOrderTenant()
    {
        // Invariante 3 (ADR-0009) — tenant_id de toda linha idêntico ao
        // do pedido; garantido por construção (AddLine sempre usa TenantId
        // do próprio Aggregate).
        var order = CreateOrderWithOneLine();

        order.AddLine("Item B", 1m, Money.Create(5m, "BRL"));

        Assert.All(order.Lines, line => Assert.Equal(TenantId, line.TenantId));
    }

    [Fact]
    public void AddLine_WithDifferentCurrency_Throws()
    {
        // Invariante 4 (ADR-0009, ajuste 3) — mesma moeda em toda linha
        // do pedido; teste unitário obrigatório exigido explicitamente
        // pelo ADR.
        var order = CreateOrderWithOneLine();

        Assert.Throws<InvalidOperationException>(() =>
            order.AddLine("Item em outra moeda", 1m, Money.Create(10m, "USD")));
    }

    [Fact]
    public void Create_WithoutCustomerId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SalesOrder.Create(
                TenantId,
                Guid.Empty,
                new[] { ("Item A", 1m, Money.Create(10m, "BRL")) }));
    }
}
