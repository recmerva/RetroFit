using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetroFit.Infrastructure.Persistencija.Migracije
{
    /// <inheritdoc />
    public partial class SviGradoviSrbije : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Gradovi",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Serbian_Latin_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.InsertData(
                table: "Gradovi",
                columns: new[] { "Id", "GeografskaDuzina", "GeografskaSirina", "Lokacija", "Naziv", "PostanskiBroj" },
                values: new object[,]
                {
                    { 29, 20.4161, 44.578600000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.4161 44.5786)"), "Barajevo", "11460" },
                    { 30, 20.717199999999998, 44.671399999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.7172 44.6714)"), "Grocka", "11306" },
                    { 31, 20.256399999999999, 44.380299999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.2564 44.3803)"), "Lazarevac", "11550" },
                    { 32, 20.696899999999999, 44.438099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6969 44.4381)"), "Mladenovac", "11400" },
                    { 33, 20.199999999999999, 44.655000000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.2 44.655)"), "Obrenovac", "11500" },
                    { 34, 20.575299999999999, 44.519399999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.5753 44.5194)"), "Sopot", "11450" },
                    { 35, 20.2803, 44.793100000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.2803 44.7931)"), "Surčin", "11271" },
                    { 36, 19.635000000000002, 45.814999999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.635 45.815)"), "Bačka Topola", "24300" },
                    { 37, 19.664999999999999, 45.7072, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.665 45.7072)"), "Mali Iđoš", "24321" },
                    { 38, 20.1356, 45.596699999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.1356 45.5967)"), "Novi Bečej", "23272" },
                    { 39, 20.6053, 45.668300000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6053 45.6683)"), "Nova Crnja", "23218" },
                    { 40, 20.550000000000001, 45.484999999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.55 45.485)"), "Žitište", "23210" },
                    { 41, 20.772500000000001, 45.366900000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.7725 45.3669)"), "Sečanj", "23240" },
                    { 42, 20.128900000000002, 45.802799999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.1289 45.8028)"), "Ada", "24430" },
                    { 43, 20.077200000000001, 45.927500000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0772 45.9275)"), "Senta", "24400" },
                    { 44, 20.050000000000001, 46.066699999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.05 46.0667)"), "Kanjiža", "24420" },
                    { 45, 20.100000000000001, 46.049999999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.1 46.05)"), "Novi Kneževac", "23330" },
                    { 46, 20.1433, 45.9422, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.1433 45.9422)"), "Čoka", "23320" },
                    { 47, 21.417200000000001, 44.897500000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.4172 44.8975)"), "Bela Crkva", "26340" },
                    { 48, 20.621400000000001, 45.111699999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6214 45.1117)"), "Kovačica", "26210" },
                    { 49, 20.976099999999999, 44.747500000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.9761 44.7475)"), "Kovin", "26220" },
                    { 50, 20.965800000000002, 45.080800000000004, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.9658 45.0808)"), "Alibunar", "26310" },
                    { 51, 20.430299999999999, 45.052199999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.4303 45.0522)"), "Opovo", "26204" },
                    { 52, 21.121700000000001, 45.226900000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.1217 45.2269)"), "Plandište", "26360" },
                    { 53, 18.984999999999999, 45.671100000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (18.985 45.6711)"), "Apatin", "25260" },
                    { 54, 19.527200000000001, 45.6083, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.5272 45.6083)"), "Kula", "25230" },
                    { 55, 19.261099999999999, 45.508299999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.2611 45.5083)"), "Odžaci", "25250" },
                    { 56, 19.238299999999999, 45.3919, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.2383 45.3919)"), "Bač", "21420" },
                    { 57, 19.3917, 45.25, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.3917 45.25)"), "Bačka Palanka", "21400" },
                    { 58, 19.591699999999999, 45.359999999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.5917 45.36)"), "Bački Petrovac", "21470" },
                    { 59, 20.033300000000001, 45.616700000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0333 45.6167)"), "Bečej", "21220" },
                    { 60, 19.720800000000001, 45.208300000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.7208 45.2083)"), "Beočin", "21300" },
                    { 61, 19.640799999999999, 45.571399999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.6408 45.5714)"), "Vrbas", "21460" },
                    { 62, 20.0639, 45.372199999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0639 45.3722)"), "Žabalj", "21230" },
                    { 63, 19.791699999999999, 45.5486, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.7917 45.5486)"), "Srbobran", "21480" },
                    { 64, 19.934200000000001, 45.202500000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.9342 45.2025)"), "Sremski Karlovci", "21205" },
                    { 65, 19.886700000000001, 45.408299999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8867 45.4083)"), "Temerin", "21235" },
                    { 66, 20.2944, 45.206099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.2944 45.2061)"), "Titel", "21240" },
                    { 67, 19.226400000000002, 45.128300000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.2264 45.1283)"), "Šid", "22240" },
                    { 68, 20.081700000000001, 45.048099999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0817 45.0481)"), "Inđija", "22320" },
                    { 69, 19.8583, 45.1006, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8583 45.1006)"), "Irig", "22406" },
                    { 70, 19.822199999999999, 45.008099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8222 45.0081)"), "Ruma", "22400" },
                    { 71, 20.160799999999998, 44.984999999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.1608 44.985)"), "Stara Pazova", "22300" },
                    { 72, 19.965599999999998, 44.908900000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.9656 44.9089)"), "Pećinci", "22410" },
                    { 73, 19.480799999999999, 44.837499999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.4808 44.8375)"), "Bogatić", "15350" },
                    { 74, 19.785599999999999, 44.613599999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.7856 44.6136)"), "Vladimirci", "15225" },
                    { 75, 19.823599999999999, 44.470799999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8236 44.4708)"), "Koceljeva", "15220" },
                    { 76, 19.1067, 44.3733, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.1067 44.3733)"), "Mali Zvornik", "15318" },
                    { 77, 19.362200000000001, 44.366700000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.3622 44.3667)"), "Krupanj", "15314" },
                    { 78, 19.372199999999999, 44.187199999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.3722 44.1872)"), "Ljubovija", "15320" },
                    { 79, 19.601400000000002, 44.372799999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.6014 44.3728)"), "Osečina", "14253" },
                    { 80, 20.073899999999998, 44.456099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0739 44.4561)"), "Ub", "14210" },
                    { 81, 20.164999999999999, 44.371099999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.165 44.3711)"), "Lajkovac", "14224" },
                    { 82, 20.081900000000001, 44.252200000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0819 44.2522)"), "Mionica", "14242" },
                    { 83, 20.2394, 44.226399999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.2394 44.2264)"), "Ljig", "14240" },
                    { 84, 20.9589, 44.365299999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.9589 44.3653)"), "Smederevska Palanka", "11420" },
                    { 85, 21.075600000000001, 44.333599999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.0756 44.3336)"), "Velika Plana", "11320" },
                    { 86, 21.516400000000001, 44.7639, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.5164 44.7639)"), "Veliko Gradište", "12220" },
                    { 87, 21.631699999999999, 44.653100000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.6317 44.6531)"), "Golubac", "12223" },
                    { 88, 21.288900000000002, 44.562199999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.2889 44.5622)"), "Malo Crniće", "12311" },
                    { 89, 21.214700000000001, 44.356099999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.2147 44.3561)"), "Žabari", "12374" },
                    { 90, 21.4192, 44.378599999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.4192 44.3786)"), "Petrovac na Mlavi", "12300" },
                    { 91, 21.671399999999998, 44.479399999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.6714 44.4794)"), "Kučevo", "12240" },
                    { 92, 21.789400000000001, 44.197800000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7894 44.1978)"), "Žagubica", "12320" },
                    { 93, 20.559999999999999, 44.306899999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.56 44.3069)"), "Aranđelovac", "34300" },
                    { 94, 20.682500000000001, 44.254399999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6825 44.2544)"), "Topola", "34310" },
                    { 95, 20.983899999999998, 44.227499999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.9839 44.2275)"), "Rača", "34210" },
                    { 96, 21.081099999999999, 44.153100000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.0811 44.1531)"), "Batočina", "34227" },
                    { 97, 20.718599999999999, 43.923299999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.7186 43.9233)"), "Knić", "34240" },
                    { 98, 21.100300000000001, 44.184199999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.1003 44.1842)"), "Lapovo", "34220" },
                    { 99, 21.370000000000001, 43.927500000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.37 43.9275)"), "Ćuprija", "35230" },
                    { 100, 21.407800000000002, 43.860799999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.4078 43.8608)"), "Paraćin", "35250" },
                    { 101, 21.194700000000001, 44.234200000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.1947 44.2342)"), "Svilajnac", "35210" },
                    { 102, 21.442499999999999, 44.093600000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.4425 44.0936)"), "Despotovac", "35213" },
                    { 103, 21.093900000000001, 43.8628, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.0939 43.8628)"), "Rekovac", "35260" },
                    { 104, 22.6142, 44.606099999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.6142 44.6061)"), "Kladovo", "19320" },
                    { 105, 21.935600000000001, 44.421399999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.9356 44.4214)"), "Majdanpek", "19250" },
                    { 106, 22.530799999999999, 44.226399999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.5308 44.2264)"), "Negotin", "19300" },
                    { 107, 21.9514, 43.830300000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.9514 43.8303)"), "Boljevac", "19370" },
                    { 108, 22.2575, 43.566400000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.2575 43.5664)"), "Knjaževac", "19350" },
                    { 109, 21.871099999999998, 43.643099999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.8711 43.6431)"), "Sokobanja", "18230" },
                    { 110, 19.567499999999999, 43.970799999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.5675 43.9708)"), "Bajina Bašta", "31250" },
                    { 111, 19.908300000000001, 44.0, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.9083 44)"), "Kosjerić", "31260" },
                    { 112, 20.0364, 43.8461, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0364 43.8461)"), "Požega", "31210" },
                    { 113, 19.715599999999998, 43.749699999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.7156 43.7497)"), "Čajetina", "31310" },
                    { 114, 20.095600000000001, 43.753100000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0956 43.7531)"), "Arilje", "31230" },
                    { 115, 19.5258, 43.583599999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.5258 43.5836)"), "Priboj", "31330" },
                    { 116, 19.813099999999999, 43.460299999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.8131 43.4603)"), "Nova Varoš", "31320" },
                    { 117, 19.648900000000001, 43.389699999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (19.6489 43.3897)"), "Prijepolje", "31300" },
                    { 118, 20.000299999999999, 43.273099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.0003 43.2731)"), "Sjenica", "36310" },
                    { 119, 20.461400000000001, 44.0261, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.4614 44.0261)"), "Gornji Milanovac", "32300" },
                    { 120, 20.138100000000001, 43.860799999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.1381 43.8608)"), "Lučani", "32240" },
                    { 121, 20.229199999999999, 43.579700000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.2292 43.5797)"), "Ivanjica", "32250" },
                    { 122, 20.8933, 43.623600000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.8933 43.6236)"), "Vrnjačka Banja", "36210" },
                    { 123, 20.613600000000002, 43.286099999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.6136 43.2861)"), "Raška", "36350" },
                    { 124, 20.337199999999999, 42.9908, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (20.3372 42.9908)"), "Tutin", "36320" },
                    { 125, 21.366399999999999, 43.723100000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.3664 43.7231)"), "Varvarin", "37260" },
                    { 126, 21.002500000000001, 43.616700000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.0025 43.6167)"), "Trstenik", "37240" },
                    { 127, 21.4419, 43.719200000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.4419 43.7192)"), "Ćićevac", "37210" },
                    { 128, 21.0472, 43.458599999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.0472 43.4586)"), "Aleksandrovac", "37230" },
                    { 129, 21.033100000000001, 43.383600000000001, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.0331 43.3836)"), "Brus", "37220" },
                    { 130, 21.706900000000001, 43.541699999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7069 43.5417)"), "Aleksinac", "18220" },
                    { 131, 22.1236, 43.4161, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.1236 43.4161)"), "Svrljig", "18360" },
                    { 132, 21.722200000000001, 43.283299999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7222 43.2833)"), "Merošina", "18252" },
                    { 133, 21.5486, 43.673900000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.5486 43.6739)"), "Ražanj", "37215" },
                    { 134, 21.832799999999999, 43.1967, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.8328 43.1967)"), "Doljevac", "18410" },
                    { 135, 22.033300000000001, 43.223599999999998, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.0333 43.2236)"), "Gadžin Han", "18240" },
                    { 136, 21.285299999999999, 43.2958, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.2853 43.2958)"), "Blace", "18420" },
                    { 137, 21.271100000000001, 43.140599999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.2711 43.1406)"), "Kuršumlija", "18430" },
                    { 138, 21.7136, 43.184699999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7136 43.1847)"), "Žitorađa", "18412" },
                    { 139, 22.308299999999999, 43.217799999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.3083 43.2178)"), "Bela Palanka", "18310" },
                    { 140, 22.4114, 43.067500000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.4114 43.0675)"), "Babušnica", "18330" },
                    { 141, 22.775600000000001, 43.016100000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.7756 43.0161)"), "Dimitrovgrad", "18320" },
                    { 142, 21.718599999999999, 43.014200000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7186 43.0142)"), "Bojnik", "16205" },
                    { 143, 21.7361, 42.916699999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7361 42.9167)"), "Lebane", "16230" },
                    { 144, 21.5853, 42.843299999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.5853 42.8433)"), "Medveđa", "16240" },
                    { 145, 22.128900000000002, 42.965800000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.1289 42.9658)"), "Vlasotince", "16210" },
                    { 146, 22.2989, 42.810000000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.2989 42.81)"), "Crna Trava", "16215" },
                    { 147, 22.066400000000002, 42.7072, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.0664 42.7072)"), "Vladičin Han", "17510" },
                    { 148, 22.170000000000002, 42.690800000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.17 42.6908)"), "Surdulica", "17530" },
                    { 149, 22.472799999999999, 42.5, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.4728 42.5)"), "Bosilegrad", "17540" },
                    { 150, 22.085599999999999, 42.351100000000002, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (22.0856 42.3511)"), "Trgovište", "17525" },
                    { 151, 21.7667, 42.459200000000003, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.7667 42.4592)"), "Bujanovac", "17520" },
                    { 152, 21.649699999999999, 42.309199999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (21.6497 42.3092)"), "Preševo", "17523" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Gradovi",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Serbian_Latin_100_CI_AS");
        }
    }
}
