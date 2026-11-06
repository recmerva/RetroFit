namespace RetroFit.Domain.Common;

/// <summary>
/// Entitet sa audit poljima. Polja automatski popunjava EF Core interceptor
/// (Infrastructure/Persistence/Interceptori/AuditInterceptor) pri svakom SaveChanges.
/// </summary>
public abstract class AuditabilniEntitet : Entitet, IAuditabilan
{
    public DateTime KreiranoUtc { get; set; }
    public DateTime? IzmenjenoUtc { get; set; }
    public int? KreiraoKorisnikId { get; set; }
    public int? IzmenioKorisnikId { get; set; }
}

public interface IAuditabilan
{
    DateTime KreiranoUtc { get; set; }
    DateTime? IzmenjenoUtc { get; set; }
    int? KreiraoKorisnikId { get; set; }
    int? IzmenioKorisnikId { get; set; }
}
