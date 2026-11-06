using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Domain.Common;
using RetroFit.Domain.Entiteti;
using RetroFit.Domain.Enumi;

namespace RetroFit.Infrastructure.Persistencija.Interceptori;

/// <summary>
/// EF Core interceptor koji se izvršava pri svakom SaveChanges:
///  1) popunjava audit polja (KreiranoUtc, IzmenjenoUtc, KreiraoKorisnikId, IzmenioKorisnikId);
///  2) za svaku izmenu "važnih" entiteta pravi AuditZapis sa starim i novim vrednostima (JSON).
///
/// Id novog reda je poznat tek POSLE INSERT-a, pa se audit zapisi upisuju u SavedChanges
/// (drugim SaveChanges-om, unutar iste transakcije ako je transakcija otvorena).
///
/// Privatnost: osetljiva polja se maskiraju, a neki entiteti se uopšte ne audituju
/// (sadržaj poruka, tokeni, verifikacioni kodovi), da audit log ne bi postao "curenje" podataka.
/// </summary>
public sealed class AuditInterceptor(IDatumVreme datumVreme, ITrenutniKorisnik trenutniKorisnik) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> NeAuditovati =
    [
        typeof(AuditZapis), typeof(Poruka), typeof(Obavestenje), typeof(RefreshToken), typeof(VerifikacijaKorisnika),
        typeof(OtisakUredjaja), typeof(OmiljeniOglas), typeof(SacuvanaPretraga), typeof(Razgovor)
    ];

    private static readonly HashSet<string> Maskirano =
        ["LozinkaHash", "Telefon", "AdresaDostave", "TokenHash", "KodHash", "Email", "NormalizovanEmail"];

    // Svojstva koja se često menjaju i nisu bitna za audit (brojači, tehnička polja).
    private static readonly HashSet<string> Zanemareno =
        ["BrojPregleda", "BrojOmiljenih", "RowVersion", "IzmenjenoUtc", "IzmenioKorisnikId", "PoslednjaPrijavaUtc", "Lokacija"];

    private static readonly JsonSerializerOptions JsonOpcije = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly List<(EntityEntry Unos, AuditZapis Zapis, bool JeNov)> _naCekanju = [];
    private bool _upisujemAudit;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && !_upisujemAudit)
        {
            PopuniAuditPolja(eventData.Context);
            PripremiAuditZapise(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null && !_upisujemAudit)
        {
            PopuniAuditPolja(eventData.Context);
            PripremiAuditZapise(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && !_upisujemAudit && _naCekanju.Count > 0)
        {
            var zapisi = ZavrsiZapise();
            try
            {
                _upisujemAudit = true;
                eventData.Context.Set<AuditZapis>().AddRange(zapisi);
                await eventData.Context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _upisujemAudit = false;
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null && !_upisujemAudit && _naCekanju.Count > 0)
        {
            var zapisi = ZavrsiZapise();
            try
            {
                _upisujemAudit = true;
                eventData.Context.Set<AuditZapis>().AddRange(zapisi);
                eventData.Context.SaveChanges();
            }
            finally
            {
                _upisujemAudit = false;
            }
        }

        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _naCekanju.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _naCekanju.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void PopuniAuditPolja(DbContext context)
    {
        var sada = datumVreme.SadaUtc;
        var korisnikId = trenutniKorisnik.Id;
        foreach (var unos in context.ChangeTracker.Entries<IAuditabilan>())
        {
            switch (unos.State)
            {
                case EntityState.Added:
                    unos.Entity.KreiranoUtc = sada;
                    unos.Entity.KreiraoKorisnikId ??= korisnikId;
                    break;
                case EntityState.Modified:
                    unos.Entity.IzmenjenoUtc = sada;
                    unos.Entity.IzmenioKorisnikId = korisnikId;
                    // Datum kreiranja nikad ne sme da se promeni.
                    unos.Property(nameof(IAuditabilan.KreiranoUtc)).IsModified = false;
                    unos.Property(nameof(IAuditabilan.KreiraoKorisnikId)).IsModified = false;
                    break;
            }
        }
    }

    private void PripremiAuditZapise(DbContext context)
    {
        _naCekanju.Clear();
        var sada = datumVreme.SadaUtc;
        foreach (var unos in context.ChangeTracker.Entries())
        {
            if (unos.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)
                || NeAuditovati.Contains(unos.Metadata.ClrType))
            {
                continue;
            }

            var stare = new Dictionary<string, object?>();
            var nove = new Dictionary<string, object?>();
            foreach (var svojstvo in unos.Properties)
            {
                var naziv = svojstvo.Metadata.Name;
                if (Zanemareno.Contains(naziv) || svojstvo.Metadata.IsPrimaryKey())
                {
                    continue;
                }

                switch (unos.State)
                {
                    case EntityState.Added:
                        nove[naziv] = Vrednost(naziv, svojstvo.CurrentValue);
                        break;
                    case EntityState.Deleted:
                        stare[naziv] = Vrednost(naziv, svojstvo.OriginalValue);
                        break;
                    case EntityState.Modified when svojstvo.IsModified && !Equals(svojstvo.OriginalValue, svojstvo.CurrentValue):
                        stare[naziv] = Vrednost(naziv, svojstvo.OriginalValue);
                        nove[naziv] = Vrednost(naziv, svojstvo.CurrentValue);
                        break;
                }
            }

            if (unos.State == EntityState.Modified && nove.Count == 0)
            {
                continue;
            }

            var akcija = unos.State switch
            {
                EntityState.Added => "Kreiranje",
                EntityState.Deleted => "Brisanje",
                _ => "Izmena"
            };

            var zapis = new AuditZapis(KategorijaAudita.Podaci, akcija, unos.Metadata.ClrType.Name, KljucEntiteta(unos),
                trenutniKorisnik.Id,
                stare.Count > 0 ? JsonSerializer.Serialize(stare, JsonOpcije) : null,
                nove.Count > 0 ? JsonSerializer.Serialize(nove, JsonOpcije) : null,
                trenutniKorisnik.IpAdresa, trenutniKorisnik.KorelacioniId, sada);

            _naCekanju.Add((unos, zapis, unos.State == EntityState.Added));
        }
    }

    private List<AuditZapis> ZavrsiZapise()
    {
        var rezultat = new List<AuditZapis>(_naCekanju.Count);
        foreach (var (unos, zapis, jeNov) in _naCekanju)
        {
            if (jeNov)
            {
                // Posle INSERT-a je Id poznat; postavljamo ga preko EF-a jer je setter privatan.
                var privremeni = new AuditZapis(zapis.Kategorija, zapis.Akcija, zapis.NazivEntiteta, KljucEntiteta(unos),
                    zapis.KorisnikId, zapis.StareVrednosti, zapis.NoveVrednosti, zapis.IpAdresa, zapis.KorelacioniId, zapis.KreiranoUtc);
                rezultat.Add(privremeni);
            }
            else
            {
                rezultat.Add(zapis);
            }
        }

        _naCekanju.Clear();
        return rezultat;
    }

    private static string KljucEntiteta(EntityEntry unos)
    {
        var kljuc = unos.Metadata.FindPrimaryKey();
        if (kljuc is null)
        {
            return string.Empty;
        }

        return string.Join("|", kljuc.Properties.Select(p => unos.Property(p.Name).CurrentValue?.ToString()));
    }

    private static object? Vrednost(string naziv, object? vrednost)
    {
        if (vrednost is null)
        {
            return null;
        }

        if (Maskirano.Contains(naziv))
        {
            return "***";
        }

        return vrednost switch
        {
            Enum e => e.ToString(),
            byte[] => null,
            DateTime d => d.ToString("O"),
            _ when vrednost.GetType().Namespace?.StartsWith("NetTopologySuite") == true => null,
            _ => vrednost
        };
    }
}
