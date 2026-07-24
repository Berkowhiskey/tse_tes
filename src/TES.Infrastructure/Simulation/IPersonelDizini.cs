namespace TES.Infrastructure.Simulation;

/// <summary>
/// Sponsor e-postasının kayıtlı bir @tse.org.tr personeli olduğunu doğrular
/// (CLAUDE.md Bölüm 8: olmayan adres reddedilir).
/// </summary>
public interface IPersonelDizini
{
    Task<bool> KayitliMiAsync(string eposta);
}
