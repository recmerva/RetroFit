namespace RetroFit.Application.Common.Izuzeci;

// Aplikacioni izuzeci. Globalni exception handler (API sloj) svaki pretvara u odgovarajući
// HTTP status i ProblemDetails odgovor, pa handleri ne moraju da znaju ništa o HTTP-u.

/// <summary>404 - traženi resurs ne postoji (ili korisnik nema pravo da zna da postoji).</summary>
public class NijePronadjenoIzuzetak(string resurs, object kljuc)
    : Exception($"{resurs} sa identifikatorom '{kljuc}' nije pronađen.")
{
    public string Resurs { get; } = resurs;
}

/// <summary>403 - korisnik je prijavljen, ali nema pravo na akciju.</summary>
public class ZabranjenoIzuzetak(string poruka = "Nemate pravo da izvršite ovu akciju.") : Exception(poruka);

/// <summary>401 - neispravni kredencijali ili nevažeći token.</summary>
public class NeovlascenIzuzetak(string poruka = "Neispravni podaci za prijavu.") : Exception(poruka);

/// <summary>
/// 409 - konflikt: istovremena izmena (RowVersion), kršenje UNIQUE ograničenja u bazi
/// (npr. neko je u istom trenutku rezervisao isti oglas) ili duplikat.
/// </summary>
public class KonfliktIzuzetak(string poruka) : Exception(poruka);

/// <summary>429 / 403 - zahtev je blokiran iz bezbednosnih razloga (blokiran nalog ili uređaj).</summary>
public class BlokiranIzuzetak(string poruka) : Exception(poruka);
