using MediatR;
using RetroFit.Application.Common.Interfejsi;
using RetroFit.Application.Common.Interfejsi.Persistencija;
using RetroFit.Application.Common.Modeli;

namespace RetroFit.Application.Features.Administracija;

public sealed record StatistikaQuery : IRequest<StatistikaDto>;

internal sealed class StatistikaHandler(IUnitOfWork uow, IDatumVreme vreme) : IRequestHandler<StatistikaQuery, StatistikaDto>
{
    public Task<StatistikaDto> Handle(StatistikaQuery z, CancellationToken ct) => uow.Statistika.IzracunajAsync(vreme.SadaUtc, ct);
}

public sealed record AuditQuery(FilterAudita Filter, Stranicenje Stranicenje) : IRequest<StranicaRezultata<AuditZapisDto>>;

internal sealed class AuditHandler(IUnitOfWork uow) : IRequestHandler<AuditQuery, StranicaRezultata<AuditZapisDto>>
{
    public Task<StranicaRezultata<AuditZapisDto>> Handle(AuditQuery z, CancellationToken ct) => uow.Audit.ListaAsync(z.Filter, z.Stranicenje, ct);
}
