using System.Security.Claims;
using EnglishSchool.BuildingBlocks.Security;
using EnglishSchool.Modules.Speech.Application;
using EnglishSchool.Modules.Speech.Contracts;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Scoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace EnglishSchool.Modules.Speech.Endpoints;

/// <summary><c>/api/v1/speech</c>: talaffuz bahosi va audio fayllar. Hammasi faqat kirgan oʻquvchi uchun.</summary>
internal static class SpeechEndpoints
{
    public const int MaxTargetTextLength = 300;

    /// <summary>Formadagi matn maydoni va multipart chegaralari uchun yozuv hajmidan tashqari joy.</summary>
    private const int FormOverheadBytes = 64 * 1024;

    public static void MapSpeechEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var maxRequestBytes = endpoints.ServiceProvider.GetRequiredService<IOptions<SpeechOptions>>().Value.MaxRecordingBytes + FormOverheadBytes;
        var group = endpoints.MapGroup("/speech").RequireAuthorization();

        // Token Authorization sarlavhasida keladi, cookie'da emas: CSRF xavfi yoʻq, antiforgery kerak emas.
        group.MapPost("/assess", AssessAsync)
            .DisableAntiforgery()
            .WithFormOptions(multipartBodyLengthLimit: maxRequestBytes)
            .WithMetadata(new RequestSizeLimit(maxRequestBytes));
        group.MapGet("/audio/{id:guid}", GetAudioAsync);
        group.MapGet("/days/{dayNumber:int}/audio", GetDayAudioAsync);
    }

    private static async Task<Results<Ok<PronunciationAssessmentDto>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> AssessAsync(
        IFormFile? audio,
        [FromForm] string? targetText,
        ClaimsPrincipal principal,
        PronunciationService pronunciation,
        IOptions<SpeechOptions> options,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (Validate(audio, targetText, options.Value) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var content = new byte[audio!.Length];
        await using (var stream = audio.OpenReadStream())
        {
            await stream.ReadExactlyAsync(content, cancellationToken);
        }

        var outcome = await pronunciation.AssessAsync(userId, targetText!, content, cancellationToken);
        return outcome.Status switch
        {
            AssessmentStatus.UnsupportedAudio => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["audio"] = ["Audio formati tanilmadi. WebM, MP4 (M4A), WAV, MP3, OGG yoki FLAC yozuv yuboring."],
            }),
            AssessmentStatus.ProviderUnavailable => TypedResults.Problem(
                detail: "Talaffuzni baholash hozircha yoqilmagan.", statusCode: StatusCodes.Status503ServiceUnavailable),
            AssessmentStatus.ProviderFailed => TypedResults.Problem(
                detail: "Nutqni tanish xizmati vaqtincha ishlamayapti. Birozdan keyin qayta urinib koʻring.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            AssessmentStatus.LimitReached => TypedResults.Problem(
                detail: $"24 soat ichida {options.Value.DailyAssessmentLimit} ta yozuv baholandi: bu kunlik chegara. Keyinroq davom eting.",
                statusCode: StatusCodes.Status429TooManyRequests),
            _ => TypedResults.Ok(outcome.Result!),
        };
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, UnauthorizedHttpResult>> GetAudioAsync(
        Guid id, ClaimsPrincipal principal, AudioQueries queries, HttpResponse response, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (await queries.OpenAsync(userId, id, cancellationToken) is not { } audio)
        {
            return TypedResults.NotFound();
        }

        // Sintez qilingan klip oʻzgarmaydi (yangi ovoz — yangi kalit); oʻquvchi yozuvi shaxsiy.
        response.Headers.CacheControl = audio.Artifact.Kind == AudioKind.Synthesized
            ? "private, max-age=31536000, immutable"
            : "private, no-cache";

        return TypedResults.Stream(
            audio.Content,
            audio.Artifact.ContentType,
            lastModified: audio.Artifact.CreatedAt,
            entityTag: new EntityTagHeaderValue($"\"{audio.Artifact.Sha256}\""),
            enableRangeProcessing: true);
    }

    private static async Task<Results<Ok<IReadOnlyList<VocabularyClipDto>>, NotFound, UnauthorizedHttpResult>> GetDayAudioAsync(
        int dayNumber, ClaimsPrincipal principal, AudioQueries queries, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out _))
        {
            return TypedResults.Unauthorized();
        }

        var clips = await queries.GetDayClipsAsync(dayNumber, cancellationToken);
        return clips is null ? TypedResults.NotFound() : TypedResults.Ok(clips);
    }

    private static Dictionary<string, string[]> Validate(IFormFile? audio, string? targetText, SpeechOptions options)
    {
        var errors = new Dictionary<string, string[]>();

        if (audio is null || audio.Length == 0)
        {
            errors["audio"] = ["Ovoz yozuvi yuborilmagan (multipart maydoni: audio)."];
        }
        else if (audio.Length > options.MaxRecordingBytes)
        {
            errors["audio"] = [$"Yozuv juda katta: eng koʻpi {options.MaxRecordingBytes / (1024 * 1024.0):0.#} MB."];
        }

        if (string.IsNullOrWhiteSpace(targetText))
        {
            errors["targetText"] = ["Aytilishi kerak boʻlgan matn berilmagan."];
        }
        else if (targetText.Length > MaxTargetTextLength)
        {
            errors["targetText"] = [$"Matn {MaxTargetTextLength} belgidan oshmasin."];
        }
        else if (TextNormalizer.Words(targetText).Count == 0)
        {
            errors["targetText"] = ["Matnda kamida bitta inglizcha soʻz boʻlishi kerak."];
        }

        return errors;
    }

    /// <summary>Kestrel'ga shu endpoint uchun soʻrov tanasi chegarasi.</summary>
    private sealed class RequestSizeLimit(long maxRequestBodySize) : IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize { get; } = maxRequestBodySize;
    }
}
