using System.Linq.Expressions;

namespace RetroFit.Application.Common.Interfejsi.Persistencija;

/// <summary>
/// Generički repozitorijum sa osnovnim operacijama. Specifični repozitorijumi ga proširuju
/// metodama koje zahtevaju Include, projekcije ili set-based naredbe.
///
/// Repozitorijum NE poziva SaveChanges: to radi isključivo IUnitOfWork, pa jedna poslovna
/// operacija (npr. prihvatanje ponude) menja više tabela u JEDNOJ transakciji.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> PronadjiPoIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> PronadjiAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default);
    Task<T?> PrviAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default);
    Task<bool> PostojiAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default);
    Task<int> PrebrojAsync(Expression<Func<T, bool>> uslov, CancellationToken ct = default);
    Task DodajAsync(T entitet, CancellationToken ct = default);
    void Ukloni(T entitet);
}
