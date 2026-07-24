using System.Security.Cryptography;
using System.Text;

namespace TES.Infrastructure.Services;

/// <summary>
/// Tek kullanımlık token ve voucher üretimi/hash'lemesi.
/// Kural (CLAUDE.md Bölüm 8): token tahmin edilemez (kriptografik) olmalı; ham değer
/// asla saklanmaz/loglanmaz — veritabanında yalnızca SHA-256 hash'i tutulur.
/// </summary>
public static class TokenYardimcisi
{
    /// <summary>URL-güvenli, 256 bit rastgele onay token'ı üretir.</summary>
    public static string TokenUret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>Okunması kolay voucher üretir (örn. "K7KX-9M2P-Q4TZ"); karışan karakterler yok.</summary>
    public static string VoucherUret()
    {
        const string alfabe = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // 0/O, 1/I/L dışarıda
        var sb = new StringBuilder(14);

        for (var blok = 0; blok < 3; blok++)
        {
            if (blok > 0)
                sb.Append('-');

            for (var i = 0; i < 4; i++)
                sb.Append(alfabe[RandomNumberGenerator.GetInt32(alfabe.Length)]);
        }

        return sb.ToString();
    }

    /// <summary>SHA-256 hash'ini küçük harfli hex olarak döner (token/voucher saklama biçimi).</summary>
    public static string Hashle(string deger)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(deger));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
