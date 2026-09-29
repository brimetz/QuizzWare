using System.Text.Json.Serialization;

namespace QuizzWare.Core.Models;

/// <summary>
/// Un domaine d'apprentissage (Réseau, C++, Bases de données...) : un tronc commun de chapitres,
/// et éventuellement un arbre de spécialisations qui ont chacune leur propre parcours.
/// </summary>
public sealed record Category(
    string Id,
    int Order,
    string Name,
    string Description,
    string Icon,
    string Color,
    IReadOnlyList<Chapter> Chapters,
    string? Language = null,
    SpecializationTree? Specializations = null)
{
    /// <summary>Le tronc commun puis le parcours de chaque spécialisation. Les leçons se débloquent dans l'ordre, parcours par parcours.</summary>
    [JsonIgnore]
    public IEnumerable<IReadOnlyList<Chapter>> Paths =>
        (Specializations?.AllNodes.Where(n => n.HasTrack).Select(n => n.Track) ?? []).Prepend(Chapters);

    [JsonIgnore]
    public IEnumerable<Chapter> AllChapters => Paths.SelectMany(p => p);

    /// <summary>Toutes les leçons, tronc commun et spécialisations compris.</summary>
    [JsonIgnore]
    public IEnumerable<Lesson> AllLessons => AllChapters.SelectMany(c => c.Lessons);

    /// <summary>Les leçons du tronc commun uniquement.</summary>
    [JsonIgnore]
    public IEnumerable<Lesson> MainLessons => Chapters.SelectMany(c => c.Lessons);

    /// <summary>Au moins une leçon jouable, dans le tronc commun ou une spécialisation.</summary>
    [JsonIgnore]
    public bool HasContent => AllLessons.Any(l => !l.IsPlanned);
}

/// <summary>Un chapitre : une suite de leçons, éventuellement conclue par un défi entretien.</summary>
public sealed record Chapter(
    string Id,
    string Title,
    string Summary,
    IReadOnlyList<Lesson> Lessons,
    Challenge? Challenge = null);

/// <summary>Tout ce qui se termine par un quizz : une leçon ou un défi.</summary>
public interface IQuizUnit
{
    string Id { get; }
    string Title { get; }
    IReadOnlyList<Question> Questions { get; }
}

/// <summary>
/// Un cours composé de sections, suivi d'un quizz (questions mélangées).
/// Sans questions, la leçon est un emplacement réservé : elle apparaît « à venir » dans le parcours.
/// </summary>
public sealed record Lesson(
    string Id,
    string Title,
    string Summary,
    int Difficulty,
    IReadOnlyList<LessonSection>? Sections,
    IReadOnlyList<Question>? Questions) : IQuizUnit
{
    [JsonIgnore]
    public bool IsPlanned => Questions is null || Questions.Count == 0;

    IReadOnlyList<Question> IQuizUnit.Questions => Questions ?? [];
}

/// <summary>
/// Défi de fin de chapitre, façon entretien technique. Les questions sont posées dans l'ordre,
/// ce qui permet d'enchaîner les étapes d'un cas pratique (trouver le bug → le corriger → l'expliquer).
/// </summary>
public sealed record Challenge(
    string Id,
    string Title,
    string Summary,
    IReadOnlyList<Question>? Questions) : IQuizUnit
{
    [JsonIgnore]
    public bool IsPlanned => Questions is null || Questions.Count == 0;

    IReadOnlyList<Question> IQuizUnit.Questions => Questions ?? [];
}

/// <param name="Language">Langage du code, si différent de celui de la catégorie.</param>
public sealed record LessonSection(string Title, string Body, string? Code = null, string? Language = null);
