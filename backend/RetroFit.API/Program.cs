using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using RetroFit.API.Konfiguracija;
using RetroFit.API.Middleware;
using RetroFit.API.Middleware.BotDetekcija;
using RetroFit.Application;
using RetroFit.Infrastructure;
using RetroFit.Infrastructure.Persistencija.Seed;
using RetroFit.Infrastructure.Servisi;
using Serilog;

// Serilog "bootstrap" logger: hvata i greške koje se dese pre nego što se aplikacija potpuno podigne.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Strukturisano logovanje (Serilog): podešavanja iz appsettings.json (sekcija "Serilog").
    builder.Host.UseSerilog((kontekst, servisi, log) => log
        .ReadFrom.Configuration(kontekst.Configuration)
        .ReadFrom.Services(servisi)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Aplikacija", "RetroFit.API"));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApi(builder.Configuration, builder.Environment);

    var app = builder.Build();

    // ---------------- HTTP pipeline (redosled je bitan) ----------------
    app.UseForwardedHeaders();
    app.UseMiddleware<KorelacioniIdMiddleware>();

    // Logovanje zahteva je SPOLJA u odnosu na exception handler, da bi beležilo stvarni status odgovora
    // (npr. 401 iz ProblemDetails), a ne 500 koji bi video pre nego što handler obradi izuzetak.
    app.UseSerilogRequestLogging(o =>
    {
        o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} odgovorio {StatusCode} za {Elapsed:0} ms";
        o.EnrichDiagnosticContext = (dijagnostika, http) =>
        {
            dijagnostika.Set("KorisnikId", http.User.FindFirst("sub")?.Value ?? "anoniman");
            dijagnostika.Set("IpAdresa", http.Connection.RemoteIpAddress?.ToString() ?? "-");
        };
    });

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseMiddleware<BezbednosnaZaglavljaMiddleware>();

    if (app.Configuration.GetValue("Swagger:Ukljucen", app.Environment.IsDevelopment()))
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            foreach (var opis in app.DescribeApiVersions())
            {
                o.SwaggerEndpoint($"/swagger/{opis.GroupName}/swagger.json", $"RetroFit API {opis.GroupName}");
            }

            o.DocumentTitle = "RetroFit API";
        });
    }

    // Otpremljene slike oglasa se serviraju kao statički fajlovi.
    var skladiste = app.Services.GetRequiredService<IOptions<SkladisteOpcije>>().Value;
    var folderSlika = Path.GetFullPath(skladiste.Putanja);
    Directory.CreateDirectory(folderSlika);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(folderSlika),
        RequestPath = skladiste.JavniPrefiks,
        OnPrepareResponse = c => c.Context.Response.Headers.CacheControl = "public,max-age=604800"
    });

    app.UseCors(ApiServisi.CorsPolitika);
    app.UseResponseCaching();
    app.UseAuthentication();
    // Bot Detection posle autentifikacije (zna Id korisnika), a pre autorizacije i kontrolera.
    app.UseMiddleware<BotDetekcijaMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("spremnost") });

    await InicijalizacijaBaze.InicijalizujAsync(app.Services, app.Configuration);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "API se nije pokrenuo.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Eksplicitna deklaracija (potrebna za testove i Swagger primere).</summary>
public partial class Program;
