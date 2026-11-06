using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroFit.Application.Common.Modeli;
using RetroFit.Application.Features.Razgovori;
using RetroFit.Application.Features.Trgovina;
using RetroFit.Domain.Enumi;

namespace RetroFit.API.Kontroleri;

public sealed record IznosZahtev(decimal Iznos, string? Napomena);
public sealed record RazlogZahtev(string? Razlog);
public sealed record KupovinaZahtev(NacinPreuzimanja NacinPreuzimanja, string? AdresaDostave, bool KoristiEscrow);
public sealed record UplataZahtev(string? KljucIdempotentnosti);
public sealed record RecenzijaZahtev(int Ocena, string? Komentar);

/// <summary>Ponude, rezervacije, kupovina (transakcije sa mock escrow-om) i recenzije.</summary>
[Route(Osnova)]
[Authorize]
public sealed class TrgovinaKontroler : ApiKontroler
{
    // ---------------- Ponude ----------------

    /// <summary>Davanje ponude cene za oglas (automatski otvara razgovor ako ne postoji).</summary>
    [HttpPost("oglasi/{oglasId:int}/ponude")]
    [ProducesResponseType(typeof(PonudaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PonudaDto>> DajPonudu(int oglasId, IznosZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(new DajPonuduCommand(oglasId, z.Iznos, z.Napomena), ct));

    /// <summary>Moje ponude (kaoKupac = true: ponude koje sam dao; false: ponude za moje oglase; prazno: sve).</summary>
    [HttpGet("ponude")]
    [ProducesResponseType(typeof(StranicaRezultata<PonudaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<PonudaDto>>> Ponude([FromQuery] bool? kaoKupac, [FromQuery] StatusPonude? status,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new PonudeQuery(kaoKupac, status, Stranica(stranica, velicina)), ct));

    /// <summary>Prihvatanje ponude: nastaje rezervacija (72h), oglas postaje Rezervisan, ostale ponude se odbijaju.</summary>
    [HttpPost("ponude/{id:int}/prihvati")]
    [ProducesResponseType(typeof(RezervacijaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RezervacijaDto>> Prihvati(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PrihvatiPonuduCommand(id), ct));

    /// <summary>Odbijanje ponude.</summary>
    [HttpPost("ponude/{id:int}/odbij")]
    [ProducesResponseType(typeof(PonudaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PonudaDto>> Odbij(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new OdbijPonuduCommand(id), ct));

    /// <summary>Kontraponuda: stara ponuda dobija status Kontraponuda, nastaje nova na koju odgovara druga strana.</summary>
    [HttpPost("ponude/{id:int}/kontraponuda")]
    [ProducesResponseType(typeof(PonudaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PonudaDto>> Kontraponuda(int id, IznosZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(new KontraponudaCommand(id, z.Iznos, z.Napomena), ct));

    // ---------------- Rezervacije ----------------

    /// <summary>Prodavac rezerviše oglas za kupca iz razgovora (po ceni iz oglasa).</summary>
    [HttpPost("razgovori/{razgovorId:int}/rezervacija")]
    [ProducesResponseType(typeof(RezervacijaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RezervacijaDto>> Rezervisi(int razgovorId, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(new RezervisiZaKupcaCommand(razgovorId), ct));

    /// <summary>Moje rezervacije (kao kupac ili prodavac).</summary>
    [HttpGet("rezervacije")]
    [ProducesResponseType(typeof(StranicaRezultata<RezervacijaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<RezervacijaDto>>> Rezervacije([FromQuery] StatusRezervacije? status,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new RezervacijeQuery(status, Stranica(stranica, velicina)), ct));

    [HttpGet("rezervacije/{id:int}")]
    [ProducesResponseType(typeof(RezervacijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RezervacijaDto>> Rezervacija(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new RezervacijaQuery(id), ct));

    /// <summary>Otkazivanje aktivne rezervacije (kupac ili prodavac); oglas se vraća u Aktivan.</summary>
    [HttpPost("rezervacije/{id:int}/otkazi")]
    [ProducesResponseType(typeof(RezervacijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RezervacijaDto>> OtkaziRezervaciju(int id, RazlogZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new OtkaziRezervacijuCommand(id, z.Razlog), ct));

    /// <summary>Kupac pokreće kupovinu iz rezervacije: bira način preuzimanja i da li koristi escrow.</summary>
    [HttpPost("rezervacije/{id:int}/kupovina")]
    [ProducesResponseType(typeof(TransakcijaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransakcijaDto>> ZapocniKupovinu(int id, KupovinaZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created,
            await Mediator.Send(new ZapocniKupovinuCommand(id, z.NacinPreuzimanja, z.AdresaDostave, z.KoristiEscrow), ct));

    // ---------------- Transakcije ----------------

    /// <summary>Istorija mojih transakcija.</summary>
    [HttpGet("transakcije")]
    [ProducesResponseType(typeof(StranicaRezultata<TransakcijaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<TransakcijaDto>>> Transakcije([FromQuery] StatusTransakcije? status,
        [FromQuery] int stranica = 1, [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new TransakcijeQuery(status, Stranica(stranica, velicina)), ct));

    /// <summary>Detalji transakcije sa istorijom escrow plaćanja.</summary>
    [HttpGet("transakcije/{id:int}")]
    [ProducesResponseType(typeof(TransakcijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransakcijaDto>> Transakcija(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new TransakcijaQuery(id), ct));

    /// <summary>Kupac uplaćuje na escrow (mock plaćanje). Novac se drži dok obe strane ne potvrde.</summary>
    [HttpPost("transakcije/{id:int}/uplata")]
    [ProducesResponseType(typeof(TransakcijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransakcijaDto>> Uplati(int id, UplataZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new UplatiNaEscrowCommand(id, z.KljucIdempotentnosti), ct));

    /// <summary>Potvrda (prodavac: predao/poslao; kupac: primio). Kada obe strane potvrde, kupovina je završena.</summary>
    [HttpPost("transakcije/{id:int}/potvrdi")]
    [ProducesResponseType(typeof(TransakcijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransakcijaDto>> Potvrdi(int id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PotvrdiTransakcijuCommand(id), ct));

    /// <summary>Otkazivanje kupovine (deponovan novac se vraća kupcu, oglas ponovo postaje aktivan).</summary>
    [HttpPost("transakcije/{id:int}/otkazi")]
    [ProducesResponseType(typeof(TransakcijaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransakcijaDto>> OtkaziTransakciju(int id, RazlogZahtev z, CancellationToken ct) =>
        Ok(await Mediator.Send(new OtkaziTransakcijuCommand(id, z.Razlog), ct));

    // ---------------- Recenzije ----------------

    /// <summary>Recenzija druge strane posle ZAVRŠENE transakcije (najviše jedna po korisniku po transakciji).</summary>
    [HttpPost("transakcije/{id:int}/recenzija")]
    [ProducesResponseType(typeof(RecenzijaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RecenzijaDto>> OstaviRecenziju(int id, RecenzijaZahtev z, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(new OstaviRecenzijuCommand(id, z.Ocena, z.Komentar), ct));

    /// <summary>Recenzije koje je korisnik dobio (javno).</summary>
    [HttpGet("korisnici/{korisnikId:int}/recenzije")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StranicaRezultata<RecenzijaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<StranicaRezultata<RecenzijaDto>>> Recenzije(int korisnikId, [FromQuery] int stranica = 1,
        [FromQuery] int velicina = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new RecenzijeKorisnikaQuery(korisnikId, Stranica(stranica, velicina)), ct));

    /// <summary>Sažetak ocena korisnika (prosek i raspodela 1-5).</summary>
    [HttpGet("korisnici/{korisnikId:int}/ocene")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SazetakOcenaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SazetakOcenaDto>> Ocene(int korisnikId, CancellationToken ct) =>
        Ok(await Mediator.Send(new SazetakOcenaQuery(korisnikId), ct));
}
