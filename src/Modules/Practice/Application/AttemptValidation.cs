using EnglishSchool.Modules.Practice.Domain;

namespace EnglishSchool.Modules.Practice.Application;

/// <summary>Soʻrovni ikki bosqichda tekshiradi: avval umumiy maydonlar, mashq topilgach — javob shakli uning turiga mosligi.</summary>
internal static class AttemptValidation
{
    public const int MaxAnswerLength = 200;
    private const int MaxTimeTakenMs = 60 * 60 * 1000;

    public static Dictionary<string, string[]> ValidateRequest(SubmitAttemptRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ExerciseId is null || request.ExerciseId == Guid.Empty)
        {
            errors["exerciseId"] = ["Mashq tanlanmagan."];
        }

        if (request.TimeTakenMs is not (>= 0 and <= MaxTimeTakenMs))
        {
            errors["timeTakenMs"] = ["Sarflangan vaqt 0 va 1 soat oraligʻida (millisekundda) boʻlishi kerak."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateAnswer(Exercise exercise, SubmitAttemptRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        switch (exercise.Type)
        {
            case ExerciseType.GapFill when request.Answer?.Trim() is not { Length: > 0 and <= MaxAnswerLength }:
                errors["answer"] = [$"Javob boʻsh boʻlmasligi va {MaxAnswerLength} belgidan oshmasligi kerak."];
                break;

            case ExerciseType.MultipleChoice when exercise.Options.All(o => o.Id != request.SelectedOptionId):
                errors["selectedOptionId"] = ["Variantlardan birini tanlang."];
                break;

            case ExerciseType.Matching when !IsCompleteMatching(exercise, request.Matches):
                errors["matches"] = ["Har bir soʻz uchun bitta juftlik yuboring."];
                break;
        }

        return errors;
    }

    private static bool IsCompleteMatching(Exercise exercise, IReadOnlyList<MatchAnswer>? matches) =>
        matches is not null
        && matches.Count == exercise.Options.Count
        && matches.Select(m => m.OptionId).Distinct().Count() == matches.Count
        && matches.All(m => exercise.Options.Any(o => o.Id == m.OptionId)
            && m.MatchText?.Trim() is { Length: > 0 and <= MaxAnswerLength });
}
