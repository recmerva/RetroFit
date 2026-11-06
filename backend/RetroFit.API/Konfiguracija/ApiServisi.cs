using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using RetroFit.API.Kontroleri;
using RetroFit.API.Middleware;
using RetroFit.API.Middleware.BotDetekcija;
using RetroFit.API.Servisi;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Domain.Pravila;
using RetroFit.Infrastructure.Identitet;
using RetroFit.Infrastructure.Persistencija;

namespace RetroFit.API.Konfiguracija;

/// <summary>Podešavanja refresh-token cookie-ja.</summary>
public sealed class KolacicOpcije
{
    public const string Sekcija = "Kolacic";
    public string Naziv { get; set; } = "retrofit_rt";
    public string Putanja { get; set; } = "/api/v1/autentifikacija";
    public bool Secure { get; set; } = true;
}

internal static class ApiServisi
{
    public const string CorsPolitika = "Frontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration konfiguracija, IWebHostEnvironment okruzenje)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITrenutniKorisnik, TrenutniKorisnik>();
        services.Configure<KolacicOpcije>(konfiguracija.GetSection(KolacicOpcije.Sekcija));

        services.AddControllers()
            .AddJsonOptions(o =>
            {
                // Enumi kao tekst ("Aktivan" umesto 1) - čitljivije za frontend i Swagger.
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Ograničenje dubine JSON-a (zaštita od DoS-a preko duboko ugnježdenih objekata).
                o.JsonSerializerOptions.MaxDepth = 32;
            });

        // Greške pri čitanju zahteva (neispravan JSON, pogrešan tip polja) u ISTOM formatu kao ostale greške,
        // na srpskom i bez otkrivanja internih naziva klasa.
        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = kontekst =>
        {
            var greske = kontekst.ModelState
                .Where(s => s.Value?.Errors.Count > 0)
                .GroupBy(s =>
                {
                    var kljuc = s.Key.TrimStart('$', '.');
                    return kljuc.Length <= 1 ? "zahtev" : char.ToLowerInvariant(kljuc[0]) + kljuc[1..];
                })
                .ToDictionary(g => g.Key, g => g.SelectMany(s => s.Value!.Errors)
                    .Select(e => e.Exception is not null || e.ErrorMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase)
                        ? "Vrednost nije u ispravnom formatu."
                        : e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase)
                            ? "Polje je obavezno."
                            : e.ErrorMessage)
                    .Distinct().ToArray());
            greske.Remove("zahtev", out var opsteGreske);
            if (greske.Count == 0 && opsteGreske is not null)
            {
                greske["zahtev"] = ["Telo zahteva je obavezno i mora biti ispravan JSON (UTF-8)."];
            }

            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Neispravni podaci",
                Detail = "Zahtev nije moguće pročitati.",
                Type = "https://retrofit.rs/greske/validacija",
                Instance = kontekst.HttpContext.Request.Path
            };
            problem.Extensions["kod"] = "validacija";
            problem.Extensions["greske"] = greske;
            problem.Extensions["traceId"] = kontekst.HttpContext.TraceIdentifier;
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        });

        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;

            // Odgovori bez tela iz ASP.NET pipeline-a (npr. 401 od JWT middleware-a) dobijaju srpski opis i "kod".
            if (!ctx.ProblemDetails.Extensions.ContainsKey("kod"))
            {
                var (naslov, detalj, kod) = ctx.ProblemDetails.Status switch
                {
                    401 => ("Neovlašćen pristup", "Morate biti prijavljeni (nedostaje ili je istekao access token).", "neovlascen"),
                    403 => ("Zabranjeno", "Nemate pravo pristupa ovom resursu.", "zabranjeno"),
                    404 => ("Nije pronađeno", "Traženi resurs ne postoji.", "nije_pronadjeno"),
                    405 => ("Metoda nije dozvoljena", "HTTP metoda nije podržana za ovu adresu.", "metoda_nije_dozvoljena"),
                    415 => ("Nepodržan format", "Telo zahteva mora biti application/json.", "nepodrzan_format"),
                    _ => ((string?)null, (string?)null, (string?)null)
                };

                if (kod is not null)
                {
                    ctx.ProblemDetails.Title = naslov;
                    ctx.ProblemDetails.Detail ??= detalj;
                    ctx.ProblemDetails.Type = $"https://retrofit.rs/greske/{kod}";
                    ctx.ProblemDetails.Extensions["kod"] = kod;
                }
            }
        });
        services.AddExceptionHandler<GlobalniExceptionHandler>();

        // API verzionisanje kroz URL segment: /api/v1/...
        services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ReportApiVersions = true;
                o.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'V";
                o.SubstituteApiVersionInUrl = true;
            });

        DodajAutentifikaciju(services, konfiguracija);
        services.AddBotDetekcija(konfiguracija);
        services.AddSwaggerDokumentacija();

        var dozvoljeniIzvori = konfiguracija.GetSection("Cors:DozvoljeniIzvori").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(CorsPolitika, p => p
            .WithOrigins(dozvoljeniIzvori)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders(KorelacioniIdMiddleware.Zaglavlje, "Retry-After")));

        // Iza reverse proxy-ja (nginx u Docker-u) prava IP adresa klijenta stiže u X-Forwarded-For.
        // Veruje se SAMO proxy-jima iz konfiguracije - inače bi napadač mogao da lažira svoju IP adresu.
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.ForwardLimit = 1;
            foreach (var mreza in konfiguracija.GetSection("ReverseProxy:PouzdaneMreze").Get<string[]>() ?? [])
            {
                if (System.Net.IPNetwork.TryParse(mreza, out var ip))
                {
                    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip.BaseAddress, ip.PrefixLength));
                }
            }
        });

        services.AddHealthChecks().AddDbContextCheck<RetroFitDbContext>("baza", tags: ["spremnost"]);
        services.AddResponseCaching();

        return services;
    }

    private static void DodajAutentifikaciju(IServiceCollection services, IConfiguration konfiguracija)
    {
        var jwt = konfiguracija.GetSection(JwtOpcije.Sekcija).Get<JwtOpcije>() ?? new JwtOpcije();
        if (Encoding.UTF8.GetByteCount(jwt.Kljuc) < 32)
        {
            throw new InvalidOperationException("Jwt:Kljuc mora imati najmanje 32 bajta (256 bita).");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Izdavac,
                    ValidateAudience = true,
                    ValidAudience = jwt.Publika,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Kljuc)),
                    ValidateLifetime = true,
                    // Mala tolerancija razlike satova između servera.
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = "sub",
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politike.Moderacija, p => p.RequireRole(NaziviUloga.Moderator, NaziviUloga.Admin))
            .AddPolicy(Politike.Administracija, p => p.RequireRole(NaziviUloga.Admin));
    }
}
