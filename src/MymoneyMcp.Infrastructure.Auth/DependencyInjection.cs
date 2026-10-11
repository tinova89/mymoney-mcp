using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Infrastructure.Auth;

public static class DependencyInjection
{
    /// <summary>Registers token issuance (research R7). Token validation lives in the Api.</summary>
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IClientCredentialStore, ClientCredentialStore>();

        return services;
    }
}
