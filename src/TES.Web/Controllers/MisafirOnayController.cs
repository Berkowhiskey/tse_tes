using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TES.Domain.Sabitler;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;

namespace TES.Web.Controllers;

/// <summary>
/// Amirin sponsor onay ekranı: kendisinin sponsor gösterildiği misafir taleplerini
/// sistem içinden onaylar/reddeder (CLAUDE.md Bölüm 5 — "Amir sponsor olabilir").
/// Sahiplik (talep.SponsorEposta == amir.Email) servis katmanında sunucuda doğrulanır.
/// </summary>
[Authorize(Roles = Roller.Amir)]
public class MisafirOnayController(
    IMisafirTalepServisi talepServisi,
    UserManager<Kullanici> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var amir = await userManager.GetUserAsync(User);
        if (amir is null)
            return RedirectToAction("Giris", "Hesap");

        if (string.IsNullOrEmpty(amir.Email))
        {
            TempData["Hata"] = "Hesabınızda kayıtlı bir e-posta yok; sponsor talepleri e-postanızla eşleştirilir.";
            return View(Array.Empty<Domain.Entities.MisafirErisimTalebi>());
        }

        var talepler = await talepServisi.SponsorunTalepleriAsync(amir.Email);
        return View(talepler);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int id)
    {
        return await KararVerAsync(id, onay: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(int id)
    {
        return await KararVerAsync(id, onay: false);
    }

    private async Task<IActionResult> KararVerAsync(int id, bool onay)
    {
        var amir = await userManager.GetUserAsync(User);
        if (amir?.Email is null)
            return RedirectToAction("Giris", "Hesap");

        var (basarili, hata) = await talepServisi.SponsorKararVerAsync(id, amir.Email, onay);

        if (basarili)
            TempData["Basari"] = onay
                ? "Talep onaylandı; misafir erişimi açıldı ve voucher talep sahibine e-postalandı."
                : "Talep reddedildi; talep sahibi bilgilendirildi.";
        else
            TempData["Hata"] = hata;

        return RedirectToAction(nameof(Index));
    }
}
