namespace TES.Infrastructure.Simulation;

/// <summary>Sponsor/talep e-postaları için soyutlama (CLAUDE.md Bölüm 9).</summary>
public interface IEmailSender
{
    Task GonderAsync(string kime, string konu, string icerik);
}
