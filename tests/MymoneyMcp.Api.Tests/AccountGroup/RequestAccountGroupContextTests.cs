using Microsoft.AspNetCore.Http;
using MymoneyMcp.Api.Common;
using MymoneyMcp.Application.Common;

namespace MymoneyMcp.Api.Tests.AccountGroup;

public sealed class RequestAccountGroupContextTests
{
    [Fact]
    public void Value_is_trimmed()
    {
        Assert.Equal("HOME", ContextWith("  HOME  ").Group);
    }

    [Fact]
    public void Letter_case_is_kept_so_matching_stays_case_sensitive()
    {
        Assert.Equal("home", ContextWith("home").Group);
        Assert.NotEqual(ContextWith("HOME").Group, ContextWith("home").Group);
    }

    [Fact]
    public void Missing_header_is_an_empty_string()
    {
        Assert.Equal(string.Empty, ContextWith(null).Group);
    }

    [Fact]
    public void No_http_request_is_an_empty_string()
    {
        Assert.Equal(string.Empty, new RequestAccountGroupContext(new HttpContextAccessor()).Group);
    }

    [Fact]
    public void Source_is_url_for_a_group_url_and_header_for_the_plain_url()
    {
        Assert.Equal(AccountGroupSource.Url, ContextWith(null, "/mcp/A").Source);
        Assert.Equal(AccountGroupSource.Header, ContextWith("A", "/mcp").Source);
    }

    [Fact]
    public void A_rejected_group_is_empty_with_no_source()
    {
        var context = ContextWith("B", "/mcp/A");

        Assert.Equal(string.Empty, context.Group);
        Assert.Equal(AccountGroupSource.None, context.Source);
    }

    private static RequestAccountGroupContext ContextWith(string? header, string path = "/mcp")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        if (header is not null)
        {
            httpContext.Request.Headers[RequestAccountGroupContext.HeaderName] = header;
        }

        return new RequestAccountGroupContext(new HttpContextAccessor { HttpContext = httpContext });
    }
}
