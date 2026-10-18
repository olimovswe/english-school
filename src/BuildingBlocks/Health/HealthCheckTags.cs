namespace EnglishSchool.BuildingBlocks.Health;

/// <summary>Health check teglari: qaysi endpoint qaysi tekshiruvlarni bajaradi.</summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Tayyorlik (<c>/health/ready</c>): servis soʻrov qabul qila oladimi. Bazalar va migratsiyalar, kesh,
    /// toʻxtash holati. Tirikligi (<c>/health/live</c>) esa hech qanday tashqi tizimni tekshirmaydi.
    /// </summary>
    public const string Ready = "ready";
}
