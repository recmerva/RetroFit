using System.Data;
using FluentValidation;
using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Common.Validacija;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Features.Trgovina;

internal static class PonudaPomoc
{
    public static async Task<Ponuda> UcitajAsync(IUnitOfWork uow, int ponudaId, int korisnikId, CancellationToken ct)
    {
        var ponuda = await uow.Ponude.PronadjiSaRazgovoromAsync(ponudaId, ct);
        if (ponuda is null || !ponuda.Razgovor.JeUcesnik(korisnikId))
        {
            throw new NijePronadjenoIzuzetak("Ponuda", ponudaId);
        }

        return ponuda;
    }

    public static string Iznos(decimal iznos) => $"{iznos:N0} RSD";
}

// =====================================================================
// DAVANJE PONUDE
// =====================================================================

public sealed record DajPonuduCommand(int OglasId, decimal Iznos, string? Napomena) : IRequest<PonudaDto>;

public sealed class DajPonuduValidator : AbstractValidator<DajPonuduCommand>
{
    public DajPonuduValidator()
    {
        RuleFor(x => (decimal?)x.Iznos).Cena().WithName("Iznos");
        RuleFor(x => x.Napomena).MaximumLength(500);
    }
}

internal sealed class DajPonuduHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<DajPonuduCommand, PonudaDto>
{
    public async Task<PonudaDto> Handle(DajPonuduCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var kupacId = trenutni.ObavezanId;
        var oglas = await uow.Oglasi.PronadjiPoIdAsync(z.OglasId, ct) ?? throw new NijePronadjenoIzuzetak("Oglas", z.OglasId);
        var razgovor = await RazgovorPomoc.PronadjiIliKreirajAsync(uow, oglas, kupacId, sada, ct);

        if (razgovor.Id > 0 && await uow.Ponude.NaCekanjuURazgovoruAsync(razgovor.Id, ct) is not null)
        {
            throw new KonfliktIzuzetak("U ovom razgovoru već postoji ponuda koja čeka odgovor.");
        }

        var ponuda = Ponuda.Kreiraj(razgovor, oglas, kupacId, z.Iznos, z.Napomena, sada);
        await uow.Ponude.DodajAsync(ponuda, ct);
        await uow.SacuvajAsync(ct);

        razgovor.PosaljiPoruku(kupacId, $"Ponuda: {PonudaPomoc.Iznos(z.Iznos)}" + (z.Napomena is null ? "" : $" - {z.Napomena}"),
            TipPoruke.Sistemska, sada, ponuda.Id);
        await obavestenja.ObavestiAsync(oglas.ProdavacId, TipObavestenja.NovaPonuda, "Nova ponuda",
            $"Dobili ste ponudu od {PonudaPomoc.Iznos(z.Iznos)} za \"{oglas.Naslov}\".", $"/razgovori/{razgovor.Id}", ct);
        await uow.SacuvajAsync(ct);

        return (await uow.Ponude.DtoAsync(ponuda.Id, ct))!;
    }
}

// =====================================================================
// PRIHVATANJE PONUDE -> REZERVACIJA
// =====================================================================

public sealed record PrihvatiPonuduCommand(int PonudaId) : IRequest<RezervacijaDto>;

/// <summary>
/// Prihvatanje ponude je najosetljivija operacija: menja 4 tabele u JEDNOJ transakciji baze:
///   Ponude (prihvaćena + ostale na čekanju odbijene), Rezervacije (nova), Oglasi (Rezervisan),
///   Poruke i Obavestenja.
/// Zaštita od istovremenog prihvatanja dve ponude za isti oglas:
///   1) RowVersion na Oglasi (optimistic concurrency) -> drugi upis dobija konflikt;
///   2) filtrirani UNIQUE indeks na Rezervacije (jedna aktivna po oglasu) -> baza odbija drugu.
/// </summary>
internal sealed class PrihvatiPonuduHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<PrihvatiPonuduCommand, RezervacijaDto>
{
    public async Task<RezervacijaDto> Handle(PrihvatiPonuduCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var rezervacijaId = await uow.UTransakcijiAsync(async t =>
        {
            var sada = vreme.SadaUtc;
            var ponuda = await PonudaPomoc.UcitajAsync(uow, z.PonudaId, korisnikId, t);
            var oglas = ponuda.Oglas;

            ponuda.Prihvati(korisnikId, oglas, sada);
            var rezervacija = Rezervacija.Kreiraj(oglas, ponuda.KupacId, ponuda.Iznos, ponuda, sada);
            await uow.Rezervacije.DodajAsync(rezervacija, t);

            // Sve ostale ponude na čekanju za ovaj oglas (od drugih kupaca) se automatski odbijaju.
            foreach (var druga in await uow.Ponude.NaCekanjuZaOglasAsync(oglas.Id, t))
            {
                if (druga.Id == ponuda.Id)
                {
                    continue;
                }

                druga.OdbijAutomatski(sada);
                await obavestenja.ObavestiAsync(druga.KupacId, TipObavestenja.PonudaOdbijena, "Oglas je rezervisan",
                    $"Oglas \"{oglas.Naslov}\" je rezervisan za drugog kupca, pa je vaša ponuda zatvorena.", $"/oglasi/{oglas.Id}", t);
            }

            ponuda.Razgovor.PosaljiPoruku(korisnikId, $"Ponuda od {PonudaPomoc.Iznos(ponuda.Iznos)} je prihvaćena. Oglas je rezervisan za kupca.",
                TipPoruke.Sistemska, sada, ponuda.Id);
            await uow.SacuvajAsync(t);

            var drugaStrana = ponuda.Razgovor.DrugiUcesnik(korisnikId);
            await obavestenja.ObavestiAsync(drugaStrana, TipObavestenja.PonudaPrihvacena, "Ponuda je prihvaćena",
                $"Ponuda od {PonudaPomoc.Iznos(ponuda.Iznos)} za \"{oglas.Naslov}\" je prihvaćena. Rezervacija važi 72 sata.",
                $"/rezervacije/{rezervacija.Id}", t);
            await uow.SacuvajAsync(t);
            return rezervacija.Id;
        }, IsolationLevel.ReadCommitted, ct);

        return (await uow.Rezervacije.DtoAsync(rezervacijaId, ct))!;
    }
}

public sealed record OdbijPonuduCommand(int PonudaId) : IRequest<PonudaDto>;

internal sealed class OdbijPonuduHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<OdbijPonuduCommand, PonudaDto>
{
    public async Task<PonudaDto> Handle(OdbijPonuduCommand z, CancellationToken ct)
    {
        var sada = vreme.SadaUtc;
        var korisnikId = trenutni.ObavezanId;
        var ponuda = await PonudaPomoc.UcitajAsync(uow, z.PonudaId, korisnikId, ct);

        ponuda.Odbij(korisnikId, sada);
        ponuda.Razgovor.PosaljiPoruku(korisnikId, $"Ponuda od {PonudaPomoc.Iznos(ponuda.Iznos)} je odbijena.", TipPoruke.Sistemska, sada, ponuda.Id);
        await obavestenja.ObavestiAsync(ponuda.PonudioKorisnikId, TipObavestenja.PonudaOdbijena, "Ponuda je odbijena",
            $"Vaša ponuda od {PonudaPomoc.Iznos(ponuda.Iznos)} za \"{ponuda.Oglas.Naslov}\" je odbijena.", $"/razgovori/{ponuda.RazgovorId}", ct);
        await uow.SacuvajAsync(ct);
        return (await uow.Ponude.DtoAsync(ponuda.Id, ct))!;
    }
}

public sealed record KontraponudaCommand(int PonudaId, decimal Iznos, string? Napomena) : IRequest<PonudaDto>;

public sealed class KontraponudaValidator : AbstractValidator<KontraponudaCommand>
{
    public KontraponudaValidator()
    {
        RuleFor(x => (decimal?)x.Iznos).Cena().WithName("Iznos");
        RuleFor(x => x.Napomena).MaximumLength(500);
    }
}

internal sealed class KontraponudaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IObavestenjaServis obavestenja, IDatumVreme vreme)
    : IRequestHandler<KontraponudaCommand, PonudaDto>
{
    public async Task<PonudaDto> Handle(KontraponudaCommand z, CancellationToken ct)
    {
        var korisnikId = trenutni.ObavezanId;
        var novaId = await uow.UTransakcijiAsync(async t =>
        {
            var sada = vreme.SadaUtc;
            var ponuda = await PonudaPomoc.UcitajAsync(uow, z.PonudaId, korisnikId, t);
            var nova = ponuda.Kontraponuda(korisnikId, z.Iznos, z.Napomena, sada);

            // Dva koraka u istoj transakciji: filtrirani UNIQUE indeks dozvoljava samo jednu ponudu
            // "NaCekanju" po razgovoru, pa stara ponuda MORA promeniti status pre upisa nove.
            await uow.SacuvajAsync(t);
            await uow.Ponude.DodajAsync(nova, t);
            await uow.SacuvajAsync(t);

            ponuda.Razgovor.PosaljiPoruku(korisnikId, $"Kontraponuda: {PonudaPomoc.Iznos(z.Iznos)}" + (z.Napomena is null ? "" : $" - {z.Napomena}"),
                TipPoruke.Sistemska, sada, nova.Id);
            await obavestenja.ObavestiAsync(ponuda.PonudioKorisnikId, TipObavestenja.Kontraponuda, "Kontraponuda",
                $"Dobili ste kontraponudu od {PonudaPomoc.Iznos(z.Iznos)} za \"{ponuda.Oglas.Naslov}\".", $"/razgovori/{ponuda.RazgovorId}", t);
            await uow.SacuvajAsync(t);
            return nova.Id;
        }, IsolationLevel.ReadCommitted, ct);

        return (await uow.Ponude.DtoAsync(novaId, ct))!;
    }
}

public sealed record PonudeQuery(bool? KaoKupac, StatusPonude? Status, Stranicenje Stranicenje) : IRequest<StranicaRezultata<PonudaDto>>;

internal sealed class PonudeHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<PonudeQuery, StranicaRezultata<PonudaDto>>
{
    public Task<StranicaRezultata<PonudaDto>> Handle(PonudeQuery z, CancellationToken ct) =>
        uow.Ponude.ZaKorisnikaAsync(trenutni.ObavezanId, z.KaoKupac, z.Status, z.Stranicenje, ct);
}
