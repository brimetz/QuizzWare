using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;

namespace QuizzWare.ViewModels;

/// <summary>Construit l'affichage d'un parcours (tronc commun ou spécialisation) : chapitres, leçons et défis avec leur état.</summary>
public static class PathBuilder
{
    public static List<ChapterViewModel> Build(Category category, IReadOnlyList<Chapter> chapters, UserProgress progress, ProgressService progressService)
    {
        var stats = progress.Categories.GetValueOrDefault(category.Id);
        int? Best(string unitId) => stats is not null && stats.BestScores.TryGetValue(unitId, out var s) ? s : null;

        var result = new List<ChapterViewModel>();
        for (var c = 0; c < chapters.Count; c++)
        {
            var chapter = chapters[c];
            var states = chapter.Lessons.Select(l => progressService.GetLessonState(progress, category, l.Id)).ToList();

            var items = chapter.Lessons
                .Select((lesson, l) => UnitItemViewModel.ForLesson(category.Id, lesson, $"{c + 1}.{l + 1}", states[l], Best(lesson.Id)))
                .ToList();

            if (chapter.Challenge is { } challenge)
            {
                items.Add(UnitItemViewModel.ForChallenge(category.Id, challenge,
                    progressService.GetChallengeState(progress, category, chapter), Best(challenge.Id)));
            }

            var done = states.Count(s => s == UnitState.Completed);
            var playable = states.Count(s => s != UnitState.Planned);
            var progressText = playable == 0 ? "🕓 À venir" : $"{done}/{playable}";
            result.Add(new ChapterViewModel($"Chapitre {c + 1}", chapter.Title, chapter.Summary, progressText, items));
        }

        return result;
    }
}
