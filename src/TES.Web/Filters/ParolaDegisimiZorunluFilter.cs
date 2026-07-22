using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TES.Infrastructure.Identity;

namespace TES.Web.Filters;

/// <summary>
/// ForceChangePassword bayrağı açık olan kullanıcıyı, parola değiştirene kadar
/// /Hesap/ParolaDegistir dışındaki tüm sayfalardan uzak tutar (CLAUDE.md Bölüm 7).
/// Global filtre olarak kayıtlıdır (Program.cs).
/// </summary>
public class ParolaDegisimiZorunluFilter(UserManager<Kullanici> userManager) : IAsyncActionFilter
{
    // Parola değişimi zorunluyken erişimine izin verilen action'lar (çıkış dahil).
    private static readonly (string Controller, string Action)[] MuafActionlar =
    [
        ("Hesap", "ParolaDegistir"),
        ("Hesap", "Cikis"),
        ("Hesap", "ErisimReddedildi")
    ];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpUser = context.HttpContext.User;

        if (httpUser.Identity?.IsAuthenticated == true)
        {
            var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
            var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

            var muaf = MuafActionlar.Any(m =>
                string.Equals(m.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.Action, action, StringComparison.OrdinalIgnoreCase));

            if (!muaf)
            {
                var kullanici = await userManager.GetUserAsync(httpUser);

                if (kullanici?.ForceChangePassword == true)
                {
                    context.Result = new RedirectToActionResult("ParolaDegistir", "Hesap", null);
                    return;
                }
            }
        }

        await next();
    }
}
