using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Speech.Contracts.Events;

/// <summary>
/// Oʻquvchi yozuvi matnga aylantirildi (Whisper). Baho bilan bitta tranzaksiyada outbox orqali chiqadi.
/// </summary>
/// <param name="JobId">Ishning identifikatori: talaffuz bahosida — baho (<see cref="SpeechPurposes.Pronunciation"/>).</param>
/// <param name="Purpose">Yozuv nima uchun yuborilgan: <see cref="SpeechPurposes"/>.</param>
/// <param name="Transcript">Whisper eshitgan matn, oʻzgartirilmagan holda.</param>
public sealed record TranscriptReady(Guid UserId, Guid JobId, string Purpose, string Transcript) : IntegrationEvent;
