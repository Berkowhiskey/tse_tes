using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TES.Infrastructure.Services;

namespace TES.Web.Hubs;

/// <summary>
/// Amir ↔ stajyer gerçek zamanlı sohbeti. Yetki hub metodunda da SUNUCUDA doğrulanır
/// (CLAUDE.md Bölüm 10): gönderen kimliği Context'ten alınır, alıcı konuşma izni kontrol edilir.
/// Kullanıcı bazlı iletim için SignalR UserIdentifier = NameIdentifier claim'i kullanılır.
/// </summary>
[Authorize]
public class ChatHub(ISohbetServisi sohbetServisi) : Hub
{
    /// <summary>
    /// Mesaj gönderir. İçeriğe/alıcıya güvenilmez: gönderen kimliği Context'ten alınır,
    /// izin sunucuda doğrulanır, mesaj kalıcılaştırılır ve iki tarafa da iletilir.
    /// </summary>
    public async Task GonderMesaj(string aliciId, string icerik)
    {
        var gondericiId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
            zaman = m.Zaman
        };

        // Alıcıya ve gönderenin diğer sekmelerine ilet (kullanıcı bazlı).
        await Clients.User(aliciId).SendAsync("MesajAlindi", yuk);
        await Clients.User(gondericiId).SendAsync("MesajAlindi", yuk);
    }
}
