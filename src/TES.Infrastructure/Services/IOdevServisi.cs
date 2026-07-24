using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

public interface IOdevServisi
{
    Task<IReadOnlyList<Odev>> StajyerinOdevleriAsync(int stajyerProfilId);

    /// <summary>Stajyerin kendi ödevleri (oturumdaki kimlikten).</summary>
    Task<IReadOnlyList<Odev>> KendiOdevlerimAsync(string kullaniciId);

    /// <summary>Amirin atadığı tüm ödevler (kendi stajyerlerine).</summary>
    Task<IReadOnlyList<Odev>> AmirinAttiklariAsync(string amirKullaniciId);

    Task<Odev?> GetirAsync(int odevId);

    /// <summary>
    /// Stajyere ödev atar. Yalnızca Admin veya stajyerin AMİRİ atayabilir; ödevin AmirProfilId'si
    /// stajyerin amiridir (stajyerin amiri yoksa atama yapılamaz).
    /// </summary>
    Task<IsSonucu> AtaAsync(int stajyerProfilId, string baslik, string? aciklama, DateOnly? teslimTarihi, YetkiBaglami yetki);

    /// <summary>
    /// Durum/ilerleme günceller. Admin, atayan amir veya ödevin sahibi stajyer yapabilir.
    /// </summary>
    Task<IsSonucu> DurumGuncelleAsync(int odevId, int ilerleme, IsDurumu durum, YetkiBaglami yetki);

    /// <summary>Ödevi siler — yalnızca Admin veya stajyerin amiri.</summary>
    Task<IsSonucu> SilAsync(int odevId, YetkiBaglami yetki);
}
