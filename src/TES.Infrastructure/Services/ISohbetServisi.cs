using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

/// <summary>Konuşma listesindeki bir kişi + okunmamış mesaj sayısı.</summary>
public record KonusmaKisi(string KullaniciId, string AdSoyad, int OkunmamisSayisi);

/// <summary>Admin izleme ekranı için amir–stajyer konuşma çifti.</summary>
public record KonusmaCifti(string AmirId, string AmirAdSoyad, string StajyerId, string StajyerAdSoyad);

/// <summary>Mesaj gönderme sonucu (hub ve controller ortak kullanır).</summary>
public record MesajSonucu(bool Basarili, string? Hata, SohbetMesaji? Mesaj);

public interface ISohbetServisi
{
    /// <summary>
    /// İki kullanıcının (amir–stajyer eşleşmesi) mesajlaşma iznini SUNUCUDA belirler.
    /// Yön fark etmez: biri diğerinin amiri/stajyeri ise true.
    /// </summary>
    Task<bool> KonusabilirlerMiAsync(string kullaniciA, string kullaniciB);

    /// <summary>Stajyer → amiri; Amir → stajyerleri (okunmamış sayısıyla). Admin bu listeyi kullanmaz.</summary>
    Task<IReadOnlyList<KonusmaKisi>> KonusmalarimAsync(YetkiBaglami yetki);

    /// <summary>Admin izleme: aralarında mesaj bulunan tüm amir–stajyer çiftleri.</summary>
    Task<IReadOnlyList<KonusmaCifti>> AdminKonusmalariAsync();

    /// <summary>
    /// İki kişi arasındaki mesajları (eskiden yeniye) getirir. Yetki: taraflardan biri olmak
    /// (konuşabilir olmak) VEYA Admin (izler). okunduIsaretle true ise karşıdan gelenleri okundu yapar.
    /// </summary>
    Task<(bool Yetkili, IReadOnlyList<SohbetMesaji> Mesajlar)> MesajlariGetirAsync(
        string benId, string karsiId, YetkiBaglami yetki, bool okunduIsaretle);

    /// <summary>
    /// Mesaj gönderir. Sahiplik SUNUCUDA: gönderen–alıcı konuşabilir olmalı. Admin gönderemez (izler).
    /// </summary>
    Task<MesajSonucu> MesajGonderAsync(string gondericiId, string aliciId, string icerik);

    /// <summary>Kullanıcının toplam okunmamış mesaj sayısı (navbar bildirimi).</summary>
    Task<int> OkunmamisToplamAsync(string kullaniciId);

    /// <summary>
    /// karsiId'den benId'ye gelen okunmamış mesajları okundu işaretler (gerçek zamanlı "Okundu").
    /// En az bir mesaj işaretlendiyse true döner.
    /// </summary>
    Task<bool> OkunduIsaretleAsync(string benId, string karsiId);

    Task<string> AdSoyadAsync(string kullaniciId);
}
