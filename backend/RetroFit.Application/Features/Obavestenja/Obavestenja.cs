using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Izuzeci;
using RetroFit.Application.Common.Modeli;

namespace RetroFit.Application.Features.Obavestenja;

public sealed record ObavestenjaQuery(bool SamoNeprocitana, Stranicenje Stranicenje) : IRequest<StranicaRezultata<ObavestenjeDto>>;

internal sealed class ObavestenjaHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<ObavestenjaQuery, StranicaRezultata<ObavestenjeDto>>
{
    public Task<StranicaRezultata<ObavestenjeDto>> Handle(ObavestenjaQuery z, CancellationToken ct) =>
        uow.Obavestenja.ZaKorisnikaAsync(trenutni.ObavezanId, z.SamoNeprocitana, z.Stranicenje, ct);
}

/// <summary>Brojači za navigaciju (zvonce, poruke, ponude) - frontend ih periodično osvežava.</summary>
public sealed record BrojaciQuery : IRequest<BrojaciDto>;

internal sealed class BrojaciHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni) : IRequestHandler<BrojaciQuery, BrojaciDto>
{
    public async Task<BrojaciDto> Handle(BrojaciQuery z, CancellationToken ct)
    {
        var id = trenutni.ObavezanId;
        // Upiti se izvršavaju redom: jedan DbContext ne sme da izvršava više upita istovremeno.
        var obavestenja = await uow.Obavestenja.BrojNeprocitanihAsync(id, ct);
        var poruke = await uow.Razgovori.BrojNeprocitanihAsync(id, ct);
        var ponude = await uow.Ponude.BrojZaMojOdgovorAsync(id, ct);
        return new BrojaciDto(obavestenja, poruke, ponude);
    }
}

public sealed record OznaciObavestenjeProcitanimCommand(int Id) : IRequest<Unit>;

internal sealed class OznaciObavestenjeProcitanimHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OznaciObavestenjeProcitanimCommand, Unit>
{
    public async Task<Unit> Handle(OznaciObavestenjeProcitanimCommand z, CancellationToken ct)
    {
        var obavestenje = await uow.Obavestenja.PronadjiPoIdAsync(z.Id, ct);
        if (obavestenje is null || obavestenje.KorisnikId != trenutni.ObavezanId)
        {
            throw new NijePronadjenoIzuzetak("Obaveštenje", z.Id);
        }

        obavestenje.OznaciProcitanim(vreme.SadaUtc);
        await uow.SacuvajAsync(ct);
        return Unit.Value;
    }
}

public sealed record OznaciSvaObavestenjaProcitanimCommand : IRequest<int>;

internal sealed class OznaciSvaObavestenjaProcitanimHandler(IUnitOfWork uow, ITrenutniKorisnik trenutni, IDatumVreme vreme)
    : IRequestHandler<OznaciSvaObavestenjaProcitanimCommand, int>
{
    public Task<int> Handle(OznaciSvaObavestenjaProcitanimCommand z, CancellationToken ct) =>
        uow.Obavestenja.OznaciSvaProcitanimAsync(trenutni.ObavezanId, vreme.SadaUtc, ct);
}
