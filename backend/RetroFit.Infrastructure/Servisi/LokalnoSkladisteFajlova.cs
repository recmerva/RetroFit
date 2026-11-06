using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Domain.Common;
using RetroFit.Domain.Pravila;

namespace RetroFit.Infrastructure.Servisi;

public sealed class SkladisteOpcije
{
    public const string Sekcija = "Skladiste";

    /// <summary>Apsolutna ili relativna putanja foldera (u Docker-u: volume /app/uploads).</summary>
    public string Putanja { get; set; } = "uploads";
    /// <summary>Javni URL prefiks pod kojim API servira fajlove.</summary>
    public string JavniPrefiks { get; set; } = "/uploads";
}

/// <summary>
/// Čuvanje slika na lokalnom disku. Bezbednosne mere:
///  - tip fajla se proverava po "magičnim bajtovima" (potpisu), ne po ekstenziji koju šalje klijent;
///  - dozvoljeni su samo JPEG, PNG i WebP, najviše 5 MB;
///  - ime fajla generiše server (GUID), pa korisnik ne može da utiče na putanju (path traversal);
///  - pri brisanju se proverava da je putanja unutar foldera za otpremanje.
/// </summary>
internal sealed class LokalnoSkladisteFajlova(IOptions<SkladisteOpcije> opcije, ILogger<LokalnoSkladisteFajlova> logger) : ISkladisteFajlova
{
    private readonly SkladisteOpcije _opcije = opcije.Value;

    private string Koren => Path.GetFullPath(_opcije.Putanja);

    public async Task<SacuvanFajl> SacuvajSlikuAsync(Stream sadrzaj, string originalniNaziv, string tipSadrzaja, string folder,
        CancellationToken ct)
    {
        using var memorija = new MemoryStream();
        await sadrzaj.CopyToAsync(memorija, ct);
        Provera(memorija.Length is > 0 and <= PoslovnaPravila.MaksVelicinaSlikeBajtova, "Slika je prazna ili veća od 5 MB.");

        var bajtovi = memorija.ToArray();
        var (pravTip, ekstenzija) = PrepoznajTip(bajtovi);
        Provera(pravTip is not null, "Dozvoljene su samo JPEG, PNG i WebP slike.");

        var bezbedanFolder = string.Join('/', folder.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => new string(d.Where(char.IsLetterOrDigit).ToArray())));
        var direktorijum = Path.Combine(Koren, bezbedanFolder);
        Directory.CreateDirectory(direktorijum);

        var imeFajla = $"{Guid.NewGuid():N}{ekstenzija}";
        await File.WriteAllBytesAsync(Path.Combine(direktorijum, imeFajla), bajtovi, ct);

        return new SacuvanFajl($"{_opcije.JavniPrefiks.TrimEnd('/')}/{bezbedanFolder}/{imeFajla}", pravTip!, bajtovi.Length);
    }

    public Task ObrisiAsync(string javnaPutanja, CancellationToken ct)
    {
        var relativna = javnaPutanja.StartsWith(_opcije.JavniPrefiks, StringComparison.OrdinalIgnoreCase)
            ? javnaPutanja[_opcije.JavniPrefiks.Length..].TrimStart('/')
            : javnaPutanja.TrimStart('/');

        var puna = Path.GetFullPath(Path.Combine(Koren, relativna));
        if (!puna.StartsWith(Koren, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Pokušaj brisanja fajla van foldera za otpremanje: {Putanja}", javnaPutanja);
            return Task.CompletedTask;
        }

        if (File.Exists(puna))
        {
            File.Delete(puna);
        }

        return Task.CompletedTask;
    }

    /// <summary>Prepoznavanje formata po potpisu fajla (magic bytes).</summary>
    internal static (string? Tip, string Ekstenzija) PrepoznajTip(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            return ("image/jpeg", ".jpg");
        }

        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
        {
            return ("image/png", ".png");
        }

        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
        {
            return ("image/webp", ".webp");
        }

        return (null, string.Empty);
    }

    private static void Provera(bool uslov, string poruka)
    {
        if (!uslov)
        {
            throw new DomenskiIzuzetak("neispravan_fajl", poruka);
        }
    }
}
