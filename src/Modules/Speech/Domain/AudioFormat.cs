namespace EnglishSchool.Modules.Speech.Domain;

/// <summary>Saqlanadigan audio formati: HTTP <c>Content-Type</c> va ombordagi fayl kengaytmasi.</summary>
internal sealed record AudioFormat(string Name, string ContentType, string Extension)
{
    public static readonly AudioFormat Wav = new("wav", "audio/wav", ".wav");
    public static readonly AudioFormat Mp3 = new("mp3", "audio/mpeg", ".mp3");
    public static readonly AudioFormat Ogg = new("ogg", "audio/ogg", ".ogg");
    public static readonly AudioFormat Webm = new("webm", "audio/webm", ".webm");
    public static readonly AudioFormat Flac = new("flac", "audio/flac", ".flac");
    public static readonly AudioFormat Mp4 = new("m4a", "audio/mp4", ".m4a");
}
