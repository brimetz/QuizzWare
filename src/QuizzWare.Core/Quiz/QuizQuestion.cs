using QuizzWare.Core.Models;

namespace QuizzWare.Core.Quiz;

/// <summary>Réponse donnée par l'utilisateur ; le type dépend du type de question.</summary>
public abstract record Answer;
public sealed record ChoiceAnswer(int Index) : Answer;
public sealed record MultiChoiceAnswer(IReadOnlyCollection<int> Indices) : Answer;
public sealed record LineAnswer(int LineIndex) : Answer;
public sealed record FillAnswer(IReadOnlyList<string> Tokens) : Answer;
public sealed record OrderAnswer(IReadOnlyList<string> Lines) : Answer;

/// <summary>Question prête à être jouée : choix mélangés, code découpé en lignes, etc.</summary>
public abstract record QuizQuestion(Question Source)
{
    public abstract bool Grade(Answer answer);

    protected static T Expect<T>(Answer answer) where T : Answer =>
        answer as T ?? throw new ArgumentException($"Réponse de type {typeof(T).Name} attendue, reçu {answer.GetType().Name}.");
}

public sealed record ChoiceQuizQuestion(Question Source, IReadOnlyList<string> Choices, int CorrectIndex) : QuizQuestion(Source)
{
    public override bool Grade(Answer answer) => Expect<ChoiceAnswer>(answer).Index == CorrectIndex;
}

public sealed record MultiChoiceQuizQuestion(Question Source, IReadOnlyList<string> Choices, IReadOnlySet<int> CorrectIndices) : QuizQuestion(Source)
{
    /// <summary>Juste uniquement si toutes les bonnes réponses, et elles seules, sont cochées.</summary>
    public override bool Grade(Answer answer) => CorrectIndices.SetEquals(Expect<MultiChoiceAnswer>(answer).Indices);
}

public sealed record BugQuizQuestion(Question Source, IReadOnlyList<string> Lines, IReadOnlySet<int> BugLineIndices) : QuizQuestion(Source)
{
    public override bool Grade(Answer answer) => BugLineIndices.Contains(Expect<LineAnswer>(answer).LineIndex);
}

/// <summary><see cref="Segments"/> contient le code découpé autour des trous : il y a toujours un segment de plus que de trous.</summary>
public sealed record FillQuizQuestion(Question Source, IReadOnlyList<string> Segments, IReadOnlyList<string> Tokens, IReadOnlyList<string> Answers) : QuizQuestion(Source)
{
    public override bool Grade(Answer answer) => Expect<FillAnswer>(answer).Tokens.SequenceEqual(Answers);

    /// <summary>Le code avec les trous remplis par <paramref name="tokens"/> (ou laissés vides).</summary>
    public string Compose(IReadOnlyList<string?> tokens)
    {
        var builder = new System.Text.StringBuilder(Segments[0]);
        for (var i = 1; i < Segments.Count; i++)
            builder.Append(tokens.ElementAtOrDefault(i - 1) ?? FillQuestion.Blank).Append(Segments[i]);
        return builder.ToString();
    }
}

public sealed record OrderQuizQuestion(Question Source, IReadOnlyList<string> ShuffledLines, IReadOnlyList<string> CorrectLines) : QuizQuestion(Source)
{
    /// <summary>On compare le texte des lignes : deux lignes identiques sont interchangeables.</summary>
    public override bool Grade(Answer answer) => Expect<OrderAnswer>(answer).Lines.SequenceEqual(CorrectLines);
}
