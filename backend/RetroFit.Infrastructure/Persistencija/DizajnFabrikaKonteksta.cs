using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RetroFit.Infrastructure.Persistencija;

/// <summary>
/// Koristi je samo alat "dotnet ef" (dizajn-vreme) za kreiranje migracija, bez pokretanja API-ja.
/// Konekcioni string se čita iz promenljive okruženja RETROFIT_DB, a podrazumevano je lokalni SQL Server.
/// </summary>
public sealed class DizajnFabrikaKonteksta : IDesignTimeDbContextFactory<RetroFitDbContext>
{
    public RetroFitDbContext CreateDbContext(string[] args)
    {
        var konekcija = Environment.GetEnvironmentVariable("RETROFIT_DB")
            ?? "Server=.;Database=RetroFit;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=False";

        var opcije = new DbContextOptionsBuilder<RetroFitDbContext>()
            .UseSqlServer(konekcija, sql =>
            {
                sql.UseNetTopologySuite();
                sql.MigrationsAssembly(typeof(RetroFitDbContext).Assembly.FullName);
            })
            .Options;

        return new RetroFitDbContext(opcije);
    }
}
