using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TES.Domain.Sabitler;
using TES.Infrastructure.Services;

namespace TES.Web.Hubs;

/// <summary>
/// Amir ↔ stajyer gerçek zamanlı sohbeti. Yetki hub metotlarında da SUNUCUDA doğrulanır
/// (CLAUDE.md Bölüm 10): gönderen kimliği Context'ten alınır, konuşma izni kontrol edilir.
/// Kullanıcı bazlı iletim için SignalR UserIdentifier = NameIdentifier claim'i kullanılır.
/// Ayrıca her konuşmanın bir SignalR grubu vardır; admin bu gruba katılarak CANLI izler.
/// </summary>
[Authorize]
public class ChatHub(ISohbetServisi sohbetServisi) : Hub
{
    /// <summary>İki kullanıcının konuşması için deterministik (sıra bağımsız) grup adı.</summary>
    public static string KonusmaGrubu(string a, string b)
    {
        var idler = new[] { a, b };
        Array.Sort(idler, StringComparer.Ordinal);
        return $"konusma:{idler[0]}:{idler[1]}";
    }

    /// <summary>
    /// Mesaj gönderir. İçeriğe/alıcıya güvenilmez: gönderen kimliği Context'ten alınır,
    /// izin sunucuda doğrulanır, mesaj kalıcılaştırılır ve iki tarafa + izleyen gruba iletilir.
    /// </summary>
    public async Task GonderMesaj(string aliciId, string icerik)
    {
        var gondericiId = KullaniciId();
        if (string.IsNullOrEmpty(gondericiId))
            throw new HubException("Kimlik doğrulanamadı.");

        var sonuc = await sohbetServisi.MesajGonderAsync(gondericiId, aliciId, icerik);
        if (!sonuc.Basarili || sonuc.Mesaj is null)
            throw new HubException(sonuc.Hata ?? "Mesaj gönderilemedi.");

        var m = sonuc.Mesaj;
        var yuk = new
        {
            gondericiId = m.GondericiId,
            aliciId = m.AliciId,
            icerik = m.Icerik,
            zaman = m.Zaman,
            gondericiAdSoyad = await sohbetServisi.AdSoyadAsync(m.GondericiId)
        };

        // Taraflar (bildirim için) + izleyen admin grubu (canlı izleme).
        await Clients.User(aliciId).SendAsync("MesajAlindi", yuk);
        await Clients.User(gondericiId).SendAsync("MesajAlindi", yuk);
        await Clients.Group(KonusmaGrubu(gondericiId, aliciId)).SendAsync("MesajAlindi", yuk);
    }

    /// <summary>Admin bir konuşmayı canlı izlemek için grubuna katılır (yalnız Admin).</summary>
    public async Task IzlemeyeBasla(string taraf1, string taraf2)
    {
        if (Context.User?.IsInRole(Roller.Admin) != true)
            throw new HubException("İzleme yetkiniz yok.");

        await Groups.AddToGroupAsync(Context.ConnectionId, KonusmaGrubu(taraf1, taraf2));
    }

    /// <summary>"Yazıyor..." bildirimi — yalnız konuşma izni olan karşı tarafa iletilir.</summary>
    public async Task Yaziyor(string aliciId)
    {
        var benId = KullaniciId();
        if (string.IsNullOrEmpty(benId))
            return;

        if (!await sohbetServisi.KonusabilirlerMiAsync(benId, aliciId))
            return; // sessizce yok say

        await Clients.User(aliciId).SendAsync("Yaziyor", benId);
    }

    /// <summary>
    /// Alıcı konuşmayı görüntüledi: karşıdan gelen mesajlar okundu işaretlenir ve
    /// gönderene "Okundu" bilgisi iletilir.
    /// </summary>
    public async Task OkunduBildir(string karsiId)
    {
        var benId = KullaniciId();
        if (string.IsNullOrEmpty(benId))
            return;

        var degisti = await sohbetServisi.OkunduIsaretleAsync(benId, karsiId);
        if (degisti)
            await Clients.User(karsiId).SendAsync("Okundu", benId);
    }

    private string? KullaniciId() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
