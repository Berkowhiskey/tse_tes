namespace TES.Web;

/// <summary>Policy adları — Program.cs'te tanımlanır, [Authorize(Policy = ...)] ile kullanılır.</summary>
public static class Politikalar
{
    /// <summary>Yalnızca Admin.</summary>
    public const string AdminPolicy = "AdminPolicy";

    /// <summary>Admin + Amir.</summary>
    public const string AmirPolicy = "AmirPolicy";

    /// <summary>Giriş yapmış tüm roller (Admin + Amir + Stajyer).</summary>
    public const string StajyerPolicy = "StajyerPolicy";
}
