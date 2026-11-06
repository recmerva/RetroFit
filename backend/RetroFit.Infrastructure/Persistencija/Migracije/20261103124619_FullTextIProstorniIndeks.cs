using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroFit.Infrastructure.Persistencija.Migracije
{
    /// <summary>
    /// Ručno napisana migracija za objekte baze koje EF Core ne može da opiše fluent API-jem:
    ///
    /// 1) SPATIAL indeks nad Gradovi.Lokacija (tip geography) - ubrzava pretragu po udaljenosti
    ///    (STDistance / IsWithinDistance), jer baza ne mora da računa udaljenost do svakog grada.
    ///
    /// 2) FULL-TEXT katalog i indeksi nad Oglasi(Naslov, Opis) i ZahteviTrazim(Naslov, Opis),
    ///    sa srpskim (latinica, LCID 2074) razdvajačem reči i bez osetljivosti na kvačice.
    ///    Kreiraju se SAMO ako je Full-Text Search
    ///    komponenta instalirana na serveru; inače aplikacija koristi LIKE pretragu (vidi OglasRepository).
    ///    FULLTEXT DDL ne sme da se izvršava unutar transakcije, zato je suppressTransaction: true.
    /// </summary>
    public partial class FullTextIProstorniIndeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE SPATIAL INDEX SIX_Gradovi_Lokacija
                    ON dbo.Gradovi (Lokacija)
                    USING GEOGRAPHY_AUTO_GRID;
                """);

            migrationBuilder.Sql(
                """
                IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                BEGIN
                    DECLARE @jezik nvarchar(10) =
                        CASE WHEN EXISTS (SELECT 1 FROM sys.fulltext_languages WHERE lcid = 2074) THEN N'2074' ELSE N'0' END;

                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'RetroFitKatalog')
                        -- ACCENT_SENSITIVITY = OFF: "cacak" pronalazi i "Čačak" (korisnici često kucaju bez kvačica).
                        EXEC (N'CREATE FULLTEXT CATALOG RetroFitKatalog WITH ACCENT_SENSITIVITY = OFF AS DEFAULT;');

                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.Oglasi'))
                        EXEC (N'CREATE FULLTEXT INDEX ON dbo.Oglasi (Naslov LANGUAGE ' + @jezik + N', Opis LANGUAGE ' + @jezik + N')
                                KEY INDEX PK_Oglasi ON RetroFitKatalog
                                WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM;');

                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ZahteviTrazim'))
                        EXEC (N'CREATE FULLTEXT INDEX ON dbo.ZahteviTrazim (Naslov LANGUAGE ' + @jezik + N', Opis LANGUAGE ' + @jezik + N')
                                KEY INDEX PK_ZahteviTrazim ON RetroFitKatalog
                                WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM;');
                END
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.ZahteviTrazim'))
                    EXEC (N'DROP FULLTEXT INDEX ON dbo.ZahteviTrazim;');
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.Oglasi'))
                    EXEC (N'DROP FULLTEXT INDEX ON dbo.Oglasi;');
                IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'RetroFitKatalog')
                    EXEC (N'DROP FULLTEXT CATALOG RetroFitKatalog;');
                """,
                suppressTransaction: true);

            migrationBuilder.Sql("DROP INDEX IF EXISTS SIX_Gradovi_Lokacija ON dbo.Gradovi;");
        }
    }
}
