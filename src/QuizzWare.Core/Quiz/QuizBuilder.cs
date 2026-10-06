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
