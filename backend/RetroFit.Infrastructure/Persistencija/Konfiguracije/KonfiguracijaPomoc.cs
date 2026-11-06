using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RetroFit.Infrastructure.Persistencija.Konfiguracije;

/// <summary>Pomoćne metode koje se ponavljaju u konfiguracijama entiteta.</summary>
internal static class KonfiguracijaPomoc
{
    /// <summary>Enum se čuva kao tekst (nvarchar), čitljivo u SSMS-u.</summary>
    public static PropertyBuilder<TEnum> KaoTekst<TEnum>(this PropertyBuilder<TEnum> builder, int duzina = 30)
        where TEnum : struct, Enum =>
        builder.HasConversion<string>().HasMaxLength(duzina).IsUnicode(false);

    public static PropertyBuilder<TEnum?> KaoTekst<TEnum>(this PropertyBuilder<TEnum?> builder, int duzina = 30)
        where TEnum : struct, Enum =>
        builder.HasConversion<string?>().HasMaxLength(duzina).IsUnicode(false);

    /// <summary>SQL izraz za CHECK ograničenje: [Kolona] IN ('A','B',...). Za nullable kolonu dozvoljava i NULL.</summary>
    public static string EnumIn<TEnum>(string kolona, bool nullable = false) where TEnum : struct, Enum
    {
        var vrednosti = string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"));
        var izraz = $"[{kolona}] IN ({vrednosti})";
        return nullable ? $"([{kolona}] IS NULL OR {izraz})" : izraz;
    }

    /// <summary>
    /// Označava vezu kao namerno kaskadnu. DbContext sve kaskade koje nisu eksplicitno
    /// zadate (konvencija za obavezne veze) pretvara u Restrict.
    /// </summary>
    public static ReferenceCollectionBuilder<TPrincipal, TDependent> Kaskadno<TPrincipal, TDependent>(
        this ReferenceCollectionBuilder<TPrincipal, TDependent> builder)
        where TPrincipal : class where TDependent : class =>
        builder.OnDelete(DeleteBehavior.Cascade);

    public static void AuditPolja<T>(this EntityTypeBuilder<T> builder) where T : Domain.Common.AuditabilniEntitet
    {
        builder.Property(e => e.KreiranoUtc).IsRequired();
        builder.Property(e => e.IzmenjenoUtc);
        builder.Property(e => e.KreiraoKorisnikId);
        builder.Property(e => e.IzmenioKorisnikId);
    }

    public static PropertyBuilder<string> Tekst<T>(this EntityTypeBuilder<T> builder, Expression<Func<T, string>> svojstvo, int duzina)
        where T : class =>
        builder.Property(svojstvo).HasMaxLength(duzina).IsRequired();

    public static PropertyBuilder<string?> OpcioniTekst<T>(this EntityTypeBuilder<T> builder, Expression<Func<T, string?>> svojstvo, int duzina)
        where T : class =>
        builder.Property(svojstvo).HasMaxLength(duzina);
}
