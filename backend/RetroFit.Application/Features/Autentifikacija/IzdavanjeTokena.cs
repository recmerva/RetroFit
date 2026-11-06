using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Domain.Entiteti;

namespace RetroFit.Application.Features.Autentifikacija;

/// <summary>
/// Rezultat prijave/registracije/osvežavanja. RefreshToken NE ide u telo HTTP odgovora,
/// već ga kontroler postavlja u httpOnly cookie (JavaScript ne može da ga pročita -> zaštita od XSS krađe).
/// </summary>
public sealed record AutentifikacijaRezultat(
    string AccessToken,
    DateTime AccessTokenIsticeUtc,
    string RefreshToken,
    DateTime RefreshTokenIsticeUtc,
    MojProfilDto Korisnik);

/// <summary>Zajednička logika izdavanja para tokena (koriste je registracija, prijava i osvežavanje).</summary>
internal sealed class IzdavanjeTokena(IUnitOfWork uow, ITokenServis tokeni, ITrenutniKorisnik trenutni, IDatumVreme vreme)
{
    /// <summary>
    /// Izdaje access + refresh token. Ako je porodica zadata (rotacija), novi refresh token je u istoj porodici;
    /// inače (nova prijava) počinje nova porodica.
    /// </summary>
    public async Task<AutentifikacijaRezultat> IzdajAsync(Korisnik korisnik, Guid? porodica, RefreshToken? stari, CancellationToken ct)
    {
        var uloge = await uow.Korisnici.UlogeKorisnikaAsync(korisnik.Id, ct);
        var (access, accessIstice) = tokeni.KreirajAccessToken(korisnik, uloge);
        var (refresh, refreshHash, refreshIstice) = tokeni.KreirajRefreshToken();

        await uow.RefreshTokeni.DodajAsync(
            new RefreshToken(korisnik.Id, refreshHash, porodica ?? Guid.NewGuid(), vreme.SadaUtc, refreshIstice, trenutni.IpAdresa), ct);
        stari?.Opozovi(vreme.SadaUtc, RefreshToken.RazlogRotacije, refreshHash);

        await uow.SacuvajAsync(ct);

        var profil = await uow.Korisnici.MojProfilAsync(korisnik.Id, ct)
                     ?? throw new NijePronadjenoIzuzetak("Korisnik", korisnik.Id);
        return new AutentifikacijaRezultat(access, accessIstice, refresh, refreshIstice, profil);
    }
}
