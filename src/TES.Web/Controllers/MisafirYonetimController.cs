using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TES.Infrastructure.Data;
using TES.Infrastructure.Services;
using TES.Infrastructure.Simulation;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Admin misafir yönetimi: tüm talepler + enforcement durumu, manuel bağlama, iptal
/// ve simüle giden e-postalar (CLAUDE.md Bölüm 5: TSE-Misafir → Admin manuel bağlama + takip).
/// </summary>
[Authorize(Policy = Politikalar.AdminPolicy)]
public class MisafirYonetimController(
    IMisafirTalepServisi talepServisi,
    INetworkAccessProvider agSaglayici,
    TesDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var talepler = await talepServisi.TumTalepleriAsync();

        var satirlar = new List<MisafirYonetimSatiri>(talepler.Count);
        foreach (var talep in talepler)
        {
            satirlar.Add(new MisafirYonetimSatiri
            {
                Talep = talep,
                AgDurumu = await agSaglayici.DurumGetirAsync(talep.Id)
            });
        }

        return View(satirlar);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManuelOnayla(int id)
    {
        var (basarili, hata) = await talepServisi.ManuelOnaylaAsync(id, User.Identity?.Name ?? "Bilinmiyor");

        if (basarili)
            TempData["Basari"] = "Talep manuel olarak onaylandı; voucher talep sahibine e-postalandı.";
        else
            TempData["Hata"] = hata;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var (basarili, hata) = await talepServisi.IptalAsync(id, User.Identity?.Name ?? "Bilinmiyor");

        if (basarili)
            TempData["Basari"] = "Talep iptal edildi ve ağ erişimi kapatıldı.";
        else
            TempData["Hata"] = hata;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Simüle SMTP'nin teslimat kutusu — demo sırasında onay bağlantısı buradan alınır.</summary>
    [HttpGet]
    public async Task<IActionResult> GidenEpostalar()
    {
        var epostalar = await db.GidenEpostalar
            .AsNoTracking()
            .OrderByDescending(e => e.Zaman)
            .Take(50)
            .ToListAsync();

        return View(epostalar);
    }
}
