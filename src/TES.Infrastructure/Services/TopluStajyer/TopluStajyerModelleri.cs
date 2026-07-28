namespace TES.Infrastructure.Services;

/// <summary>
/// Excel/CSV'den okunan ham bir stajyer satırı. Tarih alanları hem ham metin (hata mesajı için)
/// hem de ayrıştırılmış <see cref="DateOnly"/> olarak taşınır. T.C. yalnızca geçici parola olur; SAKLANMAZ.
/// </summary>
public sealed record TopluStajyerSatir(
    int SatirNo,
    string? Ad,
    string? Soyad,
    string? TcKimlikNo,
    string? KartNo,
    string? StajBaslangicMetin,
    DateOnly? StajBaslangic,
    string? StajBitisMetin,
    DateOnly? StajBitis,
    string? Okul,
    string? Bolum);

/// <summary>Doğrulamadan geçmiş, oluşturulmaya hazır stajyer (temizlenmiş değerler).</summary>
public sealed record GecerliStajyer(
    int SatirNo,
    string Ad,
    string Soyad,
    string TcKimlikNo,
    string KartNo,
    DateOnly StajBaslangic,
    DateOnly StajBitis,
    string? Okul,
    string? Bolum);

/// <summary>Bir satırın neden atlandığını raporlar (SatirNo 0 = dosya geneli hata).</summary>
public sealed record SatirHatasi(int SatirNo, string Neden);

/// <summary>Toplu içe aktarma sonucu: eklenenler + atlanan satırların gerekçeleri.</summary>
public sealed record TopluIceAktarmaSonucu(
    int EklenenSayisi,
    IReadOnlyList<SatirHatasi> Hatalar,
    IReadOnlyList<string> EklenenKullaniciAdlari)
{
    public int AtlananSayisi => Hatalar.Count(h => h.SatirNo > 0);

    public static TopluIceAktarmaSonucu DosyaHatasi(string neden) =>
        new(0, [new SatirHatasi(0, neden)], []);
}
