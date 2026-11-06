namespace RetroFit.Application.Common.Modeli;

/// <summary>Rezultat sa straničenjem (paginacijom).</summary>
public sealed record StranicaRezultata<T>(IReadOnlyList<T> Stavke, int Stranica, int VelicinaStranice, int UkupnoStavki)
{
    public int UkupnoStranica => VelicinaStranice == 0 ? 0 : (int)Math.Ceiling(UkupnoStavki / (double)VelicinaStranice);
    public bool ImaSledecu => Stranica < UkupnoStranica;
    public bool ImaPrethodnu => Stranica > 1;

    public static StranicaRezultata<T> Prazna(int stranica, int velicina) => new([], stranica, velicina, 0);
}

/// <summary>Parametri straničenja sa sigurnim granicama (zaštita od zahteva tipa velicina=1000000).</summary>
public record Stranicenje
{
    public const int MaksVelicina = 50;

    private readonly int _stranica = 1;
    private readonly int _velicina = 20;

    public int Stranica
    {
        get => _stranica;
        init => _stranica = Math.Max(1, value);
    }

    public int VelicinaStranice
    {
        get => _velicina;
        init => _velicina = Math.Clamp(value, 1, MaksVelicina);
    }

    public int Preskoci => (Stranica - 1) * VelicinaStranice;
}
