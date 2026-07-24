using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

public record IsSonucu(bool Basarili, string? Hata)
{
    public static readonly IsSonucu Basari = new(true, null);
    public static IsSonucu Hata_(string hata) => new(false, hata);
}

public interface IProjeServisi
{
    Task<Proje?> StajyerinProjesiAsync(int stajyerProfilId);

    Task<Proje?> KendiProjemAsync(string kullaniciId);

    /// <summary>
    /// Stajyere proje oluşturur veya mevcut projeyi günceller (tek proje kuralı).
    /// Yalnızca Admin veya stajyerin AMİRİ yapabilir (sunucuda doğrulanır).
    /// </summary>
    Task<IsSonucu> KaydetAsync(int stajyerProfilId, string ad, string? aciklama, YetkiBaglami yetki);

    /// <summary>
    /// Durum/ilerleme günceller. Admin, stajyerin amiri veya projenin sahibi stajyer yapabilir.
    /// </summary>
    Task<IsSonucu> IlerlemeGuncelleAsync(int projeId, int ilerleme, IsDurumu durum, YetkiBaglami yetki);

    /// <summary>Projeyi siler — yalnızca Admin veya stajyerin amiri.</summary>
    Task<IsSonucu> SilAsync(int projeId, YetkiBaglami yetki);
}
