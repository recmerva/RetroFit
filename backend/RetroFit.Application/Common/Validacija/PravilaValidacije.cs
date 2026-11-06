using System.Text.RegularExpressions;
using FluentValidation;

namespace RetroFit.Application.Common.Validacija;

/// <summary>Višekratna FluentValidation pravila sa porukama na srpskom.</summary>
public static partial class PravilaValidacije
{
    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> pravilo) =>
        pravilo.NotEmpty().WithMessage("Email je obavezan.")
            .MaximumLength(256).WithMessage("Email može imati najviše 256 karaktera.")
            .EmailAddress().WithMessage("Email adresa nije ispravna.");

    /// <summary>Najmanje 8 znakova, bar jedno slovo i bar jedna cifra (preporuka NIST: dužina je važnija od složenosti).</summary>
    public static IRuleBuilderOptions<T, string> JakaLozinka<T>(this IRuleBuilder<T, string> pravilo) =>
        pravilo.NotEmpty().WithMessage("Lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 karaktera.")
            .MaximumLength(128).WithMessage("Lozinka može imati najviše 128 karaktera.")
            .Must(l => l.Any(char.IsLetter) && l.Any(char.IsDigit))
            .WithMessage("Lozinka mora sadržati bar jedno slovo i bar jednu cifru.");

    public static IRuleBuilderOptions<T, string> Ime<T>(this IRuleBuilder<T, string> pravilo, string naziv) =>
        pravilo.NotEmpty().WithMessage($"{naziv} je obavezno.")
            .MinimumLength(2).WithMessage($"{naziv} mora imati najmanje 2 karaktera.")
            .MaximumLength(50).WithMessage($"{naziv} može imati najviše 50 karaktera.")
            .Must(i => i.All(c => char.IsLetter(c) || c is ' ' or '-' or '\''))
            .WithMessage($"{naziv} sme sadržati samo slova, razmak, crticu i apostrof.");

    public static IRuleBuilderOptions<T, string> Telefon<T>(this IRuleBuilder<T, string> pravilo) =>
        pravilo.NotEmpty().WithMessage("Broj telefona je obavezan.")
            .Must(t => BrojTelefona.Normalizuj(t) is not null)
            .WithMessage("Broj mobilnog telefona nije ispravan (primer: 064 123 4567 ili +381 64 123 4567).");

    public static IRuleBuilderOptions<T, decimal?> Cena<T>(this IRuleBuilder<T, decimal?> pravilo) =>
        pravilo.GreaterThan(0).WithMessage("Cena mora biti veća od 0.")
            .LessThanOrEqualTo(Domain.Pravila.PoslovnaPravila.MaksCena).WithMessage("Cena je prevelika.")
            .Must(c => c is null || decimal.Round(c.Value, 2) == c.Value).WithMessage("Cena može imati najviše dve decimale.");
}

/// <summary>Normalizacija srpskog broja mobilnog telefona u E.164 format (+3816XXXXXXX).</summary>
public static partial class BrojTelefona
{
    [GeneratedRegex(@"^\+3816\d{7,8}$")]
    private static partial Regex SrpskiMobilni();

    public static string? Normalizuj(string? unos)
    {
        if (string.IsNullOrWhiteSpace(unos))
        {
            return null;
        }

        var cifre = new string(unos.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (cifre.StartsWith("00381"))
        {
            cifre = "+" + cifre[2..];
        }
        else if (cifre.StartsWith("381"))
        {
            cifre = "+" + cifre;
        }
        else if (cifre.StartsWith('0'))
        {
            cifre = "+381" + cifre[1..];
        }

        return SrpskiMobilni().IsMatch(cifre) ? cifre : null;
    }
}
