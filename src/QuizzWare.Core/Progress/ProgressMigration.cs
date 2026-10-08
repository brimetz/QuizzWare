using QuizzWare.Core.Models;

namespace QuizzWare.Core.Progress;

/// <summary>
/// Adapte une sauvegarde au contenu actuel, pour qu'une mise à jour du contenu ne fasse jamais perdre de progression.
/// Les identifiants (catégories, leçons, défis, questions) servent de clés dans la sauvegarde : quand l'un d'eux est
/// renommé, la table <c>Progress/renommages.json</c> indique son nouveau nom, et la progression suit.
/// </summary>
public static class ProgressMigration
{
    /// <summary>Applique les renommages ; renvoie true si la sauvegarde a changé.</summary>
    public static bool Apply(UserProgress progress, IReadOnlyDictionary<string, string?> renames)
    {
        if (renames.Count == 0)
            return false;

        var changed = false;

        foreach (var oldId in progress.Categories.Keys.Where(renames.ContainsKey).ToList())
        {
            var category = progress.Categories[oldId];
            progress.Categories.Remove(oldId);
            changed = true;
            if (Resolve(oldId, renames) is { } newId)
                progress.Categories[newId] = progress.Categories.TryGetValue(newId, out var existing) ? Merge(existing, category) : category;
        }

        foreach (var category in progress.Categories.Values)
        {
            foreach (var oldId in category.CompletedLessons.Where(renames.ContainsKey).ToList())
            {
                category.CompletedLessons.Remove(oldId);
                if (Resolve(oldId, renames) is { } newId)
                    category.CompletedLessons.Add(newId);
                changed = true;
            }

            foreach (var oldId in category.BestScores.Keys.Where(renames.ContainsKey).ToList())
            {
                var score = category.BestScores[oldId];
                category.BestScores.Remove(oldId);
                if (Resolve(oldId, renames) is { } newId)
                    category.BestScores[newId] = Math.Max(score, category.BestScores.GetValueOrDefault(newId));
                changed = true;
            }
        }

        foreach (var oldId in progress.Reviews.Keys.Where(renames.ContainsKey).ToList())
        {
            var card = progress.Reviews[oldId];
            progress.Reviews.Remove(oldId);
            // Si la question a déjà une fiche sous son nouveau nom, c'est la plus récente : on la garde.
            if (Resolve(oldId, renames) is { } newId && !progress.Reviews.ContainsKey(newId))
                progress.Reviews[newId] = card;
            changed = true;
        }

        if (progress.Settings.Interests.Any(renames.ContainsKey))
        {
            progress.Settings.Interests = progress.Settings.Interests
                .Select(id => renames.ContainsKey(id) ? Resolve(id, renames) : id)
                .OfType<string>()
                .Distinct()
                .ToList();
            changed = true;
        }

        return changed;
    }

    /// <summary>Suit les renommages successifs (a → b, puis b → c) ; null si l'élément a été retiré.</summary>
    public static string? Resolve(string id, IReadOnlyDictionary<string, string?> renames)
    {
        var current = id;
        for (var hops = 0; renames.TryGetValue(current, out var next); hops++)
        {
            if (next is null)
                return null;
            if (hops > renames.Count)
                throw new InvalidDataException($"Renommages en boucle à partir de « {id} ».");
            current = next;
        }
        return current;
    }

    private static CategoryProgress Merge(CategoryProgress a, CategoryProgress b)
    {
        a.TotalXp += b.TotalXp;
        a.QuizzesCompleted += b.QuizzesCompleted;
        a.QuestionsAnswered += b.QuestionsAnswered;
        a.CorrectAnswers += b.CorrectAnswers;
        a.CompletedLessons.UnionWith(b.CompletedLessons);
        foreach (var (unit, score) in b.BestScores)
            a.BestScores[unit] = Math.Max(score, a.BestScores.GetValueOrDefault(unit));
        return a;
    }
}
