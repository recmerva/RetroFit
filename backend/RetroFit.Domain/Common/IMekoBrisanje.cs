namespace RetroFit.Domain.Common;

/// <summary>
/// Meko brisanje (soft delete): red ostaje u bazi, samo se označi kao obrisan.
/// Koristi se za entitete na koje se oslanja istorija (npr. oglas koji je prodat
/// i dalje mora postojati zbog transakcije i recenzija).
/// </summary>
public interface IMekoBrisanje
{
    bool Obrisan { get; }
    DateTime? ObrisanoUtc { get; }
}
