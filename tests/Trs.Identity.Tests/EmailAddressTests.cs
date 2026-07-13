using Trs.Identity;

namespace Trs.Identity.Tests;

public class EmailAddressTests
{
    [Theory]
    [InlineData("danilo@example.com")]
    [InlineData("a.b+c@sub.example.com")]
    public void Create_WithValidFormat_Succeeds(string value)
    {
        var email = EmailAddress.Create(value);

        Assert.Equal(value, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba.com")]
    [InlineData("a@b")]
    [InlineData("a b@example.com")]
    public void Create_WithInvalidFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = EmailAddress.Create("danilo@example.com");
        var second = EmailAddress.Create("danilo@example.com");

        Assert.Equal(first, second);
    }
}
