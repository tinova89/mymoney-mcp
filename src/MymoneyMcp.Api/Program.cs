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
builder.Services.AddScoped<IAccountGroupContext, RequestAccountGroupContext>();
builder.Services.AddScoped<ICallerContext, HttpCallerContext>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddTokenAuthentication(builder.Configuration);
builder.Services.AddOpenApi();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "mymoney-mcp", Version = "1.0.0" };
    })
    // Stateless: no session can carry a group from one call to the next (002 FR-007, research R3).
    .WithHttpTransport(options => options.Stateless = true)
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

// One connector per group for clients that cannot send headers (002 FR-001, research R2).
app.MapMcp("/mcp/{accountGroup}");

await app.RunAsync();

public partial class Program;
