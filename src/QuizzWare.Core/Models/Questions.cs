using System.Text.Json.Serialization;

namespace QuizzWare.Core.Models;

/// <summary>
/// Question de quizz. Tous les types se jouent sans clavier (l'utilisateur est sur téléphone) :
/// on choisit, on touche une ligne, on place des mots ou on ordonne des lignes.
/// Le champ JSON <c>type</c> indique le type concret.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChoiceQuestion), "choice")]
[JsonDerivedType(typeof(MultiChoiceQuestion), "multi")]
[JsonDerivedType(typeof(BugQuestion), "bug")]
[JsonDerivedType(typeof(FillQuestion), "fill")]
[JsonDerivedType(typeof(OrderQuestion), "order")]
public abstract record Question
{
    public required string Id { get; init; }
    public required string Prompt { get; init; }
    public string? Code { get; init; }

    /// <summary>Langage du code, si différent de celui de la catégorie (ex. une requête MongoDB dans « Bases de données »).</summary>
    public string? Language { get; init; }

    public required string Explanation { get; init; }

    /// <summary>De 1 (facile) à 3 (difficile).</summary>
    public int Difficulty { get; init; } = 1;
}

/// <summary>QCM à réponse unique. Avec <see cref="Question.Code"/>, sert aussi à « prédire la sortie ».</summary>
public sealed record ChoiceQuestion : Question
{
    public required IReadOnlyList<string> Choices { get; init; }
    public int AnswerIndex { get; init; }
}

/// <summary>Plusieurs bonnes réponses (« coche tous les problèmes de ce code »).</summary>
public sealed record MultiChoiceQuestion : Question
{
    public required IReadOnlyList<string> Choices { get; init; }
    public required IReadOnlyList<int> AnswerIndices { get; init; }
}

/// <summary>Trouver le bug : l'utilisateur touche la ligne fautive dans <see cref="Question.Code"/>.</summary>
public sealed record BugQuestion : Question
{
    /// <summary>Numéros des lignes acceptées comme réponse, à partir de 1 (comme affiché).</summary>
    public required IReadOnlyList<int> BugLines { get; init; }
}

/// <summary>
/// Code à trous : chaque <c>___</c> dans <see cref="Question.Code"/> est un trou à remplir
/// avec les mots proposés (les réponses mélangées à des leurres).
/// </summary>
public sealed record FillQuestion : Question
{
    public const string Blank = "___";

    public required IReadOnlyList<string> Answers { get; init; }
    public IReadOnlyList<string> Distractors { get; init; } = [];
}

/// <summary>Remettre des lignes dans l'ordre. <see cref="Lines"/> est l'ordre correct.</summary>
public sealed record OrderQuestion : Question
{
    public required IReadOnlyList<string> Lines { get; init; }
}
