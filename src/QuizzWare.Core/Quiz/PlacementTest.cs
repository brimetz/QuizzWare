using QuizzWare.Core.Models;

namespace QuizzWare.Core.Quiz;

/// <summary>
/// Test de niveau : quelques questions par chapitre du tronc commun, dans l'ordre du parcours.
/// Un chapitre est acquis si toutes ses questions sont réussies ; le test s'arrête au premier chapitre raté.
/// Les chapitres acquis sont validés d'un coup, ce qui évite à un développeur expérimenté de refaire les bases.
/// </summary>
public sealed class PlacementTest
{
    public const int QuestionsPerChapter = 2;

    private readonly IReadOnlyList<int> _chapterOfQuestion;

    public PlacementTest(Category category, IReadOnlyList<Chapter> chapters, IReadOnlyList<QuizQuestion> questions, IReadOnlyList<int> chapterOfQuestion)
    {
        if (questions.Count != chapterOfQuestion.Count)
            throw new ArgumentException("Chaque question doit être rattachée à un chapitre.", nameof(chapterOfQuestion));

        Category = category;
        Chapters = chapters;
        _chapterOfQuestion = chapterOfQuestion;
        Session = new QuizSession(category.Id, unitId: null, questions); // pas de reprise des erreurs : c'est un examen
    }

    public Category Category { get; }

    /// <summary>Les chapitres testés, dans l'ordre du parcours.</summary>
    public IReadOnlyList<Chapter> Chapters { get; }

    public QuizSession Session { get; }

    /// <summary>Chapitre de la question courante.</summary>
    public Chapter CurrentChapter => Chapters[_chapterOfQuestion[Session.CurrentIndex]];

    public int CurrentChapterNumber => _chapterOfQuestion[Session.CurrentIndex] + 1;

    /// <summary>Le test est fini : plus de question, ou le chapitre qui vient d'être terminé est raté.</summary>
    public bool IsFinished
    {
        get
        {
            if (!Session.IsCurrentAnswered)
                return false;
            if (!Session.HasNext)
                return true;

            var index = Session.CurrentIndex;
            var chapter = _chapterOfQuestion[index];
            var chapterDone = _chapterOfQuestion[index + 1] != chapter;
            return chapterDone && !IsChapterPassed(chapter);
        }
    }

    /// <summary>Nombre de chapitres acquis, depuis le début du parcours et sans interruption.</summary>
    public int PassedChapters
    {
        get
        {
            var passed = 0;
            while (passed < Chapters.Count && IsChapterPassed(passed))
                passed++;
            return passed;
        }
    }

    private bool IsChapterPassed(int chapter)
    {
        var any = false;
        for (var i = 0; i < _chapterOfQuestion.Count; i++)
        {
            if (_chapterOfQuestion[i] != chapter)
                continue;
            if (Session.IsCorrectAt(i) != true)
                return false;
            any = true;
        }
        return any;
    }
}
