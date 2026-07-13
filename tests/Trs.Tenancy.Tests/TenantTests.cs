using Trs.Tenancy;

namespace Trs.Tenancy.Tests;

public class TenantTests
{
    [Fact]
    public void Create_WithValidName_StartsActive()
    {
        var tenant = Tenant.Create("Acme Ltda");

        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_Throws(string invalidName)
    {
        Assert.Throws<ArgumentException>(() => Tenant.Create(invalidName));
    }

    [Fact]
    public void Suspend_WhenActive_TransitionsToSuspended()
    {
        var tenant = Tenant.Create("Acme Ltda");

        tenant.Suspend();

        Assert.Equal(TenantStatus.Suspended, tenant.Status);
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_Throws()
    {
        var tenant = Tenant.Create("Acme Ltda");
        tenant.Suspend();

        Assert.Throws<InvalidOperationException>(() => tenant.Suspend());
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_Throws()
    {
        var tenant = Tenant.Create("Acme Ltda");

        Assert.Throws<InvalidOperationException>(() => tenant.Reactivate());
    }
}
