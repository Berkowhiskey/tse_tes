using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Chatbox: amir ↔ stajyer mesajlaşması; admin izler. Mesaj gönderimi SignalR ChatHub
/// üzerinden yapılır; bu controller konuşma listesini ve geçmiş mesajları sunar.
/// Tüm yetki/sahiplik kararları SohbetServisi'nde sunucuda verilir.
/// </summary>
[Authorize(Policy = Politikalar.StajyerPolicy)]
public class SohbetController(ISohbetServisi sohbetServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var yetki = User.YetkiBaglamiOlustur();

        return View(new SohbetIndexViewModel
        {
            AdminMi = yetki.IsAdmin,
            Konusmalar = await sohbetServisi.KonusmalarimAsync(yetki),
            AdminKonusmalari = yetki.IsAdmin ? await sohbetServisi.AdminKonusmalariAsync() : []
        });
    }

    /// <summary>Bir kişiyle olan konuşma penceresi (mesaj geçmişi + gönderme kutusu).</summary>
    [HttpGet]
    public async Task<IActionResult> Konusma(string karsiId, string? benId)
    {
        var yetki = User.YetkiBaglamiOlustur();

        // Admin izlerken hangi tarafın gözünden bakacağını "benId" ile seçer; diğer roller kendisidir.
        var etkinBen = yetki.IsAdmin && !string.IsNullOrEmpty(benId) ? benId : AktifKullaniciId();

        var (yetkili, mesajlar) = await sohbetServisi.MesajlariGetirAsync(
            etkinBen, karsiId, yetki, okunduIsaretle: !yetki.IsAdmin);

        if (!yetkili)
            return Forbid();

        return View(new SohbetPencereViewModel
        {
            BenId = etkinBen,
            KarsiId = karsiId,
            KarsiAdSoyad = await sohbetServisi.AdSoyadAsync(karsiId),
            Mesajlar = mesajlar,
            SaltOkunur = yetki.IsAdmin
        });
    }

    private string AktifKullaniciId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}
