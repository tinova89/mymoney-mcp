using Microsoft.AspNetCore.Http;
using MymoneyMcp.Api.Common;

namespace MymoneyMcp.Api.Tests.AccountGroup;

public sealed class HeaderAccountGroupContextTests
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
        Assert.Equal(string.Empty, new HeaderAccountGroupContext(new HttpContextAccessor()).Group);
    }

    private static HeaderAccountGroupContext ContextWith(string? header)
    {
        var httpContext = new DefaultHttpContext();
        if (header is not null)
        {
            httpContext.Request.Headers[HeaderAccountGroupContext.HeaderName] = header;
        }

        return new HeaderAccountGroupContext(new HttpContextAccessor { HttpContext = httpContext });
    }
}
