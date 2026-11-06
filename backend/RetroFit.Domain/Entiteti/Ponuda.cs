using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Ponuda cene u okviru razgovora.
///
/// Tok pregovora:
///   kupac daje ponudu (NaCekanju) -> prodavac: Prihvati | Odbij | Kontraponuda
///   Kontraponuda: stara ponuda dobija status Kontraponuda, a nastaje NOVA ponuda
///   (PrethodnaPonudaId pokazuje na staru) na koju sada odgovara druga strana.
///   Ponuda na koju niko ne odgovori u roku od 48h prelazi u Istekla.
///
/// U bazi: filtrirani UNIQUE indeks (RazgovorId) WHERE Status = 'NaCekanju',
/// pa u jednom razgovoru u svakom trenutku postoji najviše jedna ponuda na čekanju.
/// </summary>
public class Ponuda : Entitet
{
    private Ponuda() { }

    private Ponuda(Razgovor razgovor, int ponudioKorisnikId, decimal iznos, string? napomena, DateTime sada, Ponuda? prethodna)
    {
        Provera.Uslov(iznos is > 0 and <= PoslovnaPravila.MaksCena, "validacija", "Iznos ponude mora biti veći od 0.");
        Razgovor = razgovor;
        RazgovorId = razgovor.Id;
        OglasId = razgovor.OglasId;
        KupacId = razgovor.KupacId;
        PonudioKorisnikId = ponudioKorisnikId;
        Iznos = iznos;
        Napomena = Provera.OpcioniTekst(napomena, "Napomena", 500);
        Status = StatusPonude.NaCekanju;
        KreiranoUtc = sada;
        IsticeUtc = sada.AddHours(PoslovnaPravila.TrajanjePonudeSati);
        // Veza preko navigacije: EF postavlja strani ključ i kada prethodna ponuda još nema Id.
        PrethodnaPonuda = prethodna;
        PrethodnaPonudaId = prethodna is { Id: > 0 } ? prethodna.Id : null;
    }

    public int OglasId { get; private set; }
    public Oglas Oglas { get; private set; } = null!;
    public int RazgovorId { get; private set; }
    public Razgovor Razgovor { get; private set; } = null!;
    public int KupacId { get; private set; }
    public Korisnik Kupac { get; private set; } = null!;
    /// <summary>Ko je dao ovu ponudu (kupac ili, kod kontraponude, prodavac).</summary>
    public int PonudioKorisnikId { get; private set; }
    public Korisnik PonudioKorisnik { get; private set; } = null!;
    public decimal Iznos { get; private set; }
    public string? Napomena { get; private set; }
    public StatusPonude Status { get; private set; }
    public DateTime KreiranoUtc { get; private set; }
    public DateTime IsticeUtc { get; private set; }
    public DateTime? OdgovorenoUtc { get; private set; }
    public int? PrethodnaPonudaId { get; private set; }
    public Ponuda? PrethodnaPonuda { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Kupac daje prvu ponudu. Dozvoljeno samo za aktivne oglase tipa Prodaja.</summary>
    public static Ponuda Kreiraj(Razgovor razgovor, Oglas oglas, int kupacId, decimal iznos, string? napomena, DateTime sada)
    {
        Provera.Uslov(razgovor.KupacId == kupacId, "zabranjeno", "Ponudu može dati samo kupac iz razgovora.");
        Provera.Uslov(oglas.Id == razgovor.OglasId, "validacija", "Razgovor ne pripada ovom oglasu.");
        Provera.Uslov(oglas.Tip == TipOglasa.Prodaja, "ponuda_nije_moguca", "Ponuda cene je moguća samo za oglase tipa Prodaja.");
        Provera.Uslov(oglas.Status == StatusOglasa.Aktivan && !oglas.Obrisan, "oglas_nije_aktivan",
            "Ponuda se može dati samo za aktivan oglas.");
        return new Ponuda(razgovor, kupacId, iznos, napomena, sada, null);
    }

    /// <summary>Korisnik koji treba da odgovori na ponudu (druga strana u odnosu na onoga ko je ponudio).</summary>
    public int OdgovaraKorisnikId => PonudioKorisnikId == KupacId ? Razgovor.ProdavacId : KupacId;

    public bool JeIstekla(DateTime sada) => Status == StatusPonude.NaCekanju && sada >= IsticeUtc;

    public void Prihvati(int korisnikId, Oglas oglas, DateTime sada)
    {
        ProveriOdgovor(korisnikId, sada);
        Provera.Uslov(oglas.Status == StatusOglasa.Aktivan && !oglas.Obrisan, "oglas_nije_aktivan",
            "Oglas više nije dostupan.");
        Status = StatusPonude.Prihvacena;
        OdgovorenoUtc = sada;
    }

    public void Odbij(int korisnikId, DateTime sada)
    {
        ProveriOdgovor(korisnikId, sada);
        Status = StatusPonude.Odbijena;
        OdgovorenoUtc = sada;
    }

    /// <summary>Sistemsko odbijanje (npr. kada je druga ponuda za isti oglas prihvaćena).</summary>
    public void OdbijAutomatski(DateTime sada)
    {
        if (Status != StatusPonude.NaCekanju)
        {
            return;
        }

        Status = StatusPonude.Odbijena;
        OdgovorenoUtc = sada;
    }

    public Ponuda Kontraponuda(int korisnikId, decimal noviIznos, string? napomena, DateTime sada)
    {
        ProveriOdgovor(korisnikId, sada);
        Provera.Uslov(noviIznos != Iznos, "validacija", "Kontraponuda mora imati drugačiji iznos.");
        Status = StatusPonude.Kontraponuda;
        OdgovorenoUtc = sada;
        return new Ponuda(Razgovor, korisnikId, noviIznos, napomena, sada, this);
    }

    public void Istekni(DateTime sada)
    {
        Provera.Uslov(JeIstekla(sada), "ponuda_nije_istekla", "Ponuda još nije istekla.");
        Status = StatusPonude.Istekla;
    }

    private void ProveriOdgovor(int korisnikId, DateTime sada)
    {
        Provera.Uslov(Status == StatusPonude.NaCekanju, "ponuda_zatvorena",
            $"Na ponudu u statusu '{Status}' se više ne može odgovoriti.");
        Provera.Uslov(sada < IsticeUtc, "ponuda_istekla", "Ponuda je istekla.");
        Provera.Uslov(korisnikId != PonudioKorisnikId, "zabranjeno", "Ne možete odgovoriti na sopstvenu ponudu.");
        Provera.Uslov(korisnikId == OdgovaraKorisnikId, "zabranjeno", "Niste učesnik ovog pregovora.");
    }
}
