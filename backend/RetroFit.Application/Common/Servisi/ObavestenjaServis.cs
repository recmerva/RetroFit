using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Application.Common.Servisi;

/// <summary>
/// Kreira obaveštenje u okviru iste Unit of Work transakcije kao i poslovna promena
/// (npr. "ponuda prihvaćena" se upisuje zajedno sa samom promenom statusa ponude).
/// </summary>
internal sealed class ObavestenjaServis(IUnitOfWork uow, IDatumVreme vreme) : IObavestenjaServis
{
    public Task ObavestiAsync(int korisnikId, TipObavestenja tip, string naslov, string sadrzaj, string? link, CancellationToken ct) =>
        uow.Obavestenja.DodajAsync(new Obavestenje(korisnikId, tip, Skrati(naslov, 150), Skrati(sadrzaj, 1000), link, vreme.SadaUtc), ct);

    private static string Skrati(string tekst, int maks) => tekst.Length <= maks ? tekst : tekst[..(maks - 1)] + "…";
}
