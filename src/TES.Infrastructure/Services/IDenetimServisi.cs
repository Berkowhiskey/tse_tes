namespace TES.Infrastructure.Services;

/// <summary>
/// Hassas işlemleri DenetimKaydi'na yazar. Detay alanına asla kişisel veri,
/// parola veya token yazılmaz.
/// </summary>
public interface IDenetimServisi
{
    Task KaydetAsync(string aktor, string islem, string? detay = null);
}
