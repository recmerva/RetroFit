using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RetroFit.API.Konfiguracija;

internal static class SwaggerKonfiguracija
{
    public static IServiceCollection AddSwaggerDokumentacija(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerVerzije>();
        services.AddSwaggerGen(o =>
        {
            // XML komentari (/// <summary>) iz kontrolera postaju opisi endpointa u Swagger-u.
            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml))
            {
                o.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }

            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT access token. Dobija se pozivom POST /api/v1/autentifikacija/prijava. Unesite samo token (bez reči 'Bearer').",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });

            // Primeri zahteva i odgovora (klase koje implementiraju IExamplesProvider<T>).
            o.ExampleFilters();
            o.SupportNonNullableReferenceTypes();
            o.CustomSchemaIds(t => t.FullName?.Replace("RetroFit.", string.Empty).Replace('+', '.'));
        });
        services.AddSwaggerExamplesFromAssemblyOf<Program>();
        return services;
    }

    /// <summary>Po jedan Swagger dokument za svaku verziju API-ja.</summary>
    private sealed class SwaggerVerzije(IApiVersionDescriptionProvider verzije) : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var opis in verzije.ApiVersionDescriptions)
            {
                options.SwaggerDoc(opis.GroupName, new OpenApiInfo
                {
                    Title = "RetroFit API",
                    Version = opis.ApiVersion.ToString(),
                    Description = """
                                  REST API za RetroFit - marketplace za polovne stvari (projekat iz Naprednih baza podataka).

                                  **Autentifikacija:** JWT access token (15 min) + refresh token u httpOnly cookie-ju (rotacija pri svakom osvežavanju).

                                  **Greške:** svi odgovori sa greškom su u ProblemDetails formatu (RFC 7807) sa poljima `kod` i `traceId`.

                                  **Bot zaštita:** zahtevi prolaze kroz Bot Detection middleware; sumnjivi klijenti dobijaju `428` (potreban JS izazov) ili `429`.
                                  """,
                    Contact = new OpenApiContact { Name = "RetroFit tim" }
                });
            }
        }
    }
}
