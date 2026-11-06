using System.Data;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Domain.Entiteti;

namespace RetroFit.Tests.Pomocne;

/// <summary>
/// Moq "kostur" za IUnitOfWork: svaki repozitorijum je poseban mock, a UTransakcijiAsync
/// samo izvršava prosleđenu operaciju (u jediničnim testovima nema prave baze).
/// </summary>
internal sealed class LazniUnitOfWork
{
    public Mock<IUnitOfWork> Uow { get; } = new();
    public Mock<IKorisnikRepository> Korisnici { get; } = new();
    public Mock<IRepository<Uloga>> Uloge { get; } = new();
    public Mock<IRefreshTokenRepository> RefreshTokeni { get; } = new();
    public Mock<IBlokadaRepository> Blokade { get; } = new();
    public Mock<IOglasRepository> Oglasi { get; } = new();
    public Mock<IOmiljeniRepository> Omiljeni { get; } = new();
    public Mock<IRazgovorRepository> Razgovori { get; } = new();
    public Mock<IPonudaRepository> Ponude { get; } = new();
    public Mock<IRezervacijaRepository> Rezervacije { get; } = new();
    public Mock<ITransakcijaRepository> Transakcije { get; } = new();
    public Mock<IRecenzijaRepository> Recenzije { get; } = new();
    public Mock<IObavestenjeRepository> Obavestenja { get; } = new();
    public Mock<IOtisakUredjajaRepository> OtisciUredjaja { get; } = new();
    public Mock<IGradRepository> Gradovi { get; } = new();
    public Mock<IKategorijaRepository> Kategorije { get; } = new();

    public LazniUnitOfWork()
    {
        Uow.SetupGet(u => u.Korisnici).Returns(Korisnici.Object);
        Uow.SetupGet(u => u.Uloge).Returns(Uloge.Object);
        Uow.SetupGet(u => u.RefreshTokeni).Returns(RefreshTokeni.Object);
        Uow.SetupGet(u => u.Blokade).Returns(Blokade.Object);
        Uow.SetupGet(u => u.Oglasi).Returns(Oglasi.Object);
        Uow.SetupGet(u => u.Omiljeni).Returns(Omiljeni.Object);
        Uow.SetupGet(u => u.Razgovori).Returns(Razgovori.Object);
        Uow.SetupGet(u => u.Ponude).Returns(Ponude.Object);
        Uow.SetupGet(u => u.Rezervacije).Returns(Rezervacije.Object);
        Uow.SetupGet(u => u.Transakcije).Returns(Transakcije.Object);
        Uow.SetupGet(u => u.Recenzije).Returns(Recenzije.Object);
        Uow.SetupGet(u => u.Obavestenja).Returns(Obavestenja.Object);
        Uow.SetupGet(u => u.OtisciUredjaja).Returns(OtisciUredjaja.Object);
        Uow.SetupGet(u => u.Gradovi).Returns(Gradovi.Object);
        Uow.SetupGet(u => u.Kategorije).Returns(Kategorije.Object);
        Uow.Setup(u => u.SacuvajAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        Uow.Setup(u => u.UTransakcijiAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<int>> operacija, IsolationLevel _, CancellationToken ct) => operacija(ct));
    }

    public IUnitOfWork Object => Uow.Object;
}

internal sealed class FiksnoVreme(DateTime sada) : IDatumVreme
{
    public DateTime SadaUtc { get; set; } = sada;
}

internal static class LazniKorisnik
{
    public static ITrenutniKorisnik Za(int? id, params string[] uloge)
    {
        var m = new Mock<ITrenutniKorisnik>();
        m.SetupGet(k => k.Id).Returns(id);
        m.SetupGet(k => k.JePrijavljen).Returns(id is not null);
        m.SetupGet(k => k.ObavezanId).Returns(() => id ?? throw new RetroFit.Application.Common.Izuzeci.NeovlascenIzuzetak());
        m.Setup(k => k.JeUUlozi(It.IsAny<string>())).Returns((string u) => uloge.Contains(u));
        return m.Object;
    }
}
