using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

public enum KartOkutmaSonucu
{
    KartBulunamadi,
    GirisYapildi,
    CikisYapildi
}

public interface IYoklamaServisi
{
    /// <summary>
    /// Kart okutma kuralı (CLAUDE.md Bölüm 6): stajyerin AYNI GÜN içinde açık oturumu
    /// (CikisZamani == null) varsa çıkış yazılır; yoksa yeni giriş açılır.
    /// Önceki günlerden kalan açık oturumlara dokunulmaz ("açık oturum" olarak raporlanır).
    /// </summary>
    Task<(KartOkutmaSonucu Sonuc, StajyerProfil? Stajyer)> KartOkutAsync(string kartNo, DateTime? zaman = null);

    /// <summary>Verilen stajyerlerin kayıtlarını (yeniden eskiye) getirir.</summary>
    Task<IReadOnlyList<YoklamaKaydi>> KayitlariGetirAsync(IReadOnlyCollection<int> stajyerProfilIdleri, int enFazla = 100);
}
