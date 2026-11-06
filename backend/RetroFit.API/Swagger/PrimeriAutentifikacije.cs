using RetroFit.API.Kontroleri;
using RetroFit.Application.Features.Autentifikacija;
using RetroFit.Application.Features.Korisnici;
using Swashbuckle.AspNetCore.Filters;

namespace RetroFit.API.Swagger;

// Primeri zahteva i odgovora koji se prikazuju u Swagger UI-ju (Swashbuckle.AspNetCore.Filters).

internal sealed class RegistracijaPrimer : IExamplesProvider<RegistracijaCommand>
{
    public RegistracijaCommand GetExamples() => new("petar@primer.rs", "SigurnaLozinka1", "Petar", "Perić", 1);
}

internal sealed class PrijavaPrimer : IExamplesProvider<PrijavaCommand>
{
    public PrijavaCommand GetExamples() => new("marko@retrofit.rs", "Lozinka123");
}

internal sealed class AutentifikacijaOdgovorPrimer : IExamplesProvider<AutentifikacijaOdgovor>
{
    public AutentifikacijaOdgovor GetExamples() => new(
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzIiwiaW1lIjoiTWFya28gUC4ifQ.potpis",
        new DateTime(2026, 11, 3, 12, 15, 0, DateTimeKind.Utc),
        new MojProfilDto
        {
            Id = 3,
            Email = "marko@retrofit.rs",
            Ime = "Marko",
            Prezime = "Petrović",
            PrikaznoIme = "Marko P.",
            GradId = 1,
            GradNaziv = "Beograd",
            Telefon = "+381641234567",
            TelefonVerifikovan = true,
            PrikaziTelefonKupcu = true,
            Uloge = ["Korisnik"],
            ProsecnaOcena = 4.8m,
            BrojRecenzija = 12,
            BrojZavrsenihTransakcija = 9,
            KreiranoUtc = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        });
}

internal sealed class PromeniLozinkuPrimer : IExamplesProvider<PromeniLozinkuCommand>
{
    public PromeniLozinkuCommand GetExamples() => new("Lozinka123", "NovaJakaLozinka2026");
}

internal sealed class VerifikacijaTelefonaPrimer : IExamplesProvider<ZatraziVerifikacijuTelefonaCommand>
{
    public ZatraziVerifikacijuTelefonaCommand GetExamples() => new("064 123 4567");
}
