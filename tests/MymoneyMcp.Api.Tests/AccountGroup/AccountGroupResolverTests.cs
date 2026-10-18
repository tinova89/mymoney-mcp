using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using MymoneyMcp.Api.Common;
using MymoneyMcp.Application.Common;

namespace MymoneyMcp.Api.Tests.AccountGroup;

/// <summary>
/// 002 data-model.md resolution table and research R1: one per-request resolution from the raw
/// request target and the <c>X-Account-Group</c> header.
/// </summary>
public sealed class AccountGroupResolverTests
{
    [Theory]
    [InlineData("/mcp", "A", "A", AccountGroupSource.Header)]
    [InlineData("/mcp/", "A", "A", AccountGroupSource.Header)]
    [InlineData("/mcp/A", null, "A", AccountGroupSource.Url)]
    [InlineData("/mcp/A", " A ", "A", AccountGroupSource.Both)]
    [InlineData("/mcp/A", "   ", "A", AccountGroupSource.Url)]
    [InlineData("/mcp/%20A%20", null, "A", AccountGroupSource.Url)]
    [InlineData("/mcp/Fam%C3%ADlia", null, "Família", AccountGroupSource.Url)]
    [InlineData("/mcp/a%2Fb", null, "a/b", AccountGroupSource.Url)]
    [InlineData("/mcp/%252F", null, "%2F", AccountGroupSource.Url)]
    [InlineData("/mcp/A?x=1", null, "A", AccountGroupSource.Url)]
    [InlineData("/mcp/A/", null, "A", AccountGroupSource.Url)]
    [InlineData("/MCP/A", null, "A", AccountGroupSource.Url)]
    public void Resolves_the_group_and_its_source(string rawTarget, string? header, string group, AccountGroupSource source)
    {
        var resolution = AccountGroupResolver.Resolve(rawTarget, header);

        Assert.Equal(AccountGroupError.None, resolution.Error);
        Assert.Equal(group, resolution.Group);
        Assert.Equal(source, resolution.Source);
    }

    [Theory]
    [InlineData("/mcp", null, AccountGroupError.Missing)]
    [InlineData("/mcp/", null, AccountGroupError.Missing)]
    [InlineData("/mcp", "   ", AccountGroupError.Missing)]
    [InlineData("/mcp/%20", null, AccountGroupError.BlankUrlGroup)]
    [InlineData("/mcp/%20", "A", AccountGroupError.BlankUrlGroup)]
    [InlineData("/mcp/A", "B", AccountGroupError.Mismatch)]
    [InlineData("/mcp/HOME", "home", AccountGroupError.Mismatch)]
    public void Rejects_missing_blank_and_mismatched_groups(string rawTarget, string? header, AccountGroupError error)
    {
        var resolution = AccountGroupResolver.Resolve(rawTarget, header);

        Assert.Equal(error, resolution.Error);
        Assert.Equal(string.Empty, resolution.Group);
        Assert.Equal(AccountGroupSource.None, resolution.Source);
    }

    [Theory]
    [InlineData("/auth/HOME")]
    [InlineData("/health/HOME")]
    [InlineData("/mcpx/HOME")]
    [InlineData("/mcp/A/B")]
    public void Only_a_single_segment_under_mcp_is_a_url_group(string rawTarget)
    {
        Assert.Equal(AccountGroupError.Missing, AccountGroupResolver.Resolve(rawTarget, header: null).Error);
        Assert.Equal(AccountGroupSource.Header, AccountGroupResolver.Resolve(rawTarget, "HOME").Source);
    }

    [Fact]
    public void Reads_the_raw_target_not_the_decoded_path()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/mcp/a%2Fb";
        context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/mcp/a%2Fb";

        Assert.Equal("a/b", AccountGroupResolver.Resolve(context.Request).Group);
    }

    [Fact]
    public void Falls_back_to_the_path_when_there_is_no_raw_target()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/mcp/A";

        Assert.Equal("A", AccountGroupResolver.Resolve(context.Request).Group);
    }

    [Fact]
    public void Resolution_is_kept_for_the_request_only()
    {
        var first = new DefaultHttpContext();
        first.Request.Path = "/mcp/A";
        var second = new DefaultHttpContext();
        second.Request.Path = "/mcp/B";

        Assert.Same(AccountGroupResolver.GetOrResolve(first), AccountGroupResolver.GetOrResolve(first));
        Assert.Equal("B", AccountGroupResolver.GetOrResolve(second).Group);
    }
}
