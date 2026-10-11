using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MymoneyMcp.Application.Common.Behaviors;

namespace MymoneyMcp.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers, validators and the pipeline behaviors of this assembly.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
