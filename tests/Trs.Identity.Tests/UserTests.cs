using Trs.Identity;

namespace Trs.Identity.Tests;

public class UserTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static EmailAddress ValidEmail => EmailAddress.Create("danilo@example.com");

    [Fact]
    public void Create_WithRoutineCreation_DoesNotRequireHumanStatement()
    {
        var user = User.Create(TenantId, "ext-ref-1", ValidEmail, "Danilo", UserRole.Member);

        Assert.Equal(ReasonCode.RoutineCreation, user.ReasonCode);
        Assert.Null(user.HumanStatement);
    }

    [Theory]
    [InlineData(ReasonCode.ManualOverride)]
    [InlineData(ReasonCode.ExceptionApproval)]
    [InlineData(ReasonCode.Correction)]
    public void Create_WithNonRoutineReasonCode_WithoutHumanStatement_Throws(ReasonCode reasonCode)
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(TenantId, "ext-ref-1", ValidEmail, "Danilo", UserRole.Member, reasonCode));
    }

    [Theory]
    [InlineData(ReasonCode.ManualOverride)]
    [InlineData(ReasonCode.ExceptionApproval)]
    [InlineData(ReasonCode.Correction)]
    public void Create_WithNonRoutineReasonCode_AndHumanStatement_Succeeds(ReasonCode reasonCode)
    {
        var user = User.Create(
            TenantId, "ext-ref-1", ValidEmail, "Danilo", UserRole.Member,
            reasonCode, humanStatement: "Ajuste manual solicitado pelo suporte.");

        Assert.Equal(reasonCode, user.ReasonCode);
    }

    [Fact]
    public void Create_WithoutTenantId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(Guid.Empty, "ext-ref-1", ValidEmail, "Danilo", UserRole.Member));
    }

    [Fact]
    public void Deactivate_WhenAlreadyDeactivated_Throws()
    {
        var user = User.Create(TenantId, "ext-ref-1", ValidEmail, "Danilo", UserRole.Member);
        user.Deactivate();

        Assert.Throws<InvalidOperationException>(() => user.Deactivate());
    }
}
