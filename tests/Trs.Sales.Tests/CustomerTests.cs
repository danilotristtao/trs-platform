using Trs.Sales;

namespace Trs.Sales.Tests;

public class CustomerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithRoutineCreation_DoesNotRequireHumanStatement()
    {
        var customer = Customer.Create(TenantId, "Acme Ltda", TaxId.Create("12.345.678/0001-90"));

        Assert.Null(customer.HumanStatement);
    }

    [Fact]
    public void Create_WithNonRoutineReasonCode_WithoutHumanStatement_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(TenantId, "Acme Ltda", TaxId.Create("12.345.678/0001-90"), ReasonCode.Correction));
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var customer = Customer.Create(TenantId, "Acme Ltda", TaxId.Create("12.345.678/0001-90"));
        customer.Deactivate();

        Assert.Throws<InvalidOperationException>(() => customer.Deactivate());
    }
}
