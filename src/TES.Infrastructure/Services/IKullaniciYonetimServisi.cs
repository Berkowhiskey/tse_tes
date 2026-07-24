namespace TES.Infrastructure.Services;

/// <summary>Yeni amir bilgisi. TcKimlikNo yalnızca geçici parola üretiminde kullanılır, SAKLANMAZ.</summary>
public record YeniAmirBilgisi(
    string Ad,
    string Soyad,
    string TcKimlikNo,
    int DepartmanId,
    DateOnly? IseBaslamaTarihi,
    string? Hakkimda);

/// <summary>Yeni stajyer bilgisi. TcKimlikNo yalnızca geçici parola üretiminde kullanılır, SAKLANMAZ.</summary>
public record YeniStajyerBilgisi(
    string Ad,
    string Soyad,
    string TcKimlikNo,
    string KartNo,
    DateOnly StajBaslangic,
    DateOnly StajBitis,
    string? Okul,
    string? Bolum,
    int? AmirProfilId);

public record KullaniciOlusturmaSonucu(bool Basarili, string? KullaniciAdi, string? Eposta, IReadOnlyList<string> Hatalar)
{
    public static KullaniciOlusturmaSonucu Basari(string kullaniciAdi, string? eposta = null) =>
        new(true, kullaniciAdi, eposta, []);

    public static KullaniciOlusturmaSonucu Hata(params string[] hatalar) => new(false, null, null, hatalar);
}

/// <summary>Seçim listeleri ve yönetim ekranları için amir satırı.</summary>
public record AmirSatiri(int ProfilId, string KullaniciAdi, string AdSoyad, string DepartmanAd, int StajyerSayisi);

public interface IKullaniciYonetimServisi
{
    /// <summary>Tüm amirleri (profil + departman + stajyer sayısı) listeler.</summary>
    Task<IReadOnlyList<AmirSatiri>> AmirleriListeleAsync();

    /// <summary>Amir kullanıcısı + profili oluşturur. Kullanıcı adı: ad_soyad; geçici parola: T.C.</summary>
    Task<KullaniciOlusturmaSonucu> AmirOlusturAsync(YeniAmirBilgisi bilgi, string aktor);

    /// <summary>
    /// Stajyer kullanıcısı + profili oluşturur. Kullanıcı adı: ad_soyad_kartno; geçici parola: T.C.
    /// Amir seçildiyse departman amirden gelir.
    /// </summary>
    Task<KullaniciOlusturmaSonucu> StajyerOlusturAsync(YeniStajyerBilgisi bilgi, string aktor);

    /// <summary>
    /// Stajyeri amire eşler (tek amir kuralı) ve departmanını amirin departmanıyla günceller.
    /// amirProfilId null ise eşleştirme kaldırılır.
    /// </summary>
    Task<(bool Basarili, string? Hata)> StajyerAmireAtaAsync(int stajyerProfilId, int? amirProfilId, string aktor);
}
