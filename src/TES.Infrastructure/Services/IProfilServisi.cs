namespace TES.Infrastructure.Services;

public record AmirProfilBilgi(string DepartmanAd, DateOnly? IseBaslamaTarihi, string? Hakkimda);

/// <summary>Kullanıcının KENDİ profili üzerindeki görüntüleme/düzenleme işlemleri.</summary>
public interface IProfilServisi
{
    Task<AmirProfilBilgi?> AmirProfilGetirAsync(string kullaniciId);

    /// <summary>Stajyer kendi eğitim bilgilerini günceller.</summary>
    Task<bool> StajyerBilgiGuncelleAsync(string kullaniciId, string? okul, string? bolum, string aktor);

    /// <summary>Amir kendi "Hakkında" metnini günceller.</summary>
    Task<bool> AmirHakkimdaGuncelleAsync(string kullaniciId, string? hakkimda, string aktor);
}
