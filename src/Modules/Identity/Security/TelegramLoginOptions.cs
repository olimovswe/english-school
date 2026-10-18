namespace EnglishSchool.Modules.Identity.Security;

internal sealed class TelegramLoginOptions
{
    public const string Section = "Identity:Telegram";

    /// <summary>@BotFather bergan bot tokeni (secret). Boʻsh boʻlsa, Telegram orqali kirish oʻchirilgan.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Telegram imzolagan maʼlumot shu muddatdan eski boʻlsa, qabul qilinmaydi (replay himoyasi).</summary>
    public TimeSpan MaxAuthAge { get; set; } = TimeSpan.FromMinutes(10);
}
