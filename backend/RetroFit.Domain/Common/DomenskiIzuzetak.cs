namespace RetroFit.Domain.Common;

/// <summary>
/// Izuzetak koji označava kršenje poslovnog pravila (npr. nedozvoljen prelaz statusa).
/// Globalni exception handler ga pretvara u HTTP 409/422 ProblemDetails odgovor.
/// </summary>
public class DomenskiIzuzetak : Exception
{
    public string Kod { get; }

    public DomenskiIzuzetak(string kod, string poruka) : base(poruka)
    {
        Kod = kod;
    }
}
