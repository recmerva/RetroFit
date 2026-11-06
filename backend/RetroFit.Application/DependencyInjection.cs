using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Ponasanja;
using RetroFit.Application.Common.Servisi;

namespace RetroFit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var sklop = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(sklop);
            // Redosled je bitan: prvo logovanje (meri i neuspešnu validaciju), pa validacija, pa handler.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LogovanjePonasanje<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidacijaPonasanje<,>));
        });

        services.AddValidatorsFromAssembly(sklop, includeInternalTypes: true);
        services.AddAutoMapper(sklop);
        services.AddScoped<IObavestenjaServis, ObavestenjaServis>();
        services.AddScoped<Features.Autentifikacija.IzdavanjeTokena>();

        return services;
    }
}
