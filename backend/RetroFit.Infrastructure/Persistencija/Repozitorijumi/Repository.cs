using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;

namespace RetroFit.Infrastructure.Persistencija.Repozitorijumi;

/// <summary>Generička implementacija repozitorijuma nad EF Core DbSet-om.</summary>
internal class Repository<T>(RetroFitDbContext db) : IRepository<T> where T : class
{
    protected readonly RetroFitDbContext Db = db;
    protected DbSet<T> Skup => Db.Set<T>();

    public virtual Task<T?> PronadjiPoIdAsync(int id, CancellationToken ct = default) =>
        Skup.FindAsync([id], ct).AsTask();

    public async Task<IReadOnlyList<T>> PronadjiAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default) =>
        await Skup.Where(uslov).ToListAsync(ct);

    public Task<T?> PrviAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default) =>
        Skup.FirstOrDefaultAsync(uslov, ct);

    public Task<bool> PostojiAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default) =>
        Skup.AnyAsync(uslov, ct);

    public Task<int> PrebrojAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default) =>
        Skup.CountAsync(uslov, ct);

    public async Task DodajAsync(T entitet, CancellationToken ct = default) =>
        await Skup.AddAsync(entitet, ct);

    public void Ukloni(T entitet) => Skup.Remove(entitet);
}

internal static class StranicenjeEkstenzije
{
    /// <summary>Izvršava dva upita: COUNT(*) za ukupan broj i OFFSET/FETCH za jednu stranicu.</summary>
    public static async Task<StranicaRezultata<T>> UStranicuAsync<T>(this IQueryable<T> upit, Stranicenje s, CancellationToken ct)
    {
        var ukupno = await upit.CountAsync(ct);
        if (ukupno == 0)
        {
            return StranicaRezultata<T>.Prazna(s.Stranica, s.VelicinaStranice);
        }

        var stavke = await upit.Skip(s.Preskoci).Take(s.VelicinaStranice).ToListAsync(ct);
        return new StranicaRezultata<T>(stavke, s.Stranica, s.VelicinaStranice, ukupno);
    }
}
