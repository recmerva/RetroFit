using AutoMapper;
using RetroFit.Application.Features.Administracija;
using RetroFit.Application.Features.Katalog;
using RetroFit.Application.Features.Korisnici;
using RetroFit.Application.Features.Moderacija;
using RetroFit.Application.Features.Obavestenja;
using RetroFit.Application.Features.Oglasi;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Domain.Entiteti;

namespace RetroFit.Application.Common.Mapiranje;

/// <summary>
/// AutoMapper mape entitet -> DTO. Koriste se na dva načina:
///  1) mapper.Map(entitet) u handlerima komandi (vraćanje rezultata posle izmene);
///  2) ProjectTo u repozitorijumima - AutoMapper iz mape pravi LINQ Select izraz, pa SQL vraća
///     samo kolone koje DTO koristi (bez učitavanja celih entiteta).
///
/// MaxDepth(3) na svim mapama ograničava dubinu rekurzije (mitigacija GHSA-rvv3-g6hj-g44x).
/// Privatna polja (LozinkaHash, Telefon, TokenHash...) namerno NE postoje u javnim DTO-ima.
/// </summary>
public sealed class MapiranjeProfil : Profile
{
    public MapiranjeProfil()
    {
        CreateMap<Grad, GradDto>().MaxDepth(3);

        CreateMap<DefinicijaAtributa, DefinicijaAtributaDto>()
            .ForMember(d => d.Opcije, o => o.MapFrom(s => s.ListaOpcija))
            .MaxDepth(3);

        CreateMap<Korisnik, KorisnikKratkoDto>()
            .ForMember(d => d.PrikaznoIme, o => o.MapFrom(s => s.Obrisan ? "Obrisan korisnik" : s.Ime + " " + s.Prezime.Substring(0, 1) + "."))
            .ForMember(d => d.TelefonVerifikovan, o => o.MapFrom(s => s.TelefonVerifikovanUtc != null))
            .MaxDepth(3);

        CreateMap<SlikaOglasa, SlikaDto>().MaxDepth(3);

        CreateMap<SacuvanaPretraga, SacuvanaPretragaDto>()
            .ForMember(d => d.KategorijaNaziv, o => o.MapFrom(s => s.Kategorija != null ? s.Kategorija.Naziv : null))
            .ForMember(d => d.GradNaziv, o => o.MapFrom(s => s.Grad != null ? s.Grad.Naziv : null))
            .MaxDepth(3);

        CreateMap<Poruka, PorukaDto>().MaxDepth(3);

        CreateMap<Placanje, PlacanjeDto>().MaxDepth(3);

        CreateMap<Obavestenje, ObavestenjeDto>().MaxDepth(3);

        CreateMap<AuditZapis, AuditZapisDto>()
            .ForMember(d => d.KorisnikEmail, o => o.MapFrom(s => s.Korisnik != null ? s.Korisnik.Email : null))
            .MaxDepth(3);

        CreateMap<BlokadaUredjaja, BlokadaUredjajaDto>()
            .ForMember(d => d.OtisakSkraceno, o => o.MapFrom(s => s.OtisakUredjaja.OtisakHash.Substring(0, 12)))
            .MaxDepth(3);

        CreateMap<Recenzija, RecenzijaDto>()
            .ForMember(d => d.OcenjivacIme, o => o.MapFrom(s => s.Ocenjivac.Ime + " " + s.Ocenjivac.Prezime.Substring(0, 1) + "."))
            .ForMember(d => d.OglasNaslov, o => o.MapFrom(s => s.Transakcija.Oglas.Naslov))
            .ForMember(d => d.UlogaOcenjivaca, o => o.MapFrom(s => s.OcenjivacId == s.Transakcija.KupacId ? "Kupac" : "Prodavac"))
            .MaxDepth(3);
    }
}
