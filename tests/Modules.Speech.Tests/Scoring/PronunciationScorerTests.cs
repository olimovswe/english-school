using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Providers;
using EnglishSchool.Modules.Speech.Scoring;

namespace EnglishSchool.Modules.Speech.Tests.Scoring;

public sealed class PronunciationScorerTests
{
    [Fact]
    public void An_exact_transcript_scores_100_whatever_the_case_and_punctuation()
    {
        var score = Score("Where are you from?", "where are you from");

        Assert.Equal((100m, 100m, 100m, PronunciationVerdict.Correct), (score.Overall, score.Accuracy, score.Completeness, score.Verdict));
        Assert.Equal(["where", "are", "you", "from"], score.Words.Select(w => w.Expected));
        Assert.All(score.Words, w => Assert.Equal((WordStatus.Correct, 1.0, null), (w.Status, w.Similarity, w.FeedbackCode)));
    }

    [Theory]
    [InlineData("I'm 25.", "I am twenty-five")]
    [InlineData("It's 30 degrees.", "It is thirty degrees.")]
    [InlineData("Don't worry.", "Do not worry")]
    [InlineData("I can't swim.", "I cannot swim.")]
    [InlineData("Iʼm fine.", "I'm fine.")]
    [InlineData("Let's go to the café.", "Let us go to the cafe")]
    [InlineData("Are you OK?", "Are you okay?")]
    public void Equivalent_spellings_of_the_same_speech_count_as_exact(string target, string transcript)
    {
        var score = Score(target, transcript);

        Assert.Equal((100m, PronunciationVerdict.Correct), (score.Overall, score.Verdict));
        Assert.All(score.Words, w => Assert.Equal(WordStatus.Correct, w.Status));
    }

    [Fact]
    public void A_th_said_as_s_or_t_is_mispronounced_earns_half_credit_and_gets_a_hint()
    {
        var score = Score("I think it's three o'clock.", "I sink it is tree o'clock.");

        var think = score.Words[1];
        var three = score.Words[4];
        Assert.Equal(("think", "sink", 0.675, WordStatus.Mispronounced, FeedbackCodes.ThSound), (think.Expected, think.Heard, think.Similarity, think.Status, think.FeedbackCode));
        Assert.Equal(("three", "tree", 0.733, WordStatus.Mispronounced, FeedbackCodes.ThSound), (three.Expected, three.Heard, three.Similarity, three.Status, three.FeedbackCode));

        // (4 + 0,675/2 + 0,733/2) / 6 = 0,784. Hech bir soʻz tushib qolmagan.
        Assert.Equal((78.4m, 100m, PronunciationVerdict.PartiallyCorrect), (score.Accuracy, score.Completeness, score.Verdict));
    }

    [Fact]
    public void A_missing_word_lowers_accuracy_and_completeness()
    {
        var score = Score("I'm a student.", "I am student");

        var missing = Assert.Single(score.Words, w => w.Status == WordStatus.Missing);
        Assert.Equal(("a", null, FeedbackCodes.MissingWord), (missing.Expected, missing.Heard, missing.FeedbackCode));
        Assert.Equal((75m, 75m, PronunciationVerdict.PartiallyCorrect), (score.Accuracy, score.Completeness, score.Verdict));
    }

    [Fact]
    public void Extra_words_are_kept_in_order_and_lower_accuracy()
    {
        var score = Score("How are you?", "How how are you doing?");

        Assert.Equal(
            [(null, "how", WordStatus.Extra), ("how", "how", WordStatus.Correct), ("are", "are", WordStatus.Correct), ("you", "you", WordStatus.Correct), (null, "doing", WordStatus.Extra)],
            score.Words.Select(w => (w.Expected, w.Heard, w.Status)));

        // 3 / (3 + 2 × 0,5): ortiqcha soʻzlar ham hisobga olinadi, lekin matn toʻliq aytilgan.
        Assert.Equal((75m, 100m, PronunciationVerdict.PartiallyCorrect), (score.Accuracy, score.Completeness, score.Verdict));
    }

    [Fact]
    public void A_completely_different_sentence_is_incorrect()
    {
        var score = Score("Where are you from?", "The weather is nice today");

        Assert.Equal((5.7m, 25m, PronunciationVerdict.Incorrect), (score.Overall, score.Completeness, score.Verdict));
        Assert.Equal(
            [WordStatus.Extra, WordStatus.Mispronounced, WordStatus.Missing, WordStatus.Missing, WordStatus.Missing],
            score.Words.Select(w => w.Status));
        Assert.All(score.Words.Where(w => w.Status == WordStatus.Missing), w => Assert.Equal(FeedbackCodes.WrongWord, w.FeedbackCode));
    }

    [Fact]
    public void Silence_scores_zero_with_every_word_missing()
    {
        var score = Score("Nice to meet you.", string.Empty);

        Assert.Equal((0m, 0m, 0m, PronunciationVerdict.Incorrect), (score.Overall, score.Accuracy, score.Completeness, score.Verdict));
        Assert.All(score.Words, w => Assert.Equal((WordStatus.Missing, FeedbackCodes.MissingWord), (w.Status, w.FeedbackCode)));
    }

    [Fact]
    public void A_homophone_written_differently_is_close_and_still_correct_overall()
    {
        var score = Score("Their house is big.", "There house is big.");

        Assert.Equal((0.8, WordStatus.Close), (score.Words[0].Similarity, score.Words[0].Status));
        Assert.Equal((95m, PronunciationVerdict.Correct), (score.Overall, score.Verdict));
    }

    [Theory]
    [InlineData("think", "sink", "Mispronounced", FeedbackCodes.ThSound)]
    [InlineData("this", "dis", "Mispronounced", FeedbackCodes.ThSound)]
    [InlineData("thank", "tank", "Close", FeedbackCodes.ThSound)]
    [InlineData("wine", "vine", "Mispronounced", FeedbackCodes.WvSound)]
    [InlineData("very", "wery", "Close", FeedbackCodes.WvSound)]
    [InlineData("cats", "cat", "Close", FeedbackCodes.FinalConsonant)]
    [InlineData("cold", "coal", "Mispronounced", FeedbackCodes.FinalConsonant)]
    [InlineData("apple", "house", "Missing", FeedbackCodes.WrongWord)]
    public void Typical_substitutions_get_their_status_and_feedback_code(string expected, string heard, string status, string code)
    {
        var word = Assert.Single(Score(expected, heard).Words);

        Assert.Equal((expected, heard, status, code), (word.Expected, word.Heard, word.Status.ToString(), word.FeedbackCode));
    }

    [Fact]
    public void A_correct_verdict_needs_every_word_right_even_in_a_long_sentence()
    {
        // 10 soʻzdan bittasi notoʻgʻri: aniqlik 90 dan yuqori, lekin hukm «qisman toʻgʻri».
        var score = Score("Samarkand is one of the oldest cities in the world.", "Samarkand is one of the oldest sitiz in the world.");

        Assert.True(score.Accuracy >= 90, $"Aniqlik {score.Accuracy}");
        Assert.Equal(PronunciationVerdict.PartiallyCorrect, score.Verdict);
    }

    [Fact]
    public void Fluency_counts_long_pauses_and_blends_into_the_overall_score()
    {
        var score = Score(
            "Samarkand is one of the oldest cities in the world.",
            "Samarkand is one of the oldest cities in the world.",
            ("Samarkand", 0.0, 0.6), ("is", 0.7, 0.8), ("one", 0.9, 1.0), ("of", 1.0, 1.1), ("the", 1.1, 1.2),
            ("oldest", 2.5, 2.9), ("cities", 2.9, 3.3), ("in", 3.3, 3.4), ("the", 3.4, 3.5), ("world", 3.5, 3.9));

        // 3,9 s nutqning 1,3 s'i jimlik: ravonlik 100 × (1 − 1,3/3,9).
        Assert.Equal(new FluencyMetrics(3900, 153.8, 1, 1300), score.FluencyMetrics);
        Assert.Equal((100m, 66.7m, 93.3m, PronunciationVerdict.Correct), (score.Accuracy, score.Fluency, score.Overall, score.Verdict));
    }

    [Fact]
    public void Slow_speech_lowers_fluency_even_without_pauses()
    {
        var score = Score(
            "Nice to meet you.",
            "Nice to meet you.",
            ("Nice", 0.0, 1.4), ("to", 1.6, 3.0), ("meet", 3.2, 4.6), ("you", 4.8, 6.2));

        // 4 soʻz 6,2 s'da = 38,7 soʻz/daqiqa; ravon tezlik 70 dan.
        Assert.Equal(new FluencyMetrics(6200, 38.7, 0, 0), score.FluencyMetrics);
        Assert.Equal((55.3m, 91.1m), (score.Fluency, score.Overall));
    }

    [Theory]
    [InlineData("Hello", true)]
    [InlineData("Nice to meet you.", false)]
    public void Fluency_needs_a_few_words_and_their_timestamps(string target, bool withTimestamps)
    {
        (string, double, double)[] words = withTimestamps ? [("Hello", 0.0, 0.4)] : [];

        var score = Score(target, target, words);

        Assert.Equal(((decimal?)null, (FluencyMetrics?)null, score.Accuracy), (score.Fluency, score.FluencyMetrics, score.Overall));
    }

    [Fact]
    public void A_target_without_words_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Score("… ?!", "hello"));
    }

    private static PronunciationScore Score(string target, string transcript, params (string Word, double Start, double End)[] words) =>
        PronunciationScorer.Score(target, new Transcription(transcript, null, [.. words.Select(w => new TranscribedWord(w.Word, w.Start, w.End))]));
}
