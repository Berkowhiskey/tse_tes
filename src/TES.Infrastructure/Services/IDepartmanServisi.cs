using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

public interface IDepartmanServisi
{
    /// <summary>Hiyerarşik sırayla (kökler + girintili alt departmanlar) düz liste döner.</summary>
    Task<IReadOnlyList<(Departman Departman, int Seviye)>> HiyerarsikListeAsync();

    Task<Departman?> GetirAsync(int id);

    Task<Departman> OlusturAsync(string ad, int? ustDepartmanId, string aktor);

    Task GuncelleAsync(int id, string ad, int? ustDepartmanId, string aktor);

    /// <summary>Alt departmanı, amiri veya stajyeri olan departman silinemez.</summary>
    Task<(bool Basarili, string? Hata)> SilAsync(int id, string aktor);
}
