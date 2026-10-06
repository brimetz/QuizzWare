using QuizzWare.Core.Content;
using QuizzWare.Core.Leveling;
using QuizzWare.Core.Models;

namespace QuizzWare.Core.Quiz;

public sealed class QuizBuilder(Random random)
{
    public const int PracticeQuestionCount = 5;

    /// <summary>Quizz de fin de leçon : toutes les questions de la leçon, dans un ordre aléatoire.</summary>
    public QuizSession ForLesson(Category category, Lesson lesson) =>
        new(category.Id, lesson.Id, Shuffle(lesson.Questions ?? []).Select(Prepare).ToList(), Prepare);

    /// <summary>Défi entretien : l'ordre est conservé pour que les cas pratiques s'enchaînent.</summary>
    public QuizSession ForChallenge(Category category, Challenge challenge) =>
        new(category.Id, challenge.Id, (challenge.Questions ?? []).Select(Prepare).ToList(), Prepare);

    /// <summary>
    /// Entraînement : questions piochées dans les leçons déjà accessibles, filtrées selon le niveau
    /// pour que la difficulté augmente avec la progression.
    /// </summary>
    public QuizSession ForPractice(Category category, int level, IEnumerable<Lesson> lessons, int count = PracticeQuestionCount)
    {
        var all = lessons.SelectMany(l => l.Questions ?? []).ToList();
        if (all.Count == 0)
            all = category.MainLessons.FirstOrDefault(l => !l.IsPlanned)?.Questions?.ToList()
                ?? throw new InvalidOperationException($"La catégorie {category.Id} n'a encore aucune leçon jouable.");

        var maxDifficulty = LevelSystem.MaxQuestionDifficulty(level);
        var eligible = all.Where(q => q.Difficulty <= maxDifficulty).ToList();

        // Pas assez de questions au niveau actuel : on complète avec les plus faciles restantes.
        if (eligible.Count < count)
            eligible.AddRange(all.Except(eligible).OrderBy(q => q.Difficulty).Take(count - eligible.Count));

        var picked = Shuffle(eligible).Take(count).Select(Prepare).ToList();
        return new QuizSession(category.Id, unitId: null, picked, Prepare);
    }

    /// <summary>Révision espacée : les questions dues, toutes catégories confondues, dans un ordre aléatoire.</summary>
    public QuizSession ForReview(IEnumerable<Question> dueQuestions) =>
        new(categoryId: null, unitId: null, Shuffle(dueQuestions).Select(Prepare).ToList(), Prepare);

    /// <summary>
    /// Test de niveau : <see cref="PlacementTest.QuestionsPerChapter"/> questions par chapitre jouable du tronc commun,
    /// plutôt difficiles et tirées de leçons différentes, chapitre après chapitre.
    /// </summary>
    public PlacementTest ForPlacement(Category category)
    {
        var chapters = category.Chapters.Where(c => c.Lessons.Any(l => !l.IsPlanned)).ToList();
        if (chapters.Count == 0)
            throw new InvalidOperationException($"La catégorie {category.Id} n'a encore aucun chapitre jouable.");

        var questions = new List<QuizQuestion>();
        var chapterOfQuestion = new List<int>();
        for (var i = 0; i < chapters.Count; i++)
        {
            // Une question par leçon d'abord (les plus difficiles en priorité), pour couvrir tout le chapitre.
            var picked = Shuffle(chapters[i].Lessons.Where(l => !l.IsPlanned))
                .Select(l => Shuffle(l.Questions!.Where(IsStandalone)).OrderByDescending(q => Math.Min(q.Difficulty, 2)).FirstOrDefault())
                .OfType<Question>()
                .Take(PlacementTest.QuestionsPerChapter)
                .ToList();
            if (picked.Count < PlacementTest.QuestionsPerChapter)
                picked.AddRange(Shuffle(chapters[i].Lessons.SelectMany(l => l.Questions ?? []).Where(IsStandalone).Except(picked))
                    .Take(PlacementTest.QuestionsPerChapter - picked.Count));

            foreach (var question in picked)
            {
                questions.Add(Prepare(question));
                chapterOfQuestion.Add(i);
            }
        }

        return new PlacementTest(category, chapters, questions, chapterOfQuestion);
    }

    /// <summary>
    /// Simulation d'entretien : questions du tronc commun (leçons et défis) au niveau visé, réparties entre les chapitres.
    /// Pas de reprise des erreurs, comme dans un vrai entretien.
    /// </summary>
    public QuizSession ForInterview(Category category, InterviewLevel level)
    {
        var pool = InterviewPool(category);
        if (pool.Count < level.QuestionCount)
            throw new InvalidOperationException($"La catégorie {category.Id} n'a pas assez de questions pour un entretien {level.Label}.");

        // Au niveau visé d'abord ; s'il en manque, les plus proches du niveau.
        static int Distance(int d, InterviewLevel l) => d < l.MinDifficulty ? l.MinDifficulty - d : d > l.MaxDifficulty ? d - l.MaxDifficulty : 0;
        var ranked = Shuffle(pool).OrderBy(x => Distance(x.Question.Difficulty, level)).ToList();
        var eligible = ranked.Where(x => Distance(x.Question.Difficulty, level) == 0).ToList();
        if (eligible.Count < level.QuestionCount)
            eligible = ranked.Take(Math.Max(level.QuestionCount, eligible.Count)).ToList();

        // Répartition entre les chapitres : on pioche à tour de rôle dans chacun.
        var byChapter = eligible.GroupBy(x => x.Chapter).Select(g => new Queue<Question>(g.Select(x => x.Question))).ToList();
        var picked = new List<Question>();
        while (picked.Count < level.QuestionCount && byChapter.Any(q => q.Count > 0))
            foreach (var queue in Shuffle(byChapter).Where(q => q.Count > 0))
            {
                if (picked.Count == level.QuestionCount)
                    break;
                picked.Add(queue.Dequeue());
            }

        return new QuizSession(category.Id, unitId: null, Shuffle(picked).Select(Prepare).ToList());
    }

    /// <summary>Le mode entretien n'est proposé que si le tronc commun a assez de questions pour ce niveau.</summary>
    public static bool CanInterview(Category category, InterviewLevel level) => InterviewPool(category).Count >= level.QuestionCount;

    private static List<(Chapter Chapter, Question Question)> InterviewPool(Category category) =>
        category.Chapters
            .SelectMany(c => c.Lessons.Cast<IQuizUnit>().Concat(c.Challenge is { } d ? [d] : []).Select(u => (Chapter: c, Unit: u)))
            .SelectMany(x => x.Unit.Questions.Where(IsStandalone).Select(q => (x.Chapter, Question: q)))
            .ToList();

    /// <summary>
    /// Une question qui se comprend seule. Les étapes 2/3 et 3/3 d'un cas pratique, ou une question qui renvoie au code
    /// de la leçon, n'ont pas de sens tirées au hasard.
    /// </summary>
    public static bool IsStandalone(Question question) =>
        !question.Prompt.StartsWith("Cas pratique", StringComparison.Ordinal)
        && !(question.Prompt.StartsWith("Avec ", StringComparison.Ordinal)
             && (question.Prompt.Contains("de la leçon", StringComparison.Ordinal)
                 || question.Prompt.Contains("précédente", StringComparison.Ordinal)));

    public QuizQuestion Prepare(Question question) => question switch
    {
        ChoiceQuestion q => PrepareChoice(q),
        MultiChoiceQuestion q => PrepareMulti(q),
        BugQuestion q => new BugQuizQuestion(q, CodeText.SplitLines(q.Code!), q.BugLines.Select(n => n - 1).ToHashSet()),
        FillQuestion q => new FillQuizQuestion(q, q.Code!.Split(FillQuestion.Blank), Shuffle(q.Answers.Concat(q.Distractors)), q.Answers),
        OrderQuestion q => new OrderQuizQuestion(q, ShuffleUntilDifferent(q.Lines), q.Lines),
        _ => throw new NotSupportedException($"Type de question non géré : {question.GetType().Name}"),
    };

    private ChoiceQuizQuestion PrepareChoice(ChoiceQuestion question)
    {
        var order = ShuffledIndices(question.Choices.Count);
        return new ChoiceQuizQuestion(question, order.Select(i => question.Choices[i]).ToList(), Array.IndexOf(order, question.AnswerIndex));
    }

    private MultiChoiceQuizQuestion PrepareMulti(MultiChoiceQuestion question)
    {
        var order = ShuffledIndices(question.Choices.Count);
        var correct = question.AnswerIndices.Select(i => Array.IndexOf(order, i)).ToHashSet();
        return new MultiChoiceQuizQuestion(question, order.Select(i => question.Choices[i]).ToList(), correct);
    }

    private int[] ShuffledIndices(int count)
    {
        var order = Enumerable.Range(0, count).ToArray();
        random.Shuffle(order);
        return order;
    }

    /// <summary>Mélange en évitant de présenter les lignes déjà dans le bon ordre.</summary>
    private string[] ShuffleUntilDifferent(IReadOnlyList<string> lines)
    {
        var shuffled = lines.ToArray();
        if (lines.Distinct().Count() < 2)
            return shuffled;

        do
        {
            random.Shuffle(shuffled);
        } while (shuffled.SequenceEqual(lines));
        return shuffled;
    }

    private T[] Shuffle<T>(IEnumerable<T> items)
    {
        var array = items.ToArray();
        random.Shuffle(array);
        return array;
    }
}
