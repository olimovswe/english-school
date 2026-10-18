namespace EnglishSchool.Modules.Retention.Domain;

/// <summary>Bitta baho uchun rejalashtirish natijasi: yangi xotira holati va navbatdagi takrorlash.</summary>
/// <param name="ElapsedDays">Oldingi takrorlashdan beri oʻtgan butun kunlar.</param>
/// <param name="Retrievability">Takrorlash paytida eslab qolish ehtimoli; yangi kartochkada <c>null</c>.</param>
/// <param name="IsLapse">Takrorlash bosqichidagi kartochka unutildi (Review → Again).</param>
internal sealed record CardSchedule(
    CardState State,
    DateTimeOffset DueAt,
    int ScheduledDays,
    int ElapsedDays,
    double? Retrievability,
    bool IsLapse);
