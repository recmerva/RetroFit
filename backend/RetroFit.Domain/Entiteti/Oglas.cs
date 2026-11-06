using RetroFit.Domain.Common;
using RetroFit.Domain.Enumi;
using RetroFit.Domain.Pravila;

namespace RetroFit.Domain.Entiteti;

/// <summary>
/// Oglas - centralni entitet sistema.
///
/// Mašina stanja (svi prelazi se proveravaju OVDE, ne u kontroleru):
///   Nacrt      -> Aktivan                 (Objavi)
///   Aktivan    -> Rezervisan              (Rezervisi)
///   Rezervisan -> Aktivan                 (OtkaziRezervaciju)
///   Aktivan/Rezervisan -> Prodat          (OznaciKaoProdat)
///   Aktivan    -> Istekao                 (Istekni, posle 60 dana)
///   Istekao    -> Aktivan                 (Obnovi)
///   bilo koji osim Prodat -> Uklonjen     (Ukloni - moderator)
/// Prodat i Uklonjen su završna stanja.
///
/// RowVersion služi za optimističku kontrolu konkurentnosti: ako dva zahteva istovremeno
/// pokušaju da rezervišu isti oglas, drugi dobija DbUpdateConcurrencyException.
/// </summary>
public class Oglas : AuditabilniEntitet, IMekoBrisanje
{
    private readonly List<SlikaOglasa> _slike = [];
    private readonly List<AtributOglasa> _atributi = [];

    private Oglas() { }

    private Oglas(int prodavacId) => ProdavacId = prodavacId;

    public int ProdavacId { get; private set; }
    public Korisnik Prodavac { get; private set; } = null!;
    public int KategorijaId { get; private set; }
    public Kategorija Kategorija { get; private set; } = null!;
    public int GradId { get; private set; }
    public Grad Grad { get; private set; } = null!;

    public string Naslov { get; private set; } = string.Empty;
    public string Opis { get; private set; } = string.Empty;
    /// <summary>Cena u dinarima. Obavezna za Prodaju, NULL za Razmenu i Poklon (CHECK ograničenje u bazi).</summary>
    public decimal? Cena { get; private set; }
    public TipOglasa Tip { get; private set; }
    public StanjeProizvoda Stanje { get; private set; }
    public StatusOglasa Status { get; private set; }
    /// <summary>Za oglase tipa Razmena: šta prodavac želi u zamenu.</summary>
    public string? ZeljenaZamena { get; private set; }
    public bool DostavaMoguca { get; private set; }
    public bool LicnoPreuzimanjeMoguce { get; private set; }

    public DateTime? ObjavljenoUtc { get; private set; }
    public DateTime? IsticeUtc { get; private set; }
    public string? RazlogUklanjanja { get; private set; }

    // Denormalizovani brojači za sortiranje po popularnosti bez skupih COUNT upita.
    public int BrojPregleda { get; private set; }
    public int BrojOmiljenih { get; private set; }

    public bool Obrisan { get; private set; }
    public DateTime? ObrisanoUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<SlikaOglasa> Slike => _slike;
    public IReadOnlyCollection<AtributOglasa> Atributi => _atributi;

    public bool JeJavnoVidljiv => !Obrisan && Status is StatusOglasa.Aktivan;

    public static Oglas Kreiraj(int prodavacId, int kategorijaId, int gradId, string naslov, string opis,
        TipOglasa tip, decimal? cena, StanjeProizvoda stanje, bool dostavaMoguca, bool licnoPreuzimanjeMoguce,
        string? zeljenaZamena, bool objaviOdmah, DateTime sada)
    {
        var oglas = new Oglas(prodavacId) { Status = StatusOglasa.Nacrt };
        oglas.PostaviPodatke(kategorijaId, gradId, naslov, opis, tip, cena, stanje, dostavaMoguca,
            licnoPreuzimanjeMoguce, zeljenaZamena);

        if (objaviOdmah)
        {
            oglas.Objavi(sada);
        }

        return oglas;
    }

    public bool JeVlasnik(int korisnikId) => ProdavacId == korisnikId;

    public void Izmeni(int kategorijaId, int gradId, string naslov, string opis, TipOglasa tip, decimal? cena,
        StanjeProizvoda stanje, bool dostavaMoguca, bool licnoPreuzimanjeMoguce, string? zeljenaZamena)
    {
        ProveriDaSeMozeMenjati();
        if (kategorijaId != KategorijaId)
        {
            // Atributi zavise od kategorije, pa se pri promeni kategorije brišu.
            _atributi.Clear();
        }

        PostaviPodatke(kategorijaId, gradId, naslov, opis, tip, cena, stanje, dostavaMoguca,
            licnoPreuzimanjeMoguce, zeljenaZamena);
    }

    public void Objavi(DateTime sada)
    {
        ProveriPrelaz(Status == StatusOglasa.Nacrt, StatusOglasa.Aktivan);
        Status = StatusOglasa.Aktivan;
        ObjavljenoUtc = sada;
        IsticeUtc = sada.AddDays(PoslovnaPravila.TrajanjeOglasaDana);
    }

    public void Rezervisi()
    {
        ProveriPrelaz(Status == StatusOglasa.Aktivan, StatusOglasa.Rezervisan);
        Status = StatusOglasa.Rezervisan;
    }

    public void OtkaziRezervaciju()
    {
        ProveriPrelaz(Status == StatusOglasa.Rezervisan, StatusOglasa.Aktivan);
        Status = StatusOglasa.Aktivan;
    }

    public void OznaciKaoProdat()
    {
        ProveriPrelaz(Status is StatusOglasa.Aktivan or StatusOglasa.Rezervisan, StatusOglasa.Prodat);
        Status = StatusOglasa.Prodat;
    }

    /// <summary>Automatski istek (poziva ga pozadinski posao). Dozvoljen samo ako je rok zaista prošao.</summary>
    public void Istekni(DateTime sada)
    {
        ProveriPrelaz(Status == StatusOglasa.Aktivan, StatusOglasa.Istekao);
        Provera.Uslov(IsticeUtc <= sada, "oglas_nije_istekao", "Rok trajanja oglasa još nije prošao.");
        Status = StatusOglasa.Istekao;
    }

    /// <summary>
    /// Obnova: istekli oglas se vraća u Aktivan, ili se aktivnom oglasu produžava rok
    /// ako ističe za manje od 7 dana. Novi rok je uvek "sada + 60 dana".
    /// </summary>
    public void Obnovi(DateTime sada)
    {
        ProveriDaNijeObrisan();
        var aktivanPredIstekom = Status == StatusOglasa.Aktivan
            && IsticeUtc.HasValue
            && IsticeUtc.Value - sada <= TimeSpan.FromDays(PoslovnaPravila.ObnovaMogucaDanaPreIsteka);

        Provera.Uslov(Status == StatusOglasa.Istekao || aktivanPredIstekom, "nedozvoljen_prelaz",
            $"Oglas se može obnoviti samo kada je istekao ili kada ističe za manje od {PoslovnaPravila.ObnovaMogucaDanaPreIsteka} dana.");

        Status = StatusOglasa.Aktivan;
        IsticeUtc = sada.AddDays(PoslovnaPravila.TrajanjeOglasaDana);
    }

    /// <summary>Uklanjanje od strane moderatora (npr. posle prihvaćene prijave).</summary>
    public void Ukloni(string razlog)
    {
        ProveriPrelaz(Status is not (StatusOglasa.Prodat or StatusOglasa.Uklonjen), StatusOglasa.Uklonjen);
        Status = StatusOglasa.Uklonjen;
        RazlogUklanjanja = Provera.Tekst(razlog, "Razlog", 500);
    }

    /// <summary>Vlasnik briše oglas (meko brisanje). Nije dozvoljeno dok je oglas rezervisan.</summary>
    public void ObrisiOdStraneVlasnika(DateTime sada)
    {
        ProveriDaNijeObrisan();
        Provera.Uslov(Status != StatusOglasa.Rezervisan, "oglas_rezervisan",
            "Rezervisan oglas ne može da se obriše. Prvo otkažite rezervaciju.");
        Obrisan = true;
        ObrisanoUtc = sada;
    }

    public void ZabeleziPregled() => BrojPregleda++;

    public void PromeniBrojOmiljenih(int razlika) => BrojOmiljenih = Math.Max(0, BrojOmiljenih + razlika);

    // ---------------- Slike ----------------

    public SlikaOglasa DodajSliku(string putanja, string tipSadrzaja, long velicinaBajtova, DateTime sada)
    {
        ProveriDaSeMozeMenjati();
        Provera.Uslov(_slike.Count < PoslovnaPravila.MaksSlikaPoOglasu, "previse_slika",
            $"Oglas može imati najviše {PoslovnaPravila.MaksSlikaPoOglasu} slika.");
        Provera.Uslov(velicinaBajtova is > 0 and <= PoslovnaPravila.MaksVelicinaSlikeBajtova, "validacija",
            "Slika je prevelika (najviše 5 MB).");

        var redosled = _slike.Count == 0 ? 0 : _slike.Max(s => s.Redosled) + 1;
        var slika = new SlikaOglasa(this, putanja, tipSadrzaja, velicinaBajtova, redosled, sada);
        _slike.Add(slika);
        return slika;
    }

    /// <summary>
    /// Uklanja sliku i "pomera" naredne slike za jedno mesto unazad, da redosled ostane 0..n-1 bez rupa.
    /// EF Core ovo izvršava kao DELETE + UPDATE-e u pravilnom redosledu (poštuje UNIQUE indeks).
    /// </summary>
    public SlikaOglasa UkloniSliku(int slikaId)
    {
        ProveriDaSeMozeMenjati();
        var slika = _slike.FirstOrDefault(s => s.Id == slikaId)
            ?? throw new DomenskiIzuzetak("slika_ne_postoji", "Slika ne pripada ovom oglasu.");

        _slike.Remove(slika);
        foreach (var s in _slike.Where(s => s.Redosled > slika.Redosled).OrderBy(s => s.Redosled))
        {
            s.Redosled--;
        }

        return slika;
    }

    /// <summary>
    /// Proverava da je novi redosled permutacija postojećih slika. Samu izmenu izvršava repozitorijum
    /// JEDNOM UPDATE naredbom (SQL Server proverava UNIQUE indeks na kraju naredbe, pa zamena
    /// mesta dve slike ne krši ograničenje, što ne bi bilo moguće sa više pojedinačnih UPDATE-a).
    /// </summary>
    public IReadOnlyDictionary<int, int> PripremiNoviRedosled(IReadOnlyList<int> slikeIdURedosledu)
    {
        ProveriDaSeMozeMenjati();
        var postojeci = _slike.Select(s => s.Id).OrderBy(id => id).ToList();
        var novi = slikeIdURedosledu.OrderBy(id => id).ToList();
        Provera.Uslov(postojeci.SequenceEqual(novi), "neispravan_redosled",
            "Novi redosled mora sadržati tačno sve slike oglasa, svaku jednom.");

        var mapa = new Dictionary<int, int>();
        for (var i = 0; i < slikeIdURedosledu.Count; i++)
        {
            mapa[slikeIdURedosledu[i]] = i;
        }

        // Namerno NE menjamo Redosled u memoriji: kada bi EF pratio te izmene, pokušao bi
        // pojedinačne UPDATE-e i naišao na ciklus u UNIQUE indeksu.
        return mapa;
    }

    // ---------------- Atributi ----------------

    /// <summary>
    /// Postavlja vrednosti atributa. Proverava: atribut pripada kategoriji oglasa (ili nadređenoj),
    /// vrednost odgovara tipu, i sva obavezna polja su popunjena.
    /// </summary>
    public void PostaviAtribute(IReadOnlyDictionary<int, string?> vrednosti, IReadOnlyCollection<DefinicijaAtributa> definicijeKategorije)
    {
        ProveriDaSeMozeMenjati();
        foreach (var (definicijaId, _) in vrednosti)
        {
            Provera.Uslov(definicijeKategorije.Any(d => d.Id == definicijaId), "atribut_nepoznat",
                $"Atribut {definicijaId} ne pripada izabranoj kategoriji.");
        }

        foreach (var definicija in definicijeKategorije)
        {
            vrednosti.TryGetValue(definicija.Id, out var vrednost);
            Provera.Uslov(definicija.JeValidnaVrednost(vrednost), "atribut_nevalidan",
                definicija.Obavezan && string.IsNullOrWhiteSpace(vrednost)
                    ? $"Atribut '{definicija.Naziv}' je obavezan."
                    : $"Vrednost atributa '{definicija.Naziv}' nije ispravna.");
        }

        _atributi.RemoveAll(a => !vrednosti.ContainsKey(a.DefinicijaAtributaId)
                                 || string.IsNullOrWhiteSpace(vrednosti[a.DefinicijaAtributaId]));

        foreach (var (definicijaId, vrednost) in vrednosti)
        {
            if (string.IsNullOrWhiteSpace(vrednost))
            {
                continue;
            }

            var postojeci = _atributi.FirstOrDefault(a => a.DefinicijaAtributaId == definicijaId);
            if (postojeci is null)
            {
                _atributi.Add(new AtributOglasa(this, definicijaId, vrednost.Trim()));
            }
            else
            {
                postojeci.Vrednost = vrednost.Trim();
            }
        }
    }

    // ---------------- Pomoćne metode ----------------

    private void PostaviPodatke(int kategorijaId, int gradId, string naslov, string opis, TipOglasa tip,
        decimal? cena, StanjeProizvoda stanje, bool dostavaMoguca, bool licnoPreuzimanjeMoguce, string? zeljenaZamena)
    {
        Provera.Uslov(dostavaMoguca || licnoPreuzimanjeMoguce, "validacija",
            "Mora biti omogućena bar dostava ili lično preuzimanje.");

        switch (tip)
        {
            case TipOglasa.Prodaja:
                Provera.Uslov(cena is > 0 and <= PoslovnaPravila.MaksCena, "validacija",
                    "Za prodaju je obavezna cena veća od 0.");
                break;
            case TipOglasa.Razmena:
            case TipOglasa.Poklon:
                Provera.Uslov(cena is null or 0, "validacija",
                    "Oglas za razmenu ili poklon ne sme imati cenu.");
                cena = null;
                break;
        }

        KategorijaId = kategorijaId;
        GradId = gradId;
        Naslov = Provera.Tekst(naslov, "Naslov", 120);
        Opis = Provera.Tekst(opis, "Opis", 4000);
        Tip = tip;
        Cena = cena;
        Stanje = stanje;
        DostavaMoguca = dostavaMoguca;
        LicnoPreuzimanjeMoguce = licnoPreuzimanjeMoguce;
        ZeljenaZamena = tip == TipOglasa.Razmena ? Provera.OpcioniTekst(zeljenaZamena, "Željena zamena", 300) : null;
    }

    private void ProveriDaSeMozeMenjati()
    {
        ProveriDaNijeObrisan();
        Provera.Uslov(Status is StatusOglasa.Nacrt or StatusOglasa.Aktivan or StatusOglasa.Istekao,
            "oglas_zakljucan", $"Oglas u statusu '{Status}' ne može da se menja.");
    }

    private void ProveriDaNijeObrisan() =>
        Provera.Uslov(!Obrisan, "oglas_obrisan", "Oglas je obrisan.");

    private void ProveriPrelaz(bool dozvoljen, StatusOglasa ciljni)
    {
        ProveriDaNijeObrisan();
        Provera.Uslov(dozvoljen, "nedozvoljen_prelaz",
            $"Prelaz statusa oglasa iz '{Status}' u '{ciljni}' nije dozvoljen.");
    }
}
