using MymoneyMcp.Application.Auth.IssueToken;

namespace MymoneyMcp.Application.Tests.Auth;

public sealed class IssueTokenCommandValidatorTests
{
    private readonly IssueTokenCommandValidator validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_client_id_is_rejected(string clientId)
    {
        var result = validator.Validate(new IssueTokenCommand(clientId, "secret"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IssueTokenCommand.ClientId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_client_secret_is_rejected(string clientSecret)
    {
        var result = validator.Validate(new IssueTokenCommand("client", clientSecret));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IssueTokenCommand.ClientSecret));
    }

    [Fact]
    public void Client_id_longer_than_100_chars_is_rejected()
    {
        var result = validator.Validate(new IssueTokenCommand(new string('c', 101), "secret"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IssueTokenCommand.ClientId));
    }

    [Fact]
    public void Filled_fields_pass()
    {
        Assert.True(validator.Validate(new IssueTokenCommand("client", "secret")).IsValid);
    }

    [Fact]
    public void Error_messages_never_contain_the_secret()
    {
        var result = validator.Validate(new IssueTokenCommand(new string('c', 101), "super-secret-value"));

        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage.Contains("super-secret-value"));
    }

    [Fact]
    public void ToString_never_contains_the_secret()
    {
        Assert.DoesNotContain("super-secret-value", new IssueTokenCommand("client", "super-secret-value").ToString());
    }
}
