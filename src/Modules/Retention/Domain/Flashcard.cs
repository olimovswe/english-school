using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Retention.Contracts.Events;

namespace EnglishSchool.Modules.Retention.Domain;

internal enum FlashcardKind
{
    Word,
    Phrase,
}

/// <summary>
/// Oʻquvchining bitta soʻz yoki ibora kartochkasi. Curriculum'dagi lugʻat elementiga
/// (<see cref="VocabularyItemId"/>) bogʻlangan; matn yaratilgan paytdagi nusxa, chunki kurs kontenti versiya ichida oʻzgarmaydi.
/// Takrorlash bosqichidagi kartochka unutilsa, <see cref="CardLapsed"/> chiqadi (baho bilan bitta tranzaksiyada outbox'ga).
/// </summary>
internal sealed class Flashcard : IntegrationEventSource
{
    private Flashcard()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>Identity'dagi foydalanuvchi. Boshqa modul jadvaliga FK yoʻq.</summary>
    public Guid UserId { get; private init; }

    /// <summary>Curriculum <c>vocabulary_items</c> qatori (soʻz yoki ibora).</summary>
    public Guid VocabularyItemId { get; private init; }

    public FlashcardKind Kind { get; private init; }

    /// <summary>Old tomoni (inglizcha).</summary>
    public string Front { get; private init; } = string.Empty;

    /// <summary>Orqa tomoni (oʻzbekcha).</summary>
    public string Back { get; private init; } = string.Empty;

    /// <summary>Soʻz qaysi kurs kunida oʻrganilgan.</summary>
    public int DayNumber { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Navbatdagi takrorlash vaqti. Yangi kartochka darhol navbatda.</summary>
    public DateTimeOffset DueAt { get; private set; }

    /// <summary>Oxirgi baholashda belgilangan interval (kun); daqiqalik qadamlarda 0.</summary>
    public int ScheduledDays { get; private set; }

    public CardState State { get; private set; } = CardState.Initial;

    /// <summary>Optimistik konkurrentlik (PostgreSQL <c>xmin</c>): bir kartochkaning ikki bahosi bir-birini yozib yubormaydi.</summary>
    public uint Version { get; private set; }

    public static Flashcard Create(
        Guid userId,
        Guid vocabularyItemId,
        FlashcardKind kind,
        string front,
        string back,
        int dayNumber,
        DateTimeOffset now) => new()
    {
        UserId = userId,
        VocabularyItemId = vocabularyItemId,
        Kind = kind,
        Front = front,
        Back = back,
        DayNumber = dayNumber,
        CreatedAt = now,
        DueAt = now,
    };

    /// <summary>Rejalashtiruvchi hisoblagan natijani qoʻllaydi va takrorlash yozuvini qaytaradi.</summary>
    public CardReview Apply(CardSchedule schedule, Rating rating, DateTimeOffset now, int? durationMs)
    {
        var phaseBefore = State.Phase;

        State = schedule.State;
        DueAt = schedule.DueAt;
        ScheduledDays = schedule.ScheduledDays;

        if (schedule.IsLapse)
        {
            Raise(new CardLapsed(UserId, Id, Kind.ToString(), VocabularyItemId));
        }

        return CardReview.Record(this, rating, now, phaseBefore, schedule.ElapsedDays, schedule.Retrievability, durationMs);
    }
}
