using System.Security.Claims;
using TES.Domain.Sabitler;
using TES.Infrastructure.Services;

namespace TES.Web;

/// <summary>Oturumdaki kullanıcıdan iş takibi servisleri için YetkiBaglami üretir.</summary>
public static class YetkiBaglamiExtensions
{
    public static YetkiBaglami YetkiBaglamiOlustur(this ClaimsPrincipal user)
    {
        var kullaniciId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        return new YetkiBaglami(
            kullaniciId,
            user.IsInRole(Roller.Admin),
            user.IsInRole(Roller.Amir),
            user.IsInRole(Roller.Stajyer));
    }
}
