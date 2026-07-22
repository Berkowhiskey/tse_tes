using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

/// <summary>Listeleme/görüntüleme için birleşik stajyer satırı.</summary>
public record StajyerSatiri(
    StajyerProfil Profil,
    string KullaniciAdi,
    string AdSoyad,
    string? AmirAdSoyad,
    string? DepartmanAd);

/// <summary>
/// Stajyer verisine erişim sorguları. "Kendi stajyeri / kendisi" sahiplik kuralı
/// burada, SUNUCU tarafında uygulanır (CLAUDE.md Bölüm 5).
/// </summary>
public interface IStajyerSorguServisi
{
    /// <summary>Admin: tüm stajyerler.</summary>
    Task<IReadOnlyList<StajyerSatiri>> TumunuListeleAsync();

    /// <summary>Amir: yalnızca kendi stajyerleri.</summary>
    Task<IReadOnlyList<StajyerSatiri>> AmirinStajyerleriAsync(string amirKullaniciId);

    /// <summary>
    /// Sahiplik doğrulamalı detay: Admin her stajyeri, Amir yalnız kendi stajyerini,
    /// Stajyer yalnız kendisini görebilir. Yetkisizse null döner.
    /// </summary>
    Task<StajyerSatiri?> DetayGetirAsync(int stajyerProfilId, string isteyenKullaniciId, bool isteyenAdmin, bool isteyenAmir);

    /// <summary>Stajyerin kendi profil satırı (yoksa null).</summary>
    Task<StajyerSatiri?> KendiProfilimAsync(string kullaniciId);
}
