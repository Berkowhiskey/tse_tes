using Microsoft.EntityFrameworkCore;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Simulation;

// GERÇEKTE: Kurumun AD/LDAP dizinine sorgu atılır; personel listesi kodda tutulmaz.
// Bu mock, sabit bir @tse.org.tr listesini VE sistemdeki amir hesaplarının e-postalarını
// tanır (amirler kurum personelidir; gerçekte de dizinde bulunurlar) — CLAUDE.md Bölüm 9.
public class MockPersonelDizini(TesDbContext db) : IPersonelDizini
{
    private static readonly HashSet<string> SabitPersonel = new(StringComparer.OrdinalIgnoreCase)
    {
        "mehmet.demir@tse.org.tr",
        "fatma.kaya@tse.org.tr",
        "ali.ozkan@tse.org.tr",
        "zeynep.arslan@tse.org.tr"
    };

    public async Task<bool> KayitliMiAsync(string eposta)
    {
        var temiz = eposta.Trim();

        if (!temiz.EndsWith("@tse.org.tr", StringComparison.OrdinalIgnoreCase))
            return false;

        if (SabitPersonel.Contains(temiz))
            return true;

        // Sistemde bu e-postaya sahip bir kullanıcı (amir) varsa dizinde sayılır.
        return await db.Users.AnyAsync(u => u.Email == temiz);
    }
}
