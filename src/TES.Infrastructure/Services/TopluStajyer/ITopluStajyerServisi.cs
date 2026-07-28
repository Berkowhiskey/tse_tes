namespace TES.Infrastructure.Services;

/// <summary>
/// Excel (.xlsx) ile toplu stajyer içe aktarma. Şablon üretir ve yüklenen dosyayı işler.
/// Yetki (yalnız Admin) çağıran controller'da doğrulanır; T.C. yalnızca geçici parola olur, SAKLANMAZ.
/// </summary>
public interface ITopluStajyerServisi
{
    /// <summary>Boş .xlsx şablonunu byte dizisi olarak üretir.</summary>
    byte[] SablonUret();

    /// <summary>
    /// Yüklenen .xlsx akışını bellekte işler: geçerli stajyerleri (amirsiz) ekler,
    /// hatalı satırları satır no + nedenle raporlar. Dosya diske YAZILMAZ, loglanmaz.
    /// </summary>
    Task<TopluIceAktarmaSonucu> IceAktarAsync(Stream xlsx, string aktor);
}
