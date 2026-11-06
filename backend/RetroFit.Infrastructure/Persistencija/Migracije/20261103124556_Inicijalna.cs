using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetroFit.Infrastructure.Persistencija.Migracije
{
    /// <inheritdoc />
    public partial class Inicijalna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gradovi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostanskiBroj = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    GeografskaSirina = table.Column<double>(type: "float", nullable: false),
                    GeografskaDuzina = table.Column<double>(type: "float", nullable: false),
                    Lokacija = table.Column<Point>(type: "geography", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gradovi", x => x.Id);
                    table.CheckConstraint("CK_Gradovi_Duzina", "[GeografskaDuzina] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_Gradovi_Sirina", "[GeografskaSirina] BETWEEN -90 AND 90");
                });

            migrationBuilder.CreateTable(
                name: "Kategorije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Ikonica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Redosled = table.Column<int>(type: "int", nullable: false),
                    Aktivna = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RoditeljId = table.Column<int>(type: "int", nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IzmenjenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    IzmenioKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategorije", x => x.Id);
                    table.CheckConstraint("CK_Kategorije_NijeSamaSebiRoditelj", "[RoditeljId] IS NULL OR [RoditeljId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_Kategorije_Kategorije_RoditeljId",
                        column: x => x.RoditeljId,
                        principalTable: "Kategorije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Uloge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uloge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizovanEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LozinkaHash = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Ime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OKorisniku = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GradId = table.Column<int>(type: "int", nullable: true),
                    Telefon = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    TelefonVerifikovanUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PrikaziTelefonKupcu = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProsecnaOcena = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    BrojRecenzija = table.Column<int>(type: "int", nullable: false),
                    BrojZavrsenihTransakcija = table.Column<int>(type: "int", nullable: false),
                    PoslednjaPrijavaUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Obrisan = table.Column<bool>(type: "bit", nullable: false),
                    ObrisanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IzmenjenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    IzmenioKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.Id);
                    table.CheckConstraint("CK_Korisnici_BrojRecenzija", "[BrojRecenzija] >= 0");
                    table.CheckConstraint("CK_Korisnici_BrojTransakcija", "[BrojZavrsenihTransakcija] >= 0");
                    table.CheckConstraint("CK_Korisnici_ProsecnaOcena", "[ProsecnaOcena] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_Korisnici_TelefonVerifikacija", "[TelefonVerifikovanUtc] IS NULL OR [Telefon] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Korisnici_Gradovi_GradId",
                        column: x => x.GradId,
                        principalTable: "Gradovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefinicijeAtributa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KategorijaId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kljuc = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Obavezan = table.Column<bool>(type: "bit", nullable: false),
                    Opcije = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JedinicaMere = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Redosled = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefinicijeAtributa", x => x.Id);
                    table.CheckConstraint("CK_DefinicijeAtributa_Opcije", "([Tip] = 'Izbor' AND [Opcije] IS NOT NULL) OR ([Tip] <> 'Izbor' AND [Opcije] IS NULL)");
                    table.CheckConstraint("CK_DefinicijeAtributa_Tip", "[Tip] IN ('Tekst', 'Broj', 'DaNe', 'Izbor')");
                    table.ForeignKey(
                        name: "FK_DefinicijeAtributa_Kategorije_KategorijaId",
                        column: x => x.KategorijaId,
                        principalTable: "Kategorije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditZapisi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kategorija = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Akcija = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NazivEntiteta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntitetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KorisnikId = table.Column<int>(type: "int", nullable: true),
                    StareVrednosti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoveVrednosti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAdresa = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    KorelacioniId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditZapisi", x => x.Id);
                    table.CheckConstraint("CK_AuditZapisi_Kategorija", "[Kategorija] IN ('Podaci', 'Bezbednost', 'Moderacija', 'Administracija')");
                    table.CheckConstraint("CK_AuditZapisi_NoveJson", "[NoveVrednosti] IS NULL OR ISJSON([NoveVrednosti]) = 1");
                    table.CheckConstraint("CK_AuditZapisi_StareJson", "[StareVrednosti] IS NULL OR ISJSON([StareVrednosti]) = 1");
                    table.ForeignKey(
                        name: "FK_AuditZapisi_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BlokiraniKorisnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    BlokiraoKorisnikId = table.Column<int>(type: "int", nullable: false),
                    Razlog = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PocetakUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    KrajUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UkinutoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UkinuoKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlokiraniKorisnici", x => x.Id);
                    table.CheckConstraint("CK_BlokiraniKorisnici_Kraj", "[KrajUtc] IS NULL OR [KrajUtc] > [PocetakUtc]");
                    table.CheckConstraint("CK_BlokiraniKorisnici_NeSebe", "[KorisnikId] <> [BlokiraoKorisnikId]");
                    table.ForeignKey(
                        name: "FK_BlokiraniKorisnici_Korisnici_BlokiraoKorisnikId",
                        column: x => x.BlokiraoKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlokiraniKorisnici_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlokiraniKorisnici_Korisnici_UkinuoKorisnikId",
                        column: x => x.UkinuoKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KorisnikUloge",
                columns: table => new
                {
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    UlogaId = table.Column<int>(type: "int", nullable: false),
                    DodeljenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KorisnikUloge", x => new { x.KorisnikId, x.UlogaId });
                    table.ForeignKey(
                        name: "FK_KorisnikUloge_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KorisnikUloge_Uloge_UlogaId",
                        column: x => x.UlogaId,
                        principalTable: "Uloge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Obavestenja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Naslov = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ProcitanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obavestenja", x => x.Id);
                    table.CheckConstraint("CK_Obavestenja_Tip", "[Tip] IN ('NovaPoruka', 'NovaPonuda', 'PonudaPrihvacena', 'PonudaOdbijena', 'Kontraponuda', 'PonudaIstekla', 'RezervacijaKreirana', 'RezervacijaOtkazana', 'RezervacijaIstekla', 'TransakcijaAzurirana', 'TransakcijaZavrsena', 'NovaRecenzija', 'PoklapanjePretrage', 'OdgovorNaZahtev', 'OglasIstekao', 'OglasUklonjen', 'NalogBlokiran', 'Sistemsko')");
                    table.ForeignKey(
                        name: "FK_Obavestenja_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Oglasi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProdavacId = table.Column<int>(type: "int", nullable: false),
                    KategorijaId = table.Column<int>(type: "int", nullable: false),
                    GradId = table.Column<int>(type: "int", nullable: false),
                    Naslov = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Stanje = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ZeljenaZamena = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DostavaMoguca = table.Column<bool>(type: "bit", nullable: false),
                    LicnoPreuzimanjeMoguce = table.Column<bool>(type: "bit", nullable: false),
                    ObjavljenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsticeUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RazlogUklanjanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BrojPregleda = table.Column<int>(type: "int", nullable: false),
                    BrojOmiljenih = table.Column<int>(type: "int", nullable: false),
                    Obrisan = table.Column<bool>(type: "bit", nullable: false),
                    ObrisanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IzmenjenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    IzmenioKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oglasi", x => x.Id);
                    table.CheckConstraint("CK_Oglasi_Brojaci", "[BrojPregleda] >= 0 AND [BrojOmiljenih] >= 0");
                    table.CheckConstraint("CK_Oglasi_CenaPoTipu", "([Tip] = 'Prodaja' AND [Cena] IS NOT NULL AND [Cena] > 0) OR ([Tip] IN ('Razmena', 'Poklon') AND [Cena] IS NULL)");
                    table.CheckConstraint("CK_Oglasi_DatumiObjave", "[Status] = 'Nacrt' OR ([ObjavljenoUtc] IS NOT NULL AND [IsticeUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Oglasi_IstekPosleObjave", "[IsticeUtc] IS NULL OR [IsticeUtc] > [ObjavljenoUtc]");
                    table.CheckConstraint("CK_Oglasi_NacinPreuzimanja", "[DostavaMoguca] = 1 OR [LicnoPreuzimanjeMoguce] = 1");
                    table.CheckConstraint("CK_Oglasi_Stanje", "[Stanje] IN ('Novo', 'KaoNovo', 'Polovno', 'ZaDelove')");
                    table.CheckConstraint("CK_Oglasi_Status", "[Status] IN ('Nacrt', 'Aktivan', 'Rezervisan', 'Prodat', 'Istekao', 'Uklonjen')");
                    table.CheckConstraint("CK_Oglasi_Tip", "[Tip] IN ('Prodaja', 'Razmena', 'Poklon')");
                    table.ForeignKey(
                        name: "FK_Oglasi_Gradovi_GradId",
                        column: x => x.GradId,
                        principalTable: "Gradovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oglasi_Kategorije_KategorijaId",
                        column: x => x.KategorijaId,
                        principalTable: "Kategorije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oglasi_Korisnici_ProdavacId",
                        column: x => x.ProdavacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtisciUredjaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OtisakHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OpisKlijenta = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrviPutVidjenUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PoslednjiPutVidjenUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    BrojZahteva = table.Column<long>(type: "bigint", nullable: false),
                    BrojRegistracija = table.Column<int>(type: "int", nullable: false),
                    NajveciRizik = table.Column<int>(type: "int", nullable: false),
                    PoslednjiKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtisciUredjaja", x => x.Id);
                    table.CheckConstraint("CK_OtisciUredjaja_Brojaci", "[BrojZahteva] >= 0 AND [BrojRegistracija] >= 0");
                    table.CheckConstraint("CK_OtisciUredjaja_Rizik", "[NajveciRizik] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_OtisciUredjaja_Korisnici_PoslednjiKorisnikId",
                        column: x => x.PoslednjiKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokeni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    PorodicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IsticeUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    OpozvanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RazlogOpoziva = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ZamenjenHashom = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    KreiranSaIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokeni", x => x.Id);
                    table.CheckConstraint("CK_RefreshTokeni_Istek", "[IsticeUtc] > [KreiranoUtc]");
                    table.ForeignKey(
                        name: "FK_RefreshTokeni_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SacuvanePretrage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Upit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KategorijaId = table.Column<int>(type: "int", nullable: true),
                    GradId = table.Column<int>(type: "int", nullable: true),
                    CenaOd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CenaDo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Stanje = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Obavestavaj = table.Column<bool>(type: "bit", nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PoslednjaProveraUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SacuvanePretrage", x => x.Id);
                    table.CheckConstraint("CK_SacuvanePretrage_Cene", "[CenaOd] IS NULL OR [CenaDo] IS NULL OR [CenaOd] <= [CenaDo]");
                    table.CheckConstraint("CK_SacuvanePretrage_Stanje", "([Stanje] IS NULL OR [Stanje] IN ('Novo', 'KaoNovo', 'Polovno', 'ZaDelove'))");
                    table.CheckConstraint("CK_SacuvanePretrage_Tip", "([Tip] IS NULL OR [Tip] IN ('Prodaja', 'Razmena', 'Poklon'))");
                    table.ForeignKey(
                        name: "FK_SacuvanePretrage_Gradovi_GradId",
                        column: x => x.GradId,
                        principalTable: "Gradovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SacuvanePretrage_Kategorije_KategorijaId",
                        column: x => x.KategorijaId,
                        principalTable: "Kategorije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SacuvanePretrage_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerifikacijeKorisnika",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Vrednost = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KodHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IsticeUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    BrojPokusaja = table.Column<int>(type: "int", nullable: false),
                    IskorisceneUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifikacijeKorisnika", x => x.Id);
                    table.CheckConstraint("CK_VerifikacijeKorisnika_Pokusaji", "[BrojPokusaja] >= 0");
                    table.CheckConstraint("CK_VerifikacijeKorisnika_Tip", "[Tip] IN ('Telefon')");
                    table.ForeignKey(
                        name: "FK_VerifikacijeKorisnika_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZahteviTrazim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    Naslov = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    KategorijaId = table.Column<int>(type: "int", nullable: false),
                    GradId = table.Column<int>(type: "int", nullable: true),
                    MaksimalnaCena = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IzmenjenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    IzmenioKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZahteviTrazim", x => x.Id);
                    table.CheckConstraint("CK_ZahteviTrazim_Cena", "[MaksimalnaCena] IS NULL OR [MaksimalnaCena] > 0");
                    table.CheckConstraint("CK_ZahteviTrazim_Status", "[Status] IN ('Otvoren', 'Ispunjen', 'Zatvoren')");
                    table.ForeignKey(
                        name: "FK_ZahteviTrazim_Gradovi_GradId",
                        column: x => x.GradId,
                        principalTable: "Gradovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZahteviTrazim_Kategorije_KategorijaId",
                        column: x => x.KategorijaId,
                        principalTable: "Kategorije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZahteviTrazim_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AtributiOglasa",
                columns: table => new
                {
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    DefinicijaAtributaId = table.Column<int>(type: "int", nullable: false),
                    Vrednost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtributiOglasa", x => new { x.OglasId, x.DefinicijaAtributaId });
                    table.ForeignKey(
                        name: "FK_AtributiOglasa_DefinicijeAtributa_DefinicijaAtributaId",
                        column: x => x.DefinicijaAtributaId,
                        principalTable: "DefinicijeAtributa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AtributiOglasa_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OmiljeniOglasi",
                columns: table => new
                {
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    DodatoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OmiljeniOglasi", x => new { x.KorisnikId, x.OglasId });
                    table.ForeignKey(
                        name: "FK_OmiljeniOglasi_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OmiljeniOglasi_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prijave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    PrijavioKorisnikId = table.Column<int>(type: "int", nullable: false),
                    Razlog = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ObradioModeratorId = table.Column<int>(type: "int", nullable: true),
                    ObradjenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    NapomenaModeratora = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prijave", x => x.Id);
                    table.CheckConstraint("CK_Prijave_Obrada", "[Status] = 'Otvorena' OR ([ObradioModeratorId] IS NOT NULL AND [ObradjenoUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Prijave_Razlog", "[Razlog] IN ('Prevara', 'ZabranjenSadrzaj', 'PogresnaKategorija', 'Duplikat', 'Spam', 'UvredljivSadrzaj', 'Ostalo')");
                    table.CheckConstraint("CK_Prijave_Status", "[Status] IN ('Otvorena', 'Prihvacena', 'Odbijena')");
                    table.ForeignKey(
                        name: "FK_Prijave_Korisnici_ObradioModeratorId",
                        column: x => x.ObradioModeratorId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prijave_Korisnici_PrijavioKorisnikId",
                        column: x => x.PrijavioKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prijave_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Razgovori",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    KupacId = table.Column<int>(type: "int", nullable: false),
                    ProdavacId = table.Column<int>(type: "int", nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PoslednjaPorukaUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Razgovori", x => x.Id);
                    table.CheckConstraint("CK_Razgovori_RazlicitiUcesnici", "[KupacId] <> [ProdavacId]");
                    table.ForeignKey(
                        name: "FK_Razgovori_Korisnici_KupacId",
                        column: x => x.KupacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Razgovori_Korisnici_ProdavacId",
                        column: x => x.ProdavacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Razgovori_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlikeOglasa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    Putanja = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TipSadrzaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    VelicinaBajtova = table.Column<long>(type: "bigint", nullable: false),
                    Redosled = table.Column<int>(type: "int", nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlikeOglasa", x => x.Id);
                    table.CheckConstraint("CK_SlikeOglasa_Redosled", "[Redosled] BETWEEN 0 AND 9");
                    table.CheckConstraint("CK_SlikeOglasa_Tip", "[TipSadrzaja] IN ('image/jpeg', 'image/png', 'image/webp')");
                    table.CheckConstraint("CK_SlikeOglasa_Velicina", "[VelicinaBajtova] > 0 AND [VelicinaBajtova] <= 5242880");
                    table.ForeignKey(
                        name: "FK_SlikeOglasa_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlokiraniUredjaji",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OtisakUredjajaId = table.Column<int>(type: "int", nullable: false),
                    Razlog = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PocetakUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    KrajUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Automatska = table.Column<bool>(type: "bit", nullable: false),
                    BlokiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    UkinutoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UkinuoKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlokiraniUredjaji", x => x.Id);
                    table.CheckConstraint("CK_BlokiraniUredjaji_Automatska", "([Automatska] = 1 AND [BlokiraoKorisnikId] IS NULL) OR ([Automatska] = 0 AND [BlokiraoKorisnikId] IS NOT NULL)");
                    table.CheckConstraint("CK_BlokiraniUredjaji_Kraj", "[KrajUtc] IS NULL OR [KrajUtc] > [PocetakUtc]");
                    table.ForeignKey(
                        name: "FK_BlokiraniUredjaji_Korisnici_BlokiraoKorisnikId",
                        column: x => x.BlokiraoKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlokiraniUredjaji_Korisnici_UkinuoKorisnikId",
                        column: x => x.UkinuoKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlokiraniUredjaji_OtisciUredjaja_OtisakUredjajaId",
                        column: x => x.OtisakUredjajaId,
                        principalTable: "OtisciUredjaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OdgovoriNaZahteve",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZahtevId = table.Column<int>(type: "int", nullable: false),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    OdgovorioKorisnikId = table.Column<int>(type: "int", nullable: false),
                    Poruka = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdgovoriNaZahteve", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdgovoriNaZahteve_Korisnici_OdgovorioKorisnikId",
                        column: x => x.OdgovorioKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdgovoriNaZahteve_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdgovoriNaZahteve_ZahteviTrazim_ZahtevId",
                        column: x => x.ZahtevId,
                        principalTable: "ZahteviTrazim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ponude",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    RazgovorId = table.Column<int>(type: "int", nullable: false),
                    KupacId = table.Column<int>(type: "int", nullable: false),
                    PonudioKorisnikId = table.Column<int>(type: "int", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Napomena = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IsticeUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    OdgovorenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PrethodnaPonudaId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ponude", x => x.Id);
                    table.CheckConstraint("CK_Ponude_Istek", "[IsticeUtc] > [KreiranoUtc]");
                    table.CheckConstraint("CK_Ponude_Iznos", "[Iznos] > 0");
                    table.CheckConstraint("CK_Ponude_NijeSamaSebiPrethodna", "[PrethodnaPonudaId] IS NULL OR [PrethodnaPonudaId] <> [Id]");
                    table.CheckConstraint("CK_Ponude_Status", "[Status] IN ('NaCekanju', 'Prihvacena', 'Odbijena', 'Kontraponuda', 'Istekla')");
                    table.ForeignKey(
                        name: "FK_Ponude_Korisnici_KupacId",
                        column: x => x.KupacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ponude_Korisnici_PonudioKorisnikId",
                        column: x => x.PonudioKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ponude_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ponude_Ponude_PrethodnaPonudaId",
                        column: x => x.PrethodnaPonudaId,
                        principalTable: "Ponude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ponude_Razgovori_RazgovorId",
                        column: x => x.RazgovorId,
                        principalTable: "Razgovori",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Poruke",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazgovorId = table.Column<int>(type: "int", nullable: false),
                    PosiljalacId = table.Column<int>(type: "int", nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PoslatoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ProcitanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PonudaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poruke", x => x.Id);
                    table.CheckConstraint("CK_Poruke_Tip", "[Tip] IN ('Tekst', 'BrzaPoruka', 'Sistemska')");
                    table.ForeignKey(
                        name: "FK_Poruke_Korisnici_PosiljalacId",
                        column: x => x.PosiljalacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Poruke_Ponude_PonudaId",
                        column: x => x.PonudaId,
                        principalTable: "Ponude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Poruke_Razgovori_RazgovorId",
                        column: x => x.RazgovorId,
                        principalTable: "Razgovori",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rezervacije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    KupacId = table.Column<int>(type: "int", nullable: false),
                    ProdavacId = table.Column<int>(type: "int", nullable: false),
                    PonudaId = table.Column<int>(type: "int", nullable: true),
                    DogovorenaCena = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IsticeUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ZavrsenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    OtkazaoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    RazlogOtkazivanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervacije", x => x.Id);
                    table.CheckConstraint("CK_Rezervacije_Cena", "[DogovorenaCena] IS NULL OR [DogovorenaCena] > 0");
                    table.CheckConstraint("CK_Rezervacije_Istek", "[IsticeUtc] > [KreiranoUtc]");
                    table.CheckConstraint("CK_Rezervacije_RazlicitiUcesnici", "[KupacId] <> [ProdavacId]");
                    table.CheckConstraint("CK_Rezervacije_Status", "[Status] IN ('Aktivna', 'Realizovana', 'Otkazana', 'Istekla')");
                    table.CheckConstraint("CK_Rezervacije_Zavrsetak", "[Status] = 'Aktivna' OR [ZavrsenoUtc] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Rezervacije_Korisnici_KupacId",
                        column: x => x.KupacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacije_Korisnici_ProdavacId",
                        column: x => x.ProdavacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacije_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacije_Ponude_PonudaId",
                        column: x => x.PonudaId,
                        principalTable: "Ponude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transakcije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    KupacId = table.Column<int>(type: "int", nullable: false),
                    ProdavacId = table.Column<int>(type: "int", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NacinPreuzimanja = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    AdresaDostave = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    StatusEscrowa = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    KupacPotvrdioUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ProdavacPotvrdioUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ZavrsenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    OtkazanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    OtkazaoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    RazlogOtkazivanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IzmenjenoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    IzmenioKorisnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transakcije", x => x.Id);
                    table.CheckConstraint("CK_Transakcije_AdresaZaDostavu", "[NacinPreuzimanja] <> 'Dostava' OR [AdresaDostave] IS NOT NULL");
                    table.CheckConstraint("CK_Transakcije_Cena", "[Cena] IS NULL OR [Cena] > 0");
                    table.CheckConstraint("CK_Transakcije_EscrowZahtevaCenu", "[StatusEscrowa] = 'NijeKoriscen' OR [Cena] IS NOT NULL");
                    table.CheckConstraint("CK_Transakcije_NacinPreuzimanja", "[NacinPreuzimanja] IN ('LicnoPreuzimanje', 'Dostava')");
                    table.CheckConstraint("CK_Transakcije_Otkazana", "[Status] <> 'Otkazana' OR [OtkazanoUtc] IS NOT NULL");
                    table.CheckConstraint("CK_Transakcije_RazlicitiUcesnici", "[KupacId] <> [ProdavacId]");
                    table.CheckConstraint("CK_Transakcije_Status", "[Status] IN ('UToku', 'Zavrsena', 'Otkazana')");
                    table.CheckConstraint("CK_Transakcije_StatusEscrowa", "[StatusEscrowa] IN ('NijeKoriscen', 'CekaUplatu', 'Deponovano', 'Isplaceno', 'Vraceno')");
                    table.CheckConstraint("CK_Transakcije_Zavrsena", "[Status] <> 'Zavrsena' OR ([KupacPotvrdioUtc] IS NOT NULL AND [ProdavacPotvrdioUtc] IS NOT NULL AND [ZavrsenoUtc] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Transakcije_Korisnici_KupacId",
                        column: x => x.KupacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transakcije_Korisnici_ProdavacId",
                        column: x => x.ProdavacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transakcije_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transakcije_Rezervacije_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Placanja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    TransakcijaId = table.Column<int>(type: "int", nullable: true),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Valuta = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "RSD"),
                    ReferencaProcesora = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    KljucIdempotentnosti = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    Opis = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Placanja", x => x.Id);
                    table.CheckConstraint("CK_Placanja_EscrowTransakcija", "[Tip] = 'Isticanje' OR [TransakcijaId] IS NOT NULL");
                    table.CheckConstraint("CK_Placanja_Iznos", "[Iznos] > 0");
                    table.CheckConstraint("CK_Placanja_Status", "[Status] IN ('Uspesno', 'Neuspesno')");
                    table.CheckConstraint("CK_Placanja_Tip", "[Tip] IN ('EscrowUplata', 'EscrowIsplata', 'EscrowPovracaj', 'Isticanje')");
                    table.ForeignKey(
                        name: "FK_Placanja_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Placanja_Transakcije_TransakcijaId",
                        column: x => x.TransakcijaId,
                        principalTable: "Transakcije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recenzije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransakcijaId = table.Column<int>(type: "int", nullable: false),
                    OcenjivacId = table.Column<int>(type: "int", nullable: false),
                    OcenjeniKorisnikId = table.Column<int>(type: "int", nullable: false),
                    Ocena = table.Column<byte>(type: "tinyint", nullable: false),
                    Komentar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recenzije", x => x.Id);
                    table.CheckConstraint("CK_Recenzije_NeSebe", "[OcenjivacId] <> [OcenjeniKorisnikId]");
                    table.CheckConstraint("CK_Recenzije_Ocena", "[Ocena] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Recenzije_Korisnici_OcenjeniKorisnikId",
                        column: x => x.OcenjeniKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recenzije_Korisnici_OcenjivacId",
                        column: x => x.OcenjivacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recenzije_Transakcije_TransakcijaId",
                        column: x => x.TransakcijaId,
                        principalTable: "Transakcije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IstaknutiOglasi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PocetakUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    KrajUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: false),
                    PlacanjeId = table.Column<int>(type: "int", nullable: true),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    OtkazanoUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IstaknutiOglasi", x => x.Id);
                    table.CheckConstraint("CK_IstaknutiOglasi_Period", "[KrajUtc] > [PocetakUtc]");
                    table.CheckConstraint("CK_IstaknutiOglasi_Placanje", "([Tip] = 'Podignut' AND [PlacanjeId] IS NOT NULL) OR ([Tip] = 'Izdvojen' AND [PlacanjeId] IS NULL)");
                    table.CheckConstraint("CK_IstaknutiOglasi_Tip", "[Tip] IN ('Izdvojen', 'Podignut')");
                    table.ForeignKey(
                        name: "FK_IstaknutiOglasi_Korisnici_KreiraoKorisnikId",
                        column: x => x.KreiraoKorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IstaknutiOglasi_Oglasi_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IstaknutiOglasi_Placanja_PlacanjeId",
                        column: x => x.PlacanjeId,
                        principalTable: "Placanja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Gradovi",
                columns: new[] { "Id", "GeografskaDuzina", "GeografskaSirina", "Lokacija", "Naziv", "PostanskiBroj" },
                values: new object[,]
                {
                    { 1, 20.461200000000002, 44.8125, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.4612 44.8125)"), "Beograd", "11000" },
                    { 2, 19.833500000000001, 45.267099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8335 45.2671)"), "Novi Sad", "21000" },
                    { 3, 21.895800000000001, 43.320900000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.8958 43.3209)"), "Niš", "18000" },
                    { 4, 20.9114, 44.012799999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.9114 44.0128)"), "Kragujevac", "34000" },
                    { 5, 19.665800000000001, 46.100299999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.6658 46.1003)"), "Subotica", "24000" },
                    { 6, 20.368600000000001, 45.381599999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.3686 45.3816)"), "Zrenjanin", "23000" },
                    { 7, 20.6403, 44.870800000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6403 44.8708)"), "Pančevo", "26000" },
                    { 8, 20.349699999999999, 43.891399999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.3497 43.8914)"), "Čačak", "32000" },
                    { 9, 20.689399999999999, 43.7258, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6894 43.7258)"), "Kraljevo", "36000" },
                    { 10, 20.5122, 43.136699999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.5122 43.1367)"), "Novi Pazar", "36300" },
                    { 11, 20.93, 44.662799999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.93 44.6628)"), "Smederevo", "11300" },
                    { 12, 21.946100000000001, 42.998100000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.9461 42.9981)"), "Leskovac", "16000" },
                    { 13, 19.890000000000001, 44.274999999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.89 44.275)"), "Valjevo", "14000" },
                    { 14, 21.3339, 43.579999999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.3339 43.58)"), "Kruševac", "37000" },
                    { 15, 21.899999999999999, 42.551400000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.9 42.5514)"), "Vranje", "17500" },
                    { 16, 19.690799999999999, 44.748899999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.6908 44.7489)"), "Šabac", "15000" },
                    { 17, 19.842500000000001, 43.855600000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8425 43.8556)"), "Užice", "31000" },
                    { 18, 19.112200000000001, 45.7742, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.1122 45.7742)"), "Sombor", "25000" },
                    { 19, 21.187799999999999, 44.621299999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.1878 44.6213)"), "Požarevac", "12000" },
                    { 20, 22.586099999999998, 43.153100000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.5861 43.1531)"), "Pirot", "18300" },
                    { 21, 22.284700000000001, 43.903599999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.2847 43.9036)"), "Zaječar", "19000" },
                    { 22, 20.465299999999999, 45.829700000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.4653 45.8297)"), "Kikinda", "23300" },
                    { 23, 19.612200000000001, 44.976399999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.6122 44.9764)"), "Sremska Mitrovica", "22000" },
                    { 24, 21.261099999999999, 43.977200000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.2611 43.9772)"), "Jagodina", "35000" },
                    { 25, 21.303599999999999, 45.116700000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.3036 45.1167)"), "Vršac", "26300" },
                    { 26, 22.095600000000001, 44.0747, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.0956 44.0747)"), "Bor", "19210" },
                    { 27, 21.588100000000001, 43.234200000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.5881 43.2342)"), "Prokuplje", "18400" },
                    { 28, 19.223299999999998, 44.533299999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.2233 44.5333)"), "Loznica", "15300" }
                });

            migrationBuilder.InsertData(
                table: "Kategorije",
                columns: new[] { "Id", "Aktivna", "Ikonica", "IzmenioKorisnikId", "IzmenjenoUtc", "KreiranoUtc", "KreiraoKorisnikId", "Naziv", "Redosled", "RoditeljId", "Slug" },
                values: new object[,]
                {
                    { 1, true, "elektronika", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Elektronika", 1, null, "elektronika" },
                    { 2, true, "vozila", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Vozila", 2, null, "vozila" },
                    { 3, true, "odeca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Odeća i obuća", 3, null, "odeca-i-obuca" },
                    { 4, true, "kuca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kuća i bašta", 4, null, "kuca-i-basta" },
                    { 5, true, "sport", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sport i rekreacija", 5, null, "sport-i-rekreacija" },
                    { 6, true, "knjige", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Knjige, muzika i filmovi", 6, null, "knjige-muzika-filmovi" },
                    { 7, true, "retro", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Retro i kolekcionarstvo", 7, null, "retro-i-kolekcionarstvo" },
                    { 8, true, "deca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Bebe i deca", 8, null, "bebe-i-deca" },
                    { 9, true, "ljubimci", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kućni ljubimci", 9, null, "kucni-ljubimci" },
                    { 10, true, "ostalo", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ostalo", 10, null, "ostalo" }
                });

            migrationBuilder.InsertData(
                table: "Uloge",
                columns: new[] { "Id", "Naziv", "Opis" },
                values: new object[,]
                {
                    { 1, "Korisnik", "Običan korisnik: kupuje i prodaje." },
                    { 2, "Moderator", "Obrađuje prijave, uklanja oglase i blokira korisnike." },
                    { 3, "Admin", "Puna administracija: kategorije, uloge, statistika, audit." }
                });

            migrationBuilder.InsertData(
                table: "Kategorije",
                columns: new[] { "Id", "Aktivna", "Ikonica", "IzmenioKorisnikId", "IzmenjenoUtc", "KreiranoUtc", "KreiraoKorisnikId", "Naziv", "Redosled", "RoditeljId", "Slug" },
                values: new object[,]
                {
                    { 11, true, "telefon", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Mobilni telefoni", 1, 1, "mobilni-telefoni" },
                    { 12, true, "racunar", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Računari i laptopovi", 2, 1, "racunari-i-laptopovi" },
                    { 13, true, "tv", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TV i audio", 3, 1, "tv-i-audio" },
                    { 14, true, "kamera", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Foto i video", 4, 1, "foto-i-video" },
                    { 21, true, "automobil", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Automobili", 1, 2, "automobili" },
                    { 22, true, "motocikl", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Motocikli", 2, 2, "motocikli" },
                    { 23, true, "bicikl", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Bicikli", 3, 2, "bicikli" },
                    { 24, true, "alat", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Delovi i oprema za vozila", 4, 2, "delovi-i-oprema-za-vozila" },
                    { 31, true, "odeca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ženska odeća", 1, 3, "zenska-odeca" },
                    { 32, true, "odeca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Muška odeća", 2, 3, "muska-odeca" },
                    { 33, true, "obuca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Obuća", 3, 3, "obuca" },
                    { 34, true, "odeca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Dečja odeća", 4, 3, "decja-odeca" },
                    { 41, true, "namestaj", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Nameštaj", 1, 4, "namestaj" },
                    { 42, true, "aparat", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kućni aparati", 2, 4, "kucni-aparati" },
                    { 43, true, "alat", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Bašta i alat", 3, 4, "basta-i-alat" },
                    { 51, true, "fitnes", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Fitnes oprema", 1, 5, "fitnes-oprema" },
                    { 52, true, "kamp", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kampovanje", 2, 5, "kampovanje" },
                    { 53, true, "skije", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Zimski sportovi", 3, 5, "zimski-sportovi" },
                    { 61, true, "knjige", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Knjige", 1, 6, "knjige" },
                    { 62, true, "ploca", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Vinil i CD", 2, 6, "vinil-i-cd" },
                    { 63, true, "film", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Filmovi", 3, 6, "filmovi" },
                    { 71, true, "retro", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Antikviteti", 1, 7, "antikviteti" },
                    { 72, true, "konzola", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Retro konzole i igre", 2, 7, "retro-konzole-i-igre" },
                    { 73, true, "novcic", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Markice, novčići i nalepnice", 3, 7, "markice-novcici-nalepnice" },
                    { 81, true, "igracka", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Igračke", 1, 8, "igracke" },
                    { 82, true, "kolica", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kolica i oprema", 2, 8, "kolica-i-oprema" },
                    { 91, true, "ljubimci", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Oprema za ljubimce", 1, 9, "oprema-za-ljubimce" }
                });

            migrationBuilder.InsertData(
                table: "DefinicijeAtributa",
                columns: new[] { "Id", "JedinicaMere", "KategorijaId", "Kljuc", "Naziv", "Obavezan", "Opcije", "Redosled", "Tip" },
                values: new object[,]
                {
                    { 1, null, 11, "marka", "Marka", true, null, 1, "Tekst" },
                    { 2, null, 11, "memorija", "Memorija", false, "32 GB|64 GB|128 GB|256 GB|512 GB|1 TB", 2, "Izbor" },
                    { 3, null, 11, "boja", "Boja", false, null, 3, "Tekst" },
                    { 4, null, 12, "procesor", "Procesor", false, null, 1, "Tekst" },
                    { 5, "GB", 12, "ram", "RAM", false, null, 2, "Broj" },
                    { 6, "GB", 12, "skladiste", "Skladište", false, null, 3, "Broj" },
                    { 7, "inča", 13, "dijagonala", "Dijagonala", false, null, 1, "Broj" },
                    { 8, null, 21, "marka", "Marka", true, null, 1, "Tekst" },
                    { 9, null, 21, "godiste", "Godište", true, null, 2, "Broj" },
                    { 10, "km", 21, "kilometraza", "Kilometraža", true, null, 3, "Broj" },
                    { 11, null, 21, "gorivo", "Gorivo", true, "Benzin|Dizel|Benzin + gas|Električni|Hibrid", 4, "Izbor" },
                    { 12, null, 21, "menjac", "Menjač", false, "Manuelni|Automatski", 5, "Izbor" },
                    { 13, null, 22, "godiste", "Godište", true, null, 1, "Broj" },
                    { 14, "cm³", 22, "kubikaza", "Kubikaža", false, null, 2, "Broj" },
                    { 15, null, 23, "velicina-rama", "Veličina rama", false, "XS|S|M|L|XL", 1, "Izbor" },
                    { 16, null, 23, "elektricni", "Električni", false, null, 2, "DaNe" },
                    { 17, null, 31, "velicina", "Veličina", true, "XS|S|M|L|XL|XXL", 1, "Izbor" },
                    { 18, null, 32, "velicina", "Veličina", true, "XS|S|M|L|XL|XXL", 1, "Izbor" },
                    { 19, null, 33, "broj", "Broj", true, null, 1, "Broj" },
                    { 20, null, 34, "uzrast", "Uzrast", false, null, 1, "Tekst" },
                    { 21, null, 41, "materijal", "Materijal", false, null, 1, "Tekst" },
                    { 22, null, 61, "autor", "Autor", false, null, 1, "Tekst" },
                    { 23, null, 61, "jezik", "Jezik", false, "Srpski|Engleski|Ostalo", 2, "Izbor" },
                    { 24, null, 62, "izvodjac", "Izvođač", false, null, 1, "Tekst" },
                    { 25, null, 62, "format", "Format", false, "LP|EP|Singl|CD|Kaseta", 2, "Izbor" },
                    { 26, null, 72, "platforma", "Platforma", true, null, 1, "Tekst" },
                    { 27, null, 72, "originalna-kutija", "Originalna kutija", false, null, 2, "DaNe" },
                    { 28, null, 71, "godina", "Približna godina", false, null, 1, "Broj" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtributiOglasa_Definicija_Vrednost",
                table: "AtributiOglasa",
                columns: new[] { "DefinicijaAtributaId", "Vrednost" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditZapisi_Datum",
                table: "AuditZapisi",
                column: "KreiranoUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AuditZapisi_Entitet",
                table: "AuditZapisi",
                columns: new[] { "NazivEntiteta", "EntitetId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditZapisi_Kategorija_Datum",
                table: "AuditZapisi",
                columns: new[] { "Kategorija", "KreiranoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditZapisi_KorisnikId",
                table: "AuditZapisi",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_BlokiraniKorisnici_Aktivne",
                table: "BlokiraniKorisnici",
                columns: new[] { "KorisnikId", "UkinutoUtc", "KrajUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BlokiraniKorisnici_BlokiraoKorisnikId",
                table: "BlokiraniKorisnici",
                column: "BlokiraoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_BlokiraniKorisnici_UkinuoKorisnikId",
                table: "BlokiraniKorisnici",
                column: "UkinuoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_BlokiraniUredjaji_Aktivne",
                table: "BlokiraniUredjaji",
                columns: new[] { "OtisakUredjajaId", "UkinutoUtc", "KrajUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BlokiraniUredjaji_BlokiraoKorisnikId",
                table: "BlokiraniUredjaji",
                column: "BlokiraoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_BlokiraniUredjaji_UkinuoKorisnikId",
                table: "BlokiraniUredjaji",
                column: "UkinuoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "UX_DefinicijeAtributa_Kategorija_Kljuc",
                table: "DefinicijeAtributa",
                columns: new[] { "KategorijaId", "Kljuc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Gradovi_Naziv",
                table: "Gradovi",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IstaknutiOglasi_Aktivni",
                table: "IstaknutiOglasi",
                columns: new[] { "KrajUtc", "PocetakUtc" },
                filter: "[OtkazanoUtc] IS NULL")
                .Annotation("SqlServer:Include", new[] { "OglasId", "Tip" });

            migrationBuilder.CreateIndex(
                name: "IX_IstaknutiOglasi_KreiraoKorisnikId",
                table: "IstaknutiOglasi",
                column: "KreiraoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_IstaknutiOglasi_OglasId",
                table: "IstaknutiOglasi",
                column: "OglasId");

            migrationBuilder.CreateIndex(
                name: "UX_IstaknutiOglasi_PlacanjeId",
                table: "IstaknutiOglasi",
                column: "PlacanjeId",
                unique: true,
                filter: "[PlacanjeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Kategorije_Roditelj_Redosled",
                table: "Kategorije",
                columns: new[] { "RoditeljId", "Redosled" });

            migrationBuilder.CreateIndex(
                name: "UX_Kategorije_Slug",
                table: "Kategorije",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Korisnici_GradId",
                table: "Korisnici",
                column: "GradId");

            migrationBuilder.CreateIndex(
                name: "UX_Korisnici_NormalizovanEmail",
                table: "Korisnici",
                column: "NormalizovanEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Korisnici_Telefon",
                table: "Korisnici",
                column: "Telefon",
                unique: true,
                filter: "[Telefon] IS NOT NULL AND [Obrisan] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KorisnikUloge_UlogaId",
                table: "KorisnikUloge",
                column: "UlogaId");

            migrationBuilder.CreateIndex(
                name: "IX_Obavestenja_Korisnik_Datum",
                table: "Obavestenja",
                columns: new[] { "KorisnikId", "KreiranoUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Obavestenja_Neprocitana",
                table: "Obavestenja",
                column: "KorisnikId",
                filter: "[ProcitanoUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OdgovoriNaZahteve_OdgovorioKorisnikId",
                table: "OdgovoriNaZahteve",
                column: "OdgovorioKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_OdgovoriNaZahteve_OglasId",
                table: "OdgovoriNaZahteve",
                column: "OglasId");

            migrationBuilder.CreateIndex(
                name: "UX_OdgovoriNaZahteve_Zahtev_Oglas",
                table: "OdgovoriNaZahteve",
                columns: new[] { "ZahtevId", "OglasId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_GradId",
                table: "Oglasi",
                column: "GradId");

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_KategorijaId",
                table: "Oglasi",
                column: "KategorijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_Popularnost",
                table: "Oglasi",
                columns: new[] { "Status", "BrojOmiljenih", "BrojPregleda" },
                filter: "[Obrisan] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_Pretraga_Cena",
                table: "Oglasi",
                columns: new[] { "Status", "Cena" },
                filter: "[Obrisan] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_Pretraga_Grad",
                table: "Oglasi",
                columns: new[] { "Status", "GradId", "ObjavljenoUtc" },
                filter: "[Obrisan] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_Pretraga_Kategorija",
                table: "Oglasi",
                columns: new[] { "Status", "KategorijaId", "ObjavljenoUtc" },
                descending: new[] { false, false, true },
                filter: "[Obrisan] = 0")
                .Annotation("SqlServer:Include", new[] { "Cena", "GradId", "Stanje", "Tip" });

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_Prodavac_Status",
                table: "Oglasi",
                columns: new[] { "ProdavacId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Oglasi_ZaIstek",
                table: "Oglasi",
                column: "IsticeUtc",
                filter: "[Status] = 'Aktivan' AND [Obrisan] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OmiljeniOglasi_Korisnik_Datum",
                table: "OmiljeniOglasi",
                columns: new[] { "KorisnikId", "DodatoUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OmiljeniOglasi_OglasId",
                table: "OmiljeniOglasi",
                column: "OglasId");

            migrationBuilder.CreateIndex(
                name: "IX_OtisciUredjaja_PoslednjiKorisnikId",
                table: "OtisciUredjaja",
                column: "PoslednjiKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_OtisciUredjaja_Rizik",
                table: "OtisciUredjaja",
                column: "NajveciRizik");

            migrationBuilder.CreateIndex(
                name: "UX_OtisciUredjaja_Hash",
                table: "OtisciUredjaja",
                column: "OtisakHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Placanja_Korisnik",
                table: "Placanja",
                columns: new[] { "KorisnikId", "KreiranoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Placanja_TransakcijaId",
                table: "Placanja",
                column: "TransakcijaId");

            migrationBuilder.CreateIndex(
                name: "UX_Placanja_KljucIdempotentnosti",
                table: "Placanja",
                column: "KljucIdempotentnosti",
                unique: true,
                filter: "[KljucIdempotentnosti] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Placanja_Referenca",
                table: "Placanja",
                column: "ReferencaProcesora",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ponude_Kupac",
                table: "Ponude",
                columns: new[] { "KupacId", "KreiranoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Ponude_Oglas_Status",
                table: "Ponude",
                columns: new[] { "OglasId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Ponude_PonudioKorisnikId",
                table: "Ponude",
                column: "PonudioKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Ponude_PrethodnaPonudaId",
                table: "Ponude",
                column: "PrethodnaPonudaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ponude_ZaIstek",
                table: "Ponude",
                column: "IsticeUtc",
                filter: "[Status] = 'NaCekanju'");

            migrationBuilder.CreateIndex(
                name: "UX_Ponude_JednaNaCekanju",
                table: "Ponude",
                column: "RazgovorId",
                unique: true,
                filter: "[Status] = 'NaCekanju'");

            migrationBuilder.CreateIndex(
                name: "IX_Poruke_Neprocitane",
                table: "Poruke",
                columns: new[] { "RazgovorId", "PosiljalacId" },
                filter: "[ProcitanoUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Poruke_PonudaId",
                table: "Poruke",
                column: "PonudaId");

            migrationBuilder.CreateIndex(
                name: "IX_Poruke_PosiljalacId",
                table: "Poruke",
                column: "PosiljalacId");

            migrationBuilder.CreateIndex(
                name: "IX_Poruke_Razgovor_Vreme",
                table: "Poruke",
                columns: new[] { "RazgovorId", "PoslatoUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_ObradioModeratorId",
                table: "Prijave",
                column: "ObradioModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_PrijavioKorisnikId",
                table: "Prijave",
                column: "PrijavioKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_Status_Datum",
                table: "Prijave",
                columns: new[] { "Status", "KreiranoUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Prijave_JednaOtvorenaPoKorisniku",
                table: "Prijave",
                columns: new[] { "OglasId", "PrijavioKorisnikId" },
                unique: true,
                filter: "[Status] = 'Otvorena'");

            migrationBuilder.CreateIndex(
                name: "IX_Razgovori_Kupac",
                table: "Razgovori",
                columns: new[] { "KupacId", "PoslednjaPorukaUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Razgovori_Prodavac",
                table: "Razgovori",
                columns: new[] { "ProdavacId", "PoslednjaPorukaUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UX_Razgovori_Oglas_Kupac",
                table: "Razgovori",
                columns: new[] { "OglasId", "KupacId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recenzije_Ocenjeni",
                table: "Recenzije",
                columns: new[] { "OcenjeniKorisnikId", "KreiranoUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Recenzije_OcenjivacId",
                table: "Recenzije",
                column: "OcenjivacId");

            migrationBuilder.CreateIndex(
                name: "UX_Recenzije_Transakcija_Ocenjivac",
                table: "Recenzije",
                columns: new[] { "TransakcijaId", "OcenjivacId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokeni_Korisnik_Opozvano",
                table: "RefreshTokeni",
                columns: new[] { "KorisnikId", "OpozvanoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokeni_PorodicaId",
                table: "RefreshTokeni",
                column: "PorodicaId");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokeni_TokenHash",
                table: "RefreshTokeni",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacije_Kupac",
                table: "Rezervacije",
                columns: new[] { "KupacId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacije_Prodavac",
                table: "Rezervacije",
                columns: new[] { "ProdavacId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacije_ZaIstek",
                table: "Rezervacije",
                column: "IsticeUtc",
                filter: "[Status] = 'Aktivna'");

            migrationBuilder.CreateIndex(
                name: "UX_Rezervacije_JednaAktivnaPoOglasu",
                table: "Rezervacije",
                column: "OglasId",
                unique: true,
                filter: "[Status] = 'Aktivna'");

            migrationBuilder.CreateIndex(
                name: "UX_Rezervacije_PonudaId",
                table: "Rezervacije",
                column: "PonudaId",
                unique: true,
                filter: "[PonudaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SacuvanePretrage_GradId",
                table: "SacuvanePretrage",
                column: "GradId");

            migrationBuilder.CreateIndex(
                name: "IX_SacuvanePretrage_KategorijaId",
                table: "SacuvanePretrage",
                column: "KategorijaId");

            migrationBuilder.CreateIndex(
                name: "IX_SacuvanePretrage_KorisnikId",
                table: "SacuvanePretrage",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_SacuvanePretrage_ZaObavestavanje",
                table: "SacuvanePretrage",
                column: "PoslednjaProveraUtc",
                filter: "[Obavestavaj] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_SlikeOglasa_Oglas_Redosled",
                table: "SlikeOglasa",
                columns: new[] { "OglasId", "Redosled" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transakcije_Kupac",
                table: "Transakcije",
                columns: new[] { "KupacId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Transakcije_Prodavac",
                table: "Transakcije",
                columns: new[] { "ProdavacId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Transakcije_OglasNeotkazana",
                table: "Transakcije",
                column: "OglasId",
                unique: true,
                filter: "[Status] <> 'Otkazana'");

            migrationBuilder.CreateIndex(
                name: "UX_Transakcije_RezervacijaId",
                table: "Transakcije",
                column: "RezervacijaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Uloge_Naziv",
                table: "Uloge",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verifikacije_Korisnik_Tip",
                table: "VerifikacijeKorisnika",
                columns: new[] { "KorisnikId", "Tip", "KreiranoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ZahteviTrazim_GradId",
                table: "ZahteviTrazim",
                column: "GradId");

            migrationBuilder.CreateIndex(
                name: "IX_ZahteviTrazim_KategorijaId",
                table: "ZahteviTrazim",
                column: "KategorijaId");

            migrationBuilder.CreateIndex(
                name: "IX_ZahteviTrazim_KorisnikId",
                table: "ZahteviTrazim",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_ZahteviTrazim_Pretraga",
                table: "ZahteviTrazim",
                columns: new[] { "Status", "KategorijaId", "KreiranoUtc" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtributiOglasa");

            migrationBuilder.DropTable(
                name: "AuditZapisi");

            migrationBuilder.DropTable(
                name: "BlokiraniKorisnici");

            migrationBuilder.DropTable(
                name: "BlokiraniUredjaji");

            migrationBuilder.DropTable(
                name: "IstaknutiOglasi");

            migrationBuilder.DropTable(
                name: "KorisnikUloge");

            migrationBuilder.DropTable(
                name: "Obavestenja");

            migrationBuilder.DropTable(
                name: "OdgovoriNaZahteve");

            migrationBuilder.DropTable(
                name: "OmiljeniOglasi");

            migrationBuilder.DropTable(
                name: "Poruke");

            migrationBuilder.DropTable(
                name: "Prijave");

            migrationBuilder.DropTable(
                name: "Recenzije");

            migrationBuilder.DropTable(
                name: "RefreshTokeni");

            migrationBuilder.DropTable(
                name: "SacuvanePretrage");

            migrationBuilder.DropTable(
                name: "SlikeOglasa");

            migrationBuilder.DropTable(
                name: "VerifikacijeKorisnika");

            migrationBuilder.DropTable(
                name: "DefinicijeAtributa");

            migrationBuilder.DropTable(
                name: "OtisciUredjaja");

            migrationBuilder.DropTable(
                name: "Placanja");

            migrationBuilder.DropTable(
                name: "Uloge");

            migrationBuilder.DropTable(
                name: "ZahteviTrazim");

            migrationBuilder.DropTable(
                name: "Transakcije");

            migrationBuilder.DropTable(
                name: "Rezervacije");

            migrationBuilder.DropTable(
                name: "Ponude");

            migrationBuilder.DropTable(
                name: "Razgovori");

            migrationBuilder.DropTable(
                name: "Oglasi");

            migrationBuilder.DropTable(
                name: "Kategorije");

            migrationBuilder.DropTable(
                name: "Korisnici");

            migrationBuilder.DropTable(
                name: "Gradovi");
        }
    }
}
