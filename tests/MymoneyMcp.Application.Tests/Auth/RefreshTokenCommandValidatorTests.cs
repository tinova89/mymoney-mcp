using MymoneyMcp.Application.Auth.RefreshToken;

namespace MymoneyMcp.Application.Tests.Auth;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_refresh_token_is_rejected(string refreshToken)
    {
        var result = validator.Validate(new RefreshTokenCommand(refreshToken));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }

    [Fact]
    public void Filled_refresh_token_passes()
    {
        Assert.True(validator.Validate(new RefreshTokenCommand("opaque")).IsValid);
    }

    [Fact]
    public void ToString_never_contains_the_token()
    {
        Assert.DoesNotContain("opaque-token-value", new RefreshTokenCommand("opaque-token-value").ToString());
    }
}
