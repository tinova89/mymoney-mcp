using MymoneyMcp.Api.Auth;
using MymoneyMcp.Api.Common;
using MymoneyMcp.Application;
using MymoneyMcp.Application.Common.Interfaces;
using MymoneyMcp.Infrastructure.Auth;
using MymoneyMcp.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

AuthStartupGuard.Validate(builder.Configuration, builder.Environment);

builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.AddDataInfrastructure();
builder.Services.AddAuthInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountGroupContext, HeaderAccountGroupContext>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddTokenAuthentication(builder.Configuration);
builder.Services.AddOpenApi();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "mymoney-mcp", Version = "1.0.0" };

        // Advertise the tools capability even with no tools yet, so tools/list returns []
        // (contracts/mcp-endpoint.md). Tools from later features are added to this collection.
        options.ToolCollection = [];
    })
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

AuthStartupGuard.WarnIfDisabled(app.Configuration, app.Logger);
await app.Services.MigrateDatabaseAsync();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequireAccountGroupMiddleware>();

app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapAuthEndpoints();
app.MapMcp("/mcp");

await app.RunAsync();

public partial class Program;
