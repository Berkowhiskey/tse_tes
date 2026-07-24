using TES.Domain.Sabitler;

namespace TES.Infrastructure.Services;

/// <summary>
/// İstek sahibinin kimlik/rol bağlamı. İş takibi servisleri sahiplik kararlarını
/// (kendi stajyeri / kendi işi) bu bağlama göre SUNUCUDA verir (CLAUDE.md Bölüm 5).
/// </summary>
public record YetkiBaglami(string KullaniciId, bool IsAdmin, bool IsAmir, bool IsStajyer)
{
    public static YetkiBaglami Olustur(string kullaniciId, IEnumerable<string> roller)
    {
        var rolKumesi = roller.ToHashSet();
        return new YetkiBaglami(
            kullaniciId,
            rolKumesi.Contains(Roller.Admin),
            rolKumesi.Contains(Roller.Amir),
            rolKumesi.Contains(Roller.Stajyer));
    }
}
