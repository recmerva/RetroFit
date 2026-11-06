using System.Data;
using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Trgovina;

// =====================================================================
// REZERVACIJE
// =====================================================================

/// <summary>Prodavac direktno rezerviše oglas za kupca iz razgovora (po ceni iz oglasa).</summary>
public sealed record RezervisiZaKupcaCommand(int RazgovorId) : IRequest<RezervacijaDto>;

internal sealed class RezervisiZaKupcaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<RezervisiZaKupcaCommand, RezervacijaDto>
{
    public async Task<RezervacijaDto> Handle(RezervisiZaKupcaCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var id = await uow.UTransakcijiAsync(async t =>
        {
            var sada = vreme.SadaUtc;
            var razgovor = await uow.Razgovori.PronadjiSaOglasomAsync(z.RazgovorId, t);
            if (razgovor is null || !razgovor.JeUcesnik(korisnikId))
            {
                throw new NijePronadjenoIzuzetak("Razgovor", z.RazgovorId);
            }

            if (razgovor.ProdavacId != korisnikId)
            {
                throw new ZabranjenoIzuzetak("Samo prodavac može da rezerviše oglas za kupca.");
            }

            var oglas = razgovor.Oglas;
            var rezervacija = Rezervacija.Kreiraj(oglas, razgovor.KupacId, oglas.Cena, null, sada);
            await uow.Rezervacije.DodajAsync(rezervacija, t);
            foreach (var ponuda in await uow.Ponude.NaCekanjuZaOglasAsync(oglas.Id, t))
            {
                ponuda.OdbijAutomatski(sada);
            }

            razgovor.PosaljiPoruku(korisnikId, "Prodavac je rezervisao oglas za vas. Rezervacija važi 72 sata.", TipPoruke.Sistemska, sada);
            await uow.SacuvajAsync(t);
            await obavestenja.ObavestiAsync(razgovor.KupacId, TipObavestenja.RezervacijaKreirana, "Oglas je rezervisan za vas",
                $"Prodavac je rezervisao \"{oglas.Naslov}\" za vas. Pokrenite kupovinu u roku od 72 sata.", $"/rezervacije/{rezervacija.Id}", t);
            await uow.SacuvajAsync(t);
            return rezervacija.Id;
        }, IsolationLevel.ReadCommitted, ct);

        return (await uow.Rezervacije.DtoAsync(id, ct))!;
    }
}

public sealed record OtkaziRezervacijuCommand(int Id, string? Razlog) : IRequest<RezervacijaDto>;

internal sealed class OtkaziRezervacijuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<OtkaziRezervacijuCommand, RezervacijaDto>
{
    public async Task<RezervacijaDto> Handle(OtkaziRezervacijuCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var rezervacija = await uow.Rezervacije.PronadjiSaOglasomAsync(z.Id, ct);
        if (rezervacija is null || !rezervacija.JeUcesnik(korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Rezervacija", z.Id);
        }

        rezervacija.Otkazi(korisnikId, z.Razlog, rezervacija.Oglas, vreme.SadaUtc);
        var druga = korisnikId == rezervacija.KupacId ? rezervacija.ProdavacId : rezervacija.KupacId;
        await obavestenja.ObavestiAsync(druga, TipObavestenja.RezervacijaOtkazana, "Rezervacija je otkazana",
            $"Rezervacija za \"{rezervacija.Oglas.Naslov}\" je otkazana." + (z.Razlog is null ? "" : $" Razlog: {z.Razlog}"),
            $"/oglasi/{rezervacija.OglasId}", ct);
        await uow.SacuvajAsync(ct);
        return (await uow.Rezervacije.DtoAsync(rezervacija.Id, ct))!;
    }
}

public sealed record RezervacijeQuery(StatusRezervacije? Status, Stranicenje Stranicenje) : IRequest<StranicaRezultata<RezervacijaDto>>;

internal sealed class RezervacijeHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<RezervacijeQuery, StranicaRezultata<RezervacijaDto>>
{
    public Task<StranicaRezultata<RezervacijaDto>> Handle(RezervacijeQuery z, CancellationToken ct) =>
        uow.Rezervacije.ZaKorisnikaAsync(trenutni.ObavezanId, z.Status, z.Stranicenje, ct);
}

public sealed record RezervacijaQuery(int Id) : IRequest<RezervacijaDto>;

internal sealed class RezervacijaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<RezervacijaQuery, RezervacijaDto>
{
    public async Task<RezervacijaDto> Handle(RezervacijaQuery z, CancellationToken ct)
    {
        var dto = await uow.Rezervacije.DtoAsync(z.Id, ct);
        var korisnikId = trenutni.ObavezanId;
        if (dto is null || (dto.KupacId != korisnikId && dto.ProdavacId != korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Rezervacija", z.Id);
        }

        return dto;
    }
}

// =====================================================================
// TRANSAKCIJE (kupovina + mock escrow)
// =====================================================================

internal static class TransakcijaPomoc
{
    public static async Task<Transakcija> UcitajAsync(IUnitOfWork uow, int id, int korisnikId, CancellationToken ct)
    {
        var t = await uow.Transakcije.PronadjiSaOglasomAsync(id, ct);
        if (t is null || !t.JeUcesnik(korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Transakcija", id);
        }

        return t;
    }

    /// <summary>
    /// Telefon druge strane u transakciji (privatnost: samo učesnicima, samo verifikovan broj,
    /// i samo ako je vlasnik broja dozvolio prikaz u podešavanjima).
    /// </summary>
    public static async Task<TransakcijaDto> DetaljiAsync(IUnitOfWork uow, int id, int korisnikId, CancellationToken ct)
    {
        var dto = await uow.Transakcije.DetaljiAsync(id, ct) ?? throw new NijePronadjenoIzuzetak("Transakcija", id);
        if (dto.Status == StatusTransakcije.Otkazana)
        {
            return dto;
        }

        var drugaStrana = await uow.Korisnici.PronadjiPoIdAsync(korisnikId == dto.KupacId ? dto.ProdavacId : dto.KupacId, ct);
        return drugaStrana is { PrikaziTelefonKupcu: true, TelefonVerifikovan: true } ? dto with { TelefonDrugeStrane = drugaStrana.Telefon } : dto;
    }
}

public sealed record ZapocniKupovinuCommand(int RezervacijaId, NacinPreuzimanja NacinPreuzimanja, string? AdresaDostave, bool KoristiEscrow)
    : IRequest<TransakcijaDto>;

public sealed class ZapocniKupovinuValidator : AbstractValidator<ZapocniKupovinuCommand>
{
    public ZapocniKupovinuValidator()
    {
        RuleFor(x => x.NacinPreuzimanja).IsInEnum();
        RuleFor(x => x.AdresaDostave).NotEmpty().WithMessage("Za dostavu je obavezna adresa.")
            .MaximumLength(300).When(x => x.NacinPreuzimanja == NacinPreuzimanja.Dostava);
    }
}

internal sealed class ZapocniKupovinuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<ZapocniKupovinuCommand, TransakcijaDto>
{
    public async Task<TransakcijaDto> Handle(ZapocniKupovinuCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var rezervacija = await uow.Rezervacije.PronadjiSaOglasomAsync(z.RezervacijaId, ct);
        if (rezervacija is null || !rezervacija.JeUcesnik(korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Rezervacija", z.RezervacijaId);
        }

        var transakcija = Transakcija.IzRezervacije(rezervacija, rezervacija.Oglas, korisnikId, z.NacinPreuzimanja, z.AdresaDostave,
            z.KoristiEscrow, vreme.SadaUtc);
        await uow.Transakcije.DodajAsync(transakcija, ct);
        await uow.SacuvajAsync(ct);

        await obavestenja.ObavestiAsync(transakcija.ProdavacId, TipObavestenja.TransakcijaAzurirana, "Kupac je pokrenuo kupovinu",
            $"Kupovina za \"{rezervacija.Oglas.Naslov}\" je pokrenuta ({(z.NacinPreuzimanja == NacinPreuzimanja.Dostava ? "dostava" : "lično preuzimanje")}).",
            $"/transakcije/{transakcija.Id}", ct);
        await uow.SacuvajAsync(ct);
        return await TransakcijaPomoc.DetaljiAsync(uow, transakcija.Id, korisnikId, ct);
    }
}

public sealed record UplatiNaEscrowCommand(int TransakcijaId, string? KljucIdempotentnosti) : IRequest<TransakcijaDto>;

internal sealed class UplatiNaEscrowHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IPlatniProcesor procesor,
    IObavestenjaServis obavestenja, IDatumVreme vreme) : IRequestHandler<UplatiNaEscrowCommand, TransakcijaDto>
{
    public async Task<TransakcijaDto> Handle(UplatiNaEscrowCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var t = await TransakcijaPomoc.UcitajAsync(uow, z.TransakcijaId, korisnikId, ct);

        if (z.KljucIdempotentnosti is not null && await uow.Placanja.PostojiAsync(p => p.KljucIdempotentnosti == z.KljucIdempotentnosti, ct))
        {
            // Ponovljen zahtev (npr. dupli klik): vraćamo trenutno stanje bez nove naplate.
            return await TransakcijaPomoc.DetaljiAsync(uow, t.Id, korisnikId, ct);
        }

        // Prvo domenska provera (da li se uplata očekuje), pa tek onda naplata.
        t.EvidentirajUplatu(korisnikId);
        var opis = $"Escrow uplata za oglas #{t.OglasId}";
        var rezultat = await procesor.NaplatiAsync(korisnikId, t.Cena!.Value, opis, z.KljucIdempotentnosti, ct);
        if (!rezultat.Uspesno)
        {
            // SacuvajAsync se ne poziva, pa promena statusa escrow-a (iznad) ostaje samo u memoriji
            // ovog zahteva i odbacuje se zajedno sa DbContext-om.
            throw new DomenskiIzuzetak("placanje_odbijeno", rezultat.Poruka ?? "Plaćanje je odbijeno.");
        }

        await uow.Placanja.DodajAsync(new Placanje(korisnikId, t.Id, TipPlacanja.EscrowUplata, StatusPlacanja.Uspesno, t.Cena.Value,
            rezultat.ReferencaProcesora, z.KljucIdempotentnosti, opis, sada), ct);
        await obavestenja.ObavestiAsync(t.ProdavacId, TipObavestenja.TransakcijaAzurirana, "Novac je deponovan",
            $"Kupac je uplatio {t.Cena.Value:N0} RSD na escrow. Pošaljite/predajte predmet i potvrdite.", $"/transakcije/{t.Id}", ct);
        await uow.SacuvajAsync(ct);
        return await TransakcijaPomoc.DetaljiAsync(uow, t.Id, korisnikId, ct);
    }
}

/// <summary>
/// Potvrda jedne strane. Kada obe strane potvrde: transakcija je završena, oglas prelazi u Prodat,
/// escrow isplaćuje novac prodavcu, a brojači završenih transakcija se atomično uvećavaju.
/// </summary>
public sealed record PotvrdiTransakcijuCommand(int TransakcijaId) : IRequest<TransakcijaDto>;

internal sealed class PotvrdiTransakcijuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IPlatniProcesor procesor,
    IObavestenjaServis obavestenja, IDatumVreme vreme) : IRequestHandler<PotvrdiTransakcijuCommand, TransakcijaDto>
{
    public async Task<TransakcijaDto> Handle(PotvrdiTransakcijuCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        await uow.UTransakcijiAsync(async c =>
        {
            var sada = vreme.SadaUtc;
            var t = await TransakcijaPomoc.UcitajAsync(uow, z.TransakcijaId, korisnikId, c);
            var zavrsena = t.Potvrdi(korisnikId, sada);

            if (!zavrsena)
            {
                await obavestenja.ObavestiAsync(t.DrugaStrana(korisnikId), TipObavestenja.TransakcijaAzurirana, "Druga strana je potvrdila",
                    $"{(korisnikId == t.KupacId ? "Kupac je potvrdio prijem" : "Prodavac je potvrdio predaju")} za \"{t.Oglas.Naslov}\". Potvrdite i vi.",
                    $"/transakcije/{t.Id}", c);
                await uow.SacuvajAsync(c);
                return 0;
            }

            if (t.StatusEscrowa == StatusEscrowa.Isplaceno)
            {
                var opis = $"Escrow isplata za oglas #{t.OglasId}";
                var isplata = await procesor.IsplatiAsync(t.ProdavacId, t.Cena!.Value, opis, c);
                await uow.Placanja.DodajAsync(new Placanje(t.ProdavacId, t.Id, TipPlacanja.EscrowIsplata, StatusPlacanja.Uspesno,
                    t.Cena.Value, isplata.ReferencaProcesora, null, opis, sada), c);
            }

            foreach (var ucesnik in new[] { t.KupacId, t.ProdavacId })
            {
                await obavestenja.ObavestiAsync(ucesnik, TipObavestenja.TransakcijaZavrsena, "Kupovina je završena",
                    $"Transakcija za \"{t.Oglas.Naslov}\" je uspešno završena. Ostavite recenziju drugoj strani.", $"/transakcije/{t.Id}", c);
            }

            await uow.SacuvajAsync(c);
            await uow.Korisnici.UvecajZavrseneTransakcijeAsync([t.KupacId, t.ProdavacId], c);
            return 1;
        }, IsolationLevel.ReadCommitted, ct);

        return await TransakcijaPomoc.DetaljiAsync(uow, z.TransakcijaId, korisnikId, ct);
    }
}

public sealed record OtkaziTransakcijuCommand(int TransakcijaId, string? Razlog) : IRequest<TransakcijaDto>;

public sealed class OtkaziTransakcijuValidator : AbstractValidator<OtkaziTransakcijuCommand>
{
    public OtkaziTransakcijuValidator() => RuleFor(x => x.Razlog).MaximumLength(500);
}

internal sealed class OtkaziTransakcijuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IPlatniProcesor procesor,
    IObavestenjaServis obavestenja, IDatumVreme vreme) : IRequestHandler<OtkaziTransakcijuCommand, TransakcijaDto>
{
    public async Task<TransakcijaDto> Handle(OtkaziTransakcijuCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var t = await TransakcijaPomoc.UcitajAsync(uow, z.TransakcijaId, korisnikId, ct);

        t.Otkazi(korisnikId, z.Razlog, sada);
        if (t.StatusEscrowa == StatusEscrowa.Vraceno)
        {
            // Deponovan novac se vraća kupcu.
            var opis = $"Povraćaj escrow uplate za oglas #{t.OglasId}";
            var povracaj = await procesor.VratiNovacAsync(t.KupacId, t.Cena!.Value, opis, ct);
            await uow.Placanja.DodajAsync(new Placanje(t.KupacId, t.Id, TipPlacanja.EscrowPovracaj, StatusPlacanja.Uspesno, t.Cena.Value,
                povracaj.ReferencaProcesora, null, opis, sada), ct);
        }

        await obavestenja.ObavestiAsync(t.DrugaStrana(korisnikId), TipObavestenja.TransakcijaAzurirana, "Kupovina je otkazana",
            $"Kupovina za \"{t.Oglas.Naslov}\" je otkazana." + (z.Razlog is null ? "" : $" Razlog: {z.Razlog}"), $"/transakcije/{t.Id}", ct);
        await uow.SacuvajAsync(ct);
        return await TransakcijaPomoc.DetaljiAsync(uow, t.Id, korisnikId, ct);
    }
}

public sealed record TransakcijeQuery(StatusTransakcije? Status, Stranicenje Stranicenje) : IRequest<StranicaRezultata<TransakcijaDto>>;

internal sealed class TransakcijeHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<TransakcijeQuery, StranicaRezultata<TransakcijaDto>>
{
    public Task<StranicaRezultata<TransakcijaDto>> Handle(TransakcijeQuery z, CancellationToken ct) =>
        uow.Transakcije.ZaKorisnikaAsync(trenutni.ObavezanId, z.Status, z.Stranicenje, ct);
}

public sealed record TransakcijaQuery(int Id) : IRequest<TransakcijaDto>;

internal sealed class TransakcijaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<TransakcijaQuery, TransakcijaDto>
{
    public async Task<TransakcijaDto> Handle(TransakcijaQuery z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var dto = await uow.Transakcije.DetaljiAsync(z.Id, ct);
        if (dto is null || (dto.KupacId != korisnikId && dto.ProdavacId != korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Transakcija", z.Id);
        }

        return await TransakcijaPomoc.DetaljiAsync(uow, z.Id, korisnikId, ct);
    }
}

// =====================================================================
// RECENZIJE
// =====================================================================

public sealed record OstaviRecenzijuCommand(int TransakcijaId, int Ocena, string? Komentar) : IRequest<RecenzijaDto>;

public sealed class OstaviRecenzijuValidator : AbstractValidator<OstaviRecenzijuCommand>
{
    public OstaviRecenzijuValidator()
    {
        RuleFor(x => x.Ocena).InclusiveBetween(1, 5).WithMessage("Ocena mora biti od 1 do 5.");
        RuleFor(x => x.Komentar).MaximumLength(1000).WithMessage("Komentar može imati najviše 1000 karaktera.");
    }
}

internal sealed class OstaviRecenzijuHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<OstaviRecenzijuCommand, RecenzijaDto>
{
    public async Task<RecenzijaDto> Handle(OstaviRecenzijuCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var recenzijaId = await uow.UTransakcijiAsync(async c =>
        {
            var sada = vreme.SadaUtc;
            var t = await TransakcijaPomoc.UcitajAsync(uow, z.TransakcijaId, korisnikId, c);

            // Provera u aplikaciji daje jasnu poruku; UNIQUE (TransakcijaId, OcenjivacId) u bazi je konačna garancija.
            if (await uow.Recenzije.PostojiAsync(r => r.TransakcijaId == t.Id && r.OcenjivacId == korisnikId, c))
            {
                throw new KonfliktIzuzetak("Već ste ostavili recenziju za ovu transakciju.");
            }

            var recenzija = Recenzija.Kreiraj(t, korisnikId, z.Ocena, z.Komentar, sada);
            await uow.Recenzije.DodajAsync(recenzija, c);
            await obavestenja.ObavestiAsync(recenzija.OcenjeniKorisnikId, TipObavestenja.NovaRecenzija, "Nova recenzija",
                $"Dobili ste ocenu {z.Ocena}/5 za \"{t.Oglas.Naslov}\".", $"/korisnici/{recenzija.OcenjeniKorisnikId}", c);
            await uow.SacuvajAsync(c);

            // Atomično ažuriranje proseka u istoj transakciji baze kao i upis recenzije.
            await uow.Korisnici.DodajOcenuAsync(recenzija.OcenjeniKorisnikId, z.Ocena, c);
            return recenzija.Id;
        }, IsolationLevel.ReadCommitted, ct);

        var stranica = await uow.Recenzije.ZaKorisnikaAsync(
            (await uow.Recenzije.PronadjiPoIdAsync(recenzijaId, ct))!.OcenjeniKorisnikId, new Stranicenje { VelicinaStranice = 50 }, ct);
        return stranica.Stavke.First(r => r.Id == recenzijaId);
    }
}

public sealed record RecenzijeKorisnikaQuery(int KorisnikId, Stranicenje Stranicenje) : IRequest<StranicaRezultata<RecenzijaDto>>;

internal sealed class RecenzijeKorisnikaHandler(IUnitOfWork uow) : IRequestHandler<RecenzijeKorisnikaQuery, StranicaRezultata<RecenzijaDto>>
{
    public Task<StranicaRezultata<RecenzijaDto>> Handle(RecenzijeKorisnikaQuery z, CancellationToken ct) =>
        uow.Recenzije.ZaKorisnikaAsync(z.KorisnikId, z.Stranicenje, ct);
}

public sealed record SazetakOcenaQuery(int KorisnikId) : IRequest<SazetakOcenaDto>;

internal sealed class SazetakOcenaHandler(IUnitOfWork uow) : IRequestHandler<SazetakOcenaQuery, SazetakOcenaDto>
{
    public Task<SazetakOcenaDto> Handle(SazetakOcenaQuery z, CancellationToken ct) => uow.Recenzije.SazetakAsync(z.KorisnikId, ct);
}
