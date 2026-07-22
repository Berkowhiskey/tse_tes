using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TES.Domain.Sabitler;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Yoklama görüntüleme — kapsam sunucuda role göre daraltılır (CLAUDE.md Bölüm 5):
/// Admin tümünü, Amir kendi stajyerlerini, Stajyer yalnızca kendisini görür.
/// </summary>
[Authorize(Policy = Politikalar.StajyerPolicy)]
public class YoklamaController(
    IStajyerSorguServisi stajyerSorgu,
    IYoklamaServisi yoklamaServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var kullaniciId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        IReadOnlyList<StajyerSatiri> kapsam;
        string aciklama;

        if (User.IsInRole(Roller.Admin))
        {
            kapsam = await stajyerSorgu.TumunuListeleAsync();
            aciklama = "Tüm stajyerler";
        }
        else if (User.IsInRole(Roller.Amir))
        {
            kapsam = await stajyerSorgu.AmirinStajyerleriAsync(kullaniciId);
            aciklama = "Stajyerleriniz";
        }
        else
        {
            var kendi = await stajyerSorgu.KendiProfilimAsync(kullaniciId);
            kapsam = kendi is null ? [] : [kendi];
            aciklama = "Yoklama kayıtlarınız";
        }

        var adlar = kapsam.ToDictionary(s => s.Profil.Id, s => s.AdSoyad);
        var kayitlar = await yoklamaServisi.KayitlariGetirAsync([.. adlar.Keys]);

        return View(new YoklamaListeViewModel
        {
            KapsamAciklama = aciklama,
            Satirlar = kayitlar
                .Select(y => new YoklamaSatirViewModel
                {
                    StajyerAdSoyad = adlar.GetValueOrDefault(y.StajyerProfilId, "?"),
                    GirisZamani = y.GirisZamani,
                    CikisZamani = y.CikisZamani
                })
                .ToList()
        });
    }
}
