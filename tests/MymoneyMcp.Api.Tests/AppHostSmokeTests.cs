extern alias AppHost;

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests;

/// <summary>
/// Spec US4 / FR-011: the AppHost starts PostgreSQL and the API with one command (research R12).
/// </summary>
public sealed class AppHostSmokeTests
{
    [Fact]
    public async Task AppHost_starts_the_database_and_the_api_reports_healthy()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHost::Projects.MymoneyMcp_AppHost>(ct);

        // The signing key normally comes from user-secrets; tests pass it as an environment variable.
        var api = builder.Resources.OfType<ProjectResource>().Single(r => r.Name == "mymoneymcp-api");
        builder.CreateResourceBuilder(api).WithEnvironment("Jwt__SigningKey", ApiFactory.SigningKey);

        await using var app = await builder.BuildAsync(ct);
        await app.StartAsync(ct);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        await app.ResourceNotifications.WaitForResourceHealthyAsync("mymoneymcp-api", timeout.Token);

        var response = await app.CreateHttpClient("mymoneymcp-api").GetAsync("/health", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
