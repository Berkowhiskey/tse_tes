namespace TES.Domain.Sabitler;

/// <summary>
/// Sistemdeki sabit rol adları. Identity rolleri ve policy tanımları bu sabitleri kullanır.
/// </summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Amir = "Amir";
    public const string Stajyer = "Stajyer";

    public static readonly string[] Tumu = [Admin, Amir, Stajyer];
}
