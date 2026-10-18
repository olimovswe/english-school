using System.Net;
using System.Text.Json;
using EnglishSchool.SystemTests.TestSupport;
using Npgsql;

namespace EnglishSchool.SystemTests.Api;

/// <summary>
/// Butun tizim oxirigacha: haqiqiy API host, productiondagi Worker host (outbox dispatcher), PostgreSQL, Valkey va
/// nutq provayderi stub'i. Oʻquvchi 1-kunni boshidan oxirigacha oʻtadi; modullar bir-biriga faqat outbox eventlari
/// orqali taʼsir qiladi, test esa faqat HTTP orqali tekshiradi.
/// </summary>
[Collection(SystemFixture.CollectionName)]
public sealed class StudentJourneyTests(SystemFixture system)
{
    /// <summary>1-kun: 28 soʻz va 5 ibora, hammasi har xil matn.</summary>
    private const int DayOneVocabulary = 33;

    /// <summary>Bu oʻquvchining barcha outbox'lardagi eventlari: jami va hali yetkazilmagani.</summary>
    private const string EventsOfUserSql = """
        SELECT count(*), count(*) FILTER (WHERE processed_at IS NULL)
        FROM (
            SELECT processed_at, content FROM identity.outbox_messages
            UNION ALL SELECT processed_at, content FROM practice.outbox_messages
            UNION ALL SELECT processed_at, content FROM discipline.outbox_messages
            UNION ALL SELECT processed_at, content FROM retention.outbox_messages
            UNION ALL SELECT processed_at, content FROM speech.outbox_messages
        ) AS outbox
        WHERE content->>'userId' = @userId
        """;

    [Fact]
    public async Task A_new_student_completes_day_one_through_every_module()
    {
        using var worker = system.CreateWorker().Build();
        await worker.StartAsync();

        // Identity: roʻyxatdan oʻtish, keyin hamma soʻrov Bearer token bilan.
        using var student = await Student.RegisterAsync(system.Api, "Journey Student");
        Assert.Equal("Journey Student", (await student.GetAsync("/api/v1/identity/me")).GetProperty("displayName").GetString());

        // Discipline: bugun 1-kun, darslar qulflangan; sessiya ertalabki darvozadan boshlanadi.
        var today = await student.GetAsync("/api/v1/discipline/today");
        Assert.Equal((1, false), (today.GetProperty("dayNumber").GetInt32(), today.GetProperty("lessonsUnlocked").GetBoolean()));
        Assert.Equal(HttpStatusCode.Forbidden, await student.StatusAsync("/api/v1/curriculum/days/1"));
        var session = await student.PostAsync("/api/v1/discipline/sessions/start", body: null);
        Assert.Equal("MorningGate", session.GetProperty("status").GetString());

        // Yangi oʻquvchida takrorlanadigan kartochka yoʻq: darvoza boʻsh javoblar roʻyxati bilan 100% oʻtadi.
        Assert.Equal(0, (await student.GetAsync("/api/v1/retention/due")).GetProperty("totalDue").GetInt32());
        var gate = await student.PostAsync("/api/v1/discipline/gate/submit", new { answers = Array.Empty<object>() });
        Assert.Equal(
            (true, 100m, true),
            (gate.GetProperty("assessmentValid").GetBoolean(), gate.GetProperty("score").GetDecimal(), gate.GetProperty("passed").GetBoolean()));
        today = await student.GetAsync("/api/v1/discipline/today");
        Assert.Equal((true, "Active"), (today.GetProperty("lessonsUnlocked").GetBoolean(), today.GetProperty("currentSessionState").GetString()));

        // Curriculum: kun ochildi.
        var day = await student.GetAsync("/api/v1/curriculum/days/1");
        Assert.Equal((28, 5), (day.GetProperty("vocabulary").GetProperty("words").GetArrayLength(), day.GetProperty("vocabulary").GetProperty("phrases").GetArrayLength()));

        // Worker DailyGatePassed'ni uch modulga yetkazadi: Retention kartochkalari, Speech kliplari, Analytics progressi.
        var due = await Eventually.GetAsync(
            () => student.GetAsync("/api/v1/retention/due?limit=100"), d => d.GetProperty("totalDue").GetInt32() == DayOneVocabulary);
        Assert.Equal("hello", due.GetProperty("cards")[0].GetProperty("front").GetString());
        var clips = await Eventually.GetAsync(
            () => student.GetAsync("/api/v1/speech/days/1/audio"),
            c => c.EnumerateArray().All(clip => clip.GetProperty("audioId").ValueKind == JsonValueKind.String));
        Assert.Equal(DayOneVocabulary, clips.GetArrayLength());
        var progress = await Eventually.GetAsync(
            () => student.GetAsync("/api/v1/analytics/progress"), p => p.GetProperty("activeVocabulary").GetInt32() == DayOneVocabulary);
        Assert.Equal((1, 1), (progress.GetProperty("currentDay").GetInt32(), progress.GetProperty("activeStreak").GetInt32()));

        // Practice: kunning gap-fill va test savoliga toʻgʻri javob.
        var exercises = (await student.GetAsync("/api/v1/practice/days/1/exercises")).EnumerateArray().ToList();
        var gapFill = exercises.Single(e => e.GetProperty("type").GetString() == "GapFill");
        var hello = await student.PostAsync("/api/v1/practice/attempts", new { exerciseId = gapFill.GetProperty("id").GetGuid(), answer = "Hello", timeTakenMs = 4_000 });
        var magicE = exercises.Single(e => e.GetProperty("choices").EnumerateArray().Any(c => c.GetProperty("text").GetString() == "cape"));
        var cape = magicE.GetProperty("choices").EnumerateArray().Single(c => c.GetProperty("text").GetString() == "cape");
        var choice = await student.PostAsync("/api/v1/practice/attempts", new
        {
            exerciseId = magicE.GetProperty("id").GetGuid(),
            selectedOptionId = cape.GetProperty("id").GetGuid(),
            timeTakenMs = 3_000,
        });
        Assert.Equal(("Correct", "Correct"), (hello.GetProperty("verdict").GetString(), choice.GetProperty("verdict").GetString()));

        // Retention: birinchi kartochka «Good» (3) bilan: oʻrganish bosqichiga oʻtadi va navbatdan chiqadi.
        var review = await student.PostAsync("/api/v1/retention/review", new
        {
            cardId = due.GetProperty("cards")[0].GetProperty("id").GetGuid(),
            rating = 3,
            durationMs = 2_500,
        });
        Assert.Equal(("Learning", false), (review.GetProperty("phase").GetString(), review.GetProperty("lapsed").GetBoolean()));
        Assert.Equal(DayOneVocabulary - 1, (await student.GetAsync("/api/v1/retention/due")).GetProperty("totalDue").GetInt32());

        // Speech: kun iborasining talaffuzi. Whisper stub'i aynan shuni eshitadi; Worker yaratgan namuna klip ulanadi.
        system.Speech.Transcript = "Nice to meet you.";
        var recording = AudioSamples.Wav();
        var assessment = await student.AssessAsync(recording, "Nice to meet you.");
        Assert.Equal(
            (100m, "Correct", 100m),
            (assessment.GetProperty("overallScore").GetDecimal(), assessment.GetProperty("verdict").GetString(), assessment.GetProperty("fluencyScore").GetDecimal()));
        var recordingId = assessment.GetProperty("recordingId").GetGuid();
        var referenceAudioId = assessment.GetProperty("referenceAudioId").GetGuid();
        Assert.Equal(SpeechProviderStub.Mp3, await student.GetBytesAsync($"/api/v1/speech/audio/{referenceAudioId}"));
        Assert.Equal(recording, await student.GetBytesAsync($"/api/v1/speech/audio/{recordingId}"));

        // Discipline: 25 daqiqalik fokus bloki bilan sessiya yakunlanadi.
        var completed = await student.PostAsync("/api/v1/discipline/sessions/complete", new
        {
            sessionId = session.GetProperty("sessionId").GetGuid(),
            focusSeconds = 1_500,
            distractionCount = 1,
            minimalDay = true,
        });
        Assert.Equal(("Completed", 1_500), (completed.GetProperty("status").GetString(), completed.GetProperty("totalFocusSeconds").GetInt32()));

        // Analytics: urinishlar, fokus vaqti va yakunlangan kun read model'ga yetdi.
        progress = await Eventually.GetAsync(
            () => student.GetAsync("/api/v1/analytics/progress"),
            p => p.GetProperty("totalPracticeAttempts").GetInt32() == 2 && p.GetProperty("focusMinutesThisWeek").GetInt32() == 25);
        Assert.Equal(
            (1, DayOneVocabulary, 1, 1),
            (progress.GetProperty("currentDay").GetInt32(), progress.GetProperty("activeVocabulary").GetInt32(),
                progress.GetProperty("completedDays").GetInt32(), progress.GetProperty("activeStreak").GetInt32()));
        var weekly = await student.GetAsync("/api/v1/analytics/reports/weekly");
        Assert.Equal(
            (DayOneVocabulary, 25m, 2),
            (weekly.GetProperty("wordsLearned").GetInt32(), weekly.GetProperty("totalFocusMinutes").GetDecimal(), weekly.GetProperty("totalPracticeAttempts").GetInt32()));

        // Yoʻl davomidagi 7 event (UserRegistered, DailyGatePassed, 2 × AttemptEvaluated, BlockCompleted, SessionEnded,
        // TranscriptReady) toʻliq yetkazilgan: birortasi ham outbox'da osilib qolmagan.
        var (total, pending) = await Eventually.GetAsync(() => EventsOfAsync(student.UserId), events => events.Pending == 0);
        Assert.Equal(7, total);

        // Boshqa oʻquvchi bu maʼlumotlarni koʻrmaydi; umumiy namuna klip esa hammaga ochiq.
        using var classmate = await Student.RegisterAsync(system.Api, "Classmate");
        Assert.Equal(0, (await classmate.GetAsync("/api/v1/retention/due")).GetProperty("totalDue").GetInt32());
        Assert.Equal(0, (await classmate.GetAsync("/api/v1/analytics/progress")).GetProperty("totalPracticeAttempts").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, await classmate.StatusAsync($"/api/v1/speech/audio/{recordingId}"));
        Assert.Equal(HttpStatusCode.OK, await classmate.StatusAsync($"/api/v1/speech/audio/{referenceAudioId}"));

        await worker.StopAsync();
    }

    private async Task<(long Total, long Pending)> EventsOfAsync(Guid userId)
    {
        await using var connection = new NpgsqlConnection(system.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(EventsOfUserSql, connection);
        command.Parameters.AddWithValue("userId", userId.ToString());
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt64(0), reader.GetInt64(1));
    }
}
