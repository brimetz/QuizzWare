using QuizzWare.Core.Models;

namespace QuizzWare.Core.Content;

/// <summary>Vérifie la cohérence du contenu au chargement, pour détecter tôt une erreur dans un fichier JSON.</summary>
public static class ContentValidator
{
    public static void Validate(IReadOnlyList<Category> categories)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>();

        void CheckId(string? id, string what)
        {
            if (string.IsNullOrWhiteSpace(id))
                errors.Add($"{what} sans identifiant.");
            else if (!ids.Add(id))
                errors.Add($"Identifiant en double : {id}");
        }

        foreach (var category in categories)
        {
            CheckId(category.Id, $"Catégorie « {category.Name} »");
            if (category.Chapters is null || category.Chapters.Count == 0)
            {
                errors.Add($"{category.Id} : aucun chapitre.");
                continue;
            }

            if (category.Specializations is { } tree)
            {
                if (tree.UnlockedBy is { } chapterId && category.Chapters.All(c => c.Id != chapterId))
                    errors.Add($"{category.Id} : unlockedBy référence un chapitre inconnu du tronc commun ({chapterId}).");

                foreach (var node in tree.AllNodes)
                {
                    CheckId(node.Id, $"Spécialisation « {node.Title} »");
                    if (!SpecializationKind.All.Contains(node.Kind))
                        errors.Add($"{node.Id} : kind « {node.Kind} » inconnu (attendu : {string.Join(", ", SpecializationKind.All)}).");
                    if (node.Kind == SpecializationKind.Family && node.ChildNodes.Count == 0)
                        errors.Add($"{node.Id} : une famille doit avoir au moins une spécialisation.");
                    if (node.RelatedCategory is { } related && categories.All(c => c.Id != related))
                        errors.Add($"{node.Id} : relatedCategory inconnue ({related}).");
                    if (node.LinkTo is { } link)
                        errors.AddRange(ValidateLink(node, link, categories).Select(e => $"{node.Id} : {e}"));
                    else if (node.IsDoor)
                        errors.Add($"{node.Id} : une porte doit indiquer la catégorie visée (linkTo).");
                }
            }

            // Tronc commun et parcours des spécialisations
            foreach (var chapter in category.AllChapters)
            {
                CheckId(chapter.Id, $"Chapitre « {chapter.Title} »");
                if (chapter.Lessons is null || chapter.Lessons.Count == 0)
                    errors.Add($"{chapter.Id} : aucune leçon.");

                foreach (var lesson in chapter.Lessons ?? [])
                {
                    CheckId(lesson.Id, $"Leçon « {lesson.Title} »");
                    if (lesson.IsPlanned)
                        continue; // emplacement réservé : contenu à venir

                    if (lesson.Sections is null || lesson.Sections.Count == 0)
                        errors.Add($"{lesson.Id} : aucune section de cours.");
                    ValidateQuestions(lesson.Id, lesson.Questions, CheckId, errors);
                }

                if (chapter.Challenge is { } challenge)
                {
                    CheckId(challenge.Id, $"Défi « {challenge.Title} »");
                    if (!challenge.IsPlanned)
                        ValidateQuestions(challenge.Id, challenge.Questions, CheckId, errors);
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidDataException("Contenu invalide :" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    private static void ValidateQuestions(string unitId, IReadOnlyList<Question>? questions, Action<string?, string> checkId, List<string> errors)
    {
        if (questions is null || questions.Count == 0)
        {
            errors.Add($"{unitId} : aucune question.");
            return;
        }

        foreach (var q in questions)
        {
            checkId(q.Id, $"Question de {unitId}");
            if (q.Difficulty is < 1 or > 3)
                errors.Add($"{q.Id} : la difficulté doit être comprise entre 1 et 3.");
            if (string.IsNullOrWhiteSpace(q.Explanation))
                errors.Add($"{q.Id} : explication manquante.");

            foreach (var error in ValidateByType(q))
                errors.Add($"{q.Id} : {error}");
        }
    }

    private static IEnumerable<string> ValidateByType(Question question)
    {
        switch (question)
        {
            case ChoiceQuestion q:
                foreach (var e in ValidateChoices(q.Choices)) yield return e;
                if (q.AnswerIndex < 0 || q.AnswerIndex >= q.Choices.Count)
                    yield return "answerIndex hors limites.";
                break;

            case MultiChoiceQuestion q:
                foreach (var e in ValidateChoices(q.Choices)) yield return e;
                if (q.AnswerIndices.Count == 0)
                    yield return "answerIndices ne doit pas être vide.";
                if (q.AnswerIndices.Any(i => i < 0 || i >= q.Choices.Count))
                    yield return "answerIndices hors limites.";
                if (q.AnswerIndices.Distinct().Count() != q.AnswerIndices.Count)
                    yield return "answerIndices en double.";
                break;

            case BugQuestion q:
                if (string.IsNullOrWhiteSpace(q.Code))
                {
                    yield return "une question « bug » doit contenir du code.";
                    break;
                }
                var lineCount = CodeText.SplitLines(q.Code).Count;
                if (q.BugLines.Count == 0)
                    yield return "bugLines ne doit pas être vide.";
                if (q.BugLines.Any(n => n < 1 || n > lineCount))
                    yield return $"bugLines hors limites (le code fait {lineCount} lignes, numérotées à partir de 1).";
                break;

            case FillQuestion q:
                if (string.IsNullOrWhiteSpace(q.Code))
                {
                    yield return "une question « fill » doit contenir du code avec des trous ___.";
                    break;
                }
                var blanks = q.Code.Split(FillQuestion.Blank).Length - 1;
                if (blanks == 0)
                    yield return "aucun trou ___ dans le code.";
                if (blanks != q.Answers.Count)
                    yield return $"{blanks} trou(s) dans le code mais {q.Answers.Count} réponse(s).";
                if (q.Code.Contains(FillQuestion.Blank + "_"))
                    yield return "un trou ___ est collé à un « _ » : le découpage serait ambigu, mettre le « _ » dans la réponse.";
                if (q.Distractors.Any(q.Answers.Contains))
                    yield return "un leurre est identique à une réponse.";
                break;

            case OrderQuestion q:
                if (q.Lines.Count < 2 || q.Lines.Distinct().Count() < 2)
                    yield return "il faut au moins 2 lignes différentes à ordonner.";
                break;
        }
    }

    /// <summary>Découpe un renvoi « categorie/noeud ».</summary>
    public static (string CategoryId, string NodeId) SplitLink(string link)
    {
        var parts = link.Split('/');
        return parts.Length == 2 ? (parts[0], parts[1]) : throw new FormatException($"Renvoi invalide « {link} » (attendu : categorie/noeud).");
    }

    private static IEnumerable<string> ValidateLink(SpecializationNode node, string link, IReadOnlyList<Category> categories)
    {
        if (node.ChildNodes.Count > 0 || node.Track.Count > 0)
            yield return "un nœud de renvoi ou une porte (linkTo) ne peut avoir ni enfants ni parcours propre.";

        if (node.IsDoor)
        {
            if (categories.All(c => c.Id != link))
                yield return $"la porte mène à une catégorie inconnue ({link}).";
            yield break;
        }

        var parts = link.Split('/');
        if (parts.Length != 2)
        {
            yield return $"linkTo « {link} » invalide (attendu : categorie/noeud).";
            yield break;
        }

        var target = categories.FirstOrDefault(c => c.Id == parts[0])?.Specializations?.AllNodes.FirstOrDefault(n => n.Id == parts[1]);
        if (target is null)
            yield return $"linkTo « {link} » ne correspond à aucune spécialisation.";
        else if (target.IsLink)
            yield return $"linkTo « {link} » pointe vers un autre renvoi.";
    }

    private static IEnumerable<string> ValidateChoices(IReadOnlyList<string> choices)
    {
        if (choices.Count < 2)
            yield return "au moins 2 choix sont requis.";
        else if (choices.Distinct().Count() != choices.Count)
            yield return "choix en double.";
    }
}
