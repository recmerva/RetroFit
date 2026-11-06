using MediatR;
using Microsoft.Extensions.Logging;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Odrzavanje;

// Komande koje periodično pokreće pozadinski posao (Infrastructure/PozadinskiPoslovi).
// Svaka obrađuje ograničen broj redova po pokretanju (batch), da jedan prolaz ne traje predugo.

public sealed record IstekniOglaseCommand(int Maks = 500) : IRequest<int>;

internal sealed class IstekniOglaseHandler(IUnitOfWork uow, IObavestenjaServis obavestenja, IDatumVreme vreme, ILogger<IstekniOglaseHandler> logger)
    : IRequestHandler<IstekniOglaseCommand, int>
{
    public async Task<int> Handle(IstekniOglaseCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var oglasi = await uow.Oglasi.ZaIstekAsync(sada, z.Maks, ct);
        foreach (var oglas in oglasi)
        {
            // Prelaz statusa ide kroz domensku metodu - ista pravila kao i u ostatku aplikacije.
            oglas.Istekni(sada);
            await obavestenja.ObavestiAsync(oglas.ProdavacId, TipObavestenja.OglasIstekao, "Oglas je istekao",
                $"Oglas \"{oglas.Naslov}\" je istekao posle 60 dana. Možete ga obnoviti jednim klikom.", $"/oglasi/{oglas.Id}", ct);

            foreach (var ponuda in await uow.Ponude.NaCekanjuZaOglasAsync(oglas.Id, ct))
            {
                ponuda.OdbijAutomatski(sada);
            }
        }

        if (oglasi.Count > 0)
        {
            await uow.SacuvajAsync(ct);
            logger.LogInformation("Isteklo {Broj} oglasa.", oglasi.Count);
        }

        return oglasi.Count;
    }
}

public sealed record IstekniPonudeCommand(int Maks = 500) : IRequest<int>;

internal sealed class IstekniPonudeHandler(IUnitOfWork uow, IObavestenjaServis obavestenja, IDatumVreme vreme, ILogger<IstekniPonudeHandler> logger)
    : IRequestHandler<IstekniPonudeCommand, int>
{
    public async Task<int> Handle(IstekniPonudeCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var ponude = await uow.Ponude.IstekleAsync(sada, z.Maks, ct);
        foreach (var ponuda in ponude)
        {
            ponuda.Istekni(sada);
            await obavestenja.ObavestiAsync(ponuda.PonudioKorisnikId, TipObavestenja.PonudaIstekla, "Ponuda je istekla",
                $"Vaša ponuda od {ponuda.Iznos:N0} RSD je istekla bez odgovora.", $"/razgovori/{ponuda.RazgovorId}", ct);
        }

        if (ponude.Count > 0)
        {
            await uow.SacuvajAsync(ct);
            logger.LogInformation("Isteklo {Broj} ponuda.", ponude.Count);
        }

        return ponude.Count;
    }
}

public sealed record IstekniRezervacijeCommand(int Maks = 200) : IRequest<int>;

internal sealed class IstekniRezervacijeHandler(IUnitOfWork uow, IObavestenjaServis obavestenja, IDatumVreme vreme,
    ILogger<IstekniRezervacijeHandler> logger) : IRequestHandler<IstekniRezervacijeCommand, int>
{
    public async Task<int> Handle(IstekniRezervacijeCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var rezervacije = await uow.Rezervacije.IstekleAsync(sada, z.Maks, ct);
        foreach (var r in rezervacije)
        {
            // Rezervacija ističe -> oglas se vraća u Aktivan i ponovo je vidljiv u pretrazi.
            r.Istekni(r.Oglas, sada);
            const string poruka = "Rezervacija je istekla jer kupovina nije pokrenuta u roku od 72 sata. Oglas je ponovo aktivan.";
            await obavestenja.ObavestiAsync(r.KupacId, TipObavestenja.RezervacijaIstekla, "Rezervacija je istekla", poruka, $"/oglasi/{r.OglasId}", ct);
            await obavestenja.ObavestiAsync(r.ProdavacId, TipObavestenja.RezervacijaIstekla, "Rezervacija je istekla", poruka, $"/oglasi/{r.OglasId}", ct);
        }

        if (rezervacije.Count > 0)
        {
            await uow.SacuvajAsync(ct);
            logger.LogInformation("Isteklo {Broj} rezervacija.", rezervacije.Count);
        }

        return rezervacije.Count;
    }
}

/// <summary>
/// Obaveštenja o novim oglasima za sačuvane pretrage. Za svaku pretragu traže se oglasi objavljeni
/// POSLE poslednje provere, pa korisnik nikad ne dobije dva obaveštenja za isti oglas.
/// </summary>
public sealed record ObavestiSacuvanePretrageCommand(int Maks = 200) : IRequest<int>;

internal sealed class ObavestiSacuvanePretrageHandler(IUnitOfWork uow, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<ObavestiSacuvanePretrageCommand, int>
{
    public async Task<int> Handle(ObavestiSacuvanePretrageCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var pretrage = await uow.SacuvanePretrage.ZaObavestavanjeAsync(sada.AddMinutes(-1), z.Maks, ct);
        var poslato = 0;
        foreach (var p in pretrage)
        {
            IReadOnlyList<int>? kategorije = p.KategorijaId is null ? null : await uow.Kategorije.IdSaPodkategorijamaAsync(p.KategorijaId.Value, ct);
            var novi = await uow.Oglasi.NoviOglasiZaPretraguAsync(p, kategorije, p.PoslednjaProveraUtc, sada, 20, ct);
            if (novi.Count > 0)
            {
                var tekst = novi.Count == 1 ? "1 novi oglas" : $"{novi.Count} novih oglasa";
                await obavestenja.ObavestiAsync(p.KorisnikId, TipObavestenja.PoklapanjePretrage, $"Nova poklapanja: {p.Naziv}",
                    $"Pronađeno je {tekst} za vašu sačuvanu pretragu \"{p.Naziv}\".",
                    novi.Count == 1 ? $"/oglasi/{novi[0]}" : $"/oglasi?sacuvanaPretraga={p.Id}", ct);
                poslato++;
            }

            p.OznaciProvereno(sada);
        }

        if (pretrage.Count > 0)
        {
            await uow.SacuvajAsync(ct);
        }

        return poslato;
    }
}

public sealed record OcistiTokeneCommand : IRequest<int>;

internal sealed class OcistiTokeneHandler(IUnitOfWork uow, IDatumVreme vreme) : IRequestHandler<OcistiTokeneCommand, int>
{
    // Istekli refresh tokeni se čuvaju još 30 dana (za detekciju ponovne upotrebe i audit), pa se brišu.
    public Task<int> Handle(OcistiTokeneCommand z, CancellationToken ct) => uow.RefreshTokeni.ObrisiZastareleAsync(vreme.SadaUtc.AddDays(-30), ct);
}
