using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

public class QuestionTypeTests
{
    private readonly QuizBuilder _builder = new(new Random(7));

    [Fact]
    public void Multi_RequiresExactlyTheCorrectSet()
    {
        var question = new MultiChoiceQuestion
        {
            Id = "m", Prompt = "?", Explanation = "!",
            Choices = ["A", "B", "C", "D"],
            AnswerIndices = [0, 2],
        };
        var prepared = (MultiChoiceQuizQuestion)_builder.Prepare(question);

        var correctTexts = prepared.CorrectIndices.Select(i => prepared.Choices[i]).Order();
        Assert.Equal(["A", "C"], correctTexts);

        Assert.True(prepared.Grade(new MultiChoiceAnswer(prepared.CorrectIndices.ToList())));
        Assert.False(prepared.Grade(new MultiChoiceAnswer([prepared.CorrectIndices.First()])));
        Assert.False(prepared.Grade(new MultiChoiceAnswer([0, 1, 2, 3])));
    }

    [Fact]
    public void Bug_AcceptsAnyListedLine_OneBased()
    {
        var question = new BugQuestion
        {
            Id = "b", Prompt = "?", Explanation = "!",
            Code = "int a;\nint b = a;\nreturn b;",
            BugLines = [1, 2],
        };
        var prepared = (BugQuizQuestion)_builder.Prepare(question);

        Assert.Equal(3, prepared.Lines.Count);
        Assert.True(prepared.Grade(new LineAnswer(0)));
        Assert.True(prepared.Grade(new LineAnswer(1)));
        Assert.False(prepared.Grade(new LineAnswer(2)));
    }

    [Fact]
    public void Fill_SplitsAroundBlanks_AndGradesInOrder()
    {
        var question = new FillQuestion
        {
            Id = "f", Prompt = "?", Explanation = "!",
            Code = "for (___ auto___ x : v)",
            Answers = ["const", "&"],
            Distractors = ["*"],
        };
        var prepared = (FillQuizQuestion)_builder.Prepare(question);

        Assert.Equal(["for (", " auto", " x : v)"], prepared.Segments);
        Assert.Equal(["&", "*", "const"], prepared.Tokens.Order(StringComparer.Ordinal));
        Assert.True(prepared.Grade(new FillAnswer(["const", "&"])));
        Assert.False(prepared.Grade(new FillAnswer(["&", "const"])));
        Assert.Equal("for (const auto& x : v)", prepared.Compose(["const", "&"]));
        Assert.Equal("for (const auto___ x : v)", prepared.Compose(["const", null]));
    }

    [Fact]
    public void Order_IsNeverPresentedAlreadySorted_AndGradesByText()
    {
        var question = new OrderQuestion
        {
            Id = "o", Prompt = "?", Explanation = "!",
            Lines = ["a", "b", "c"],
        };

        for (var seed = 0; seed < 50; seed++)
        {
            var prepared = (OrderQuizQuestion)new QuizBuilder(new Random(seed)).Prepare(question);
            Assert.NotEqual(question.Lines, prepared.ShuffledLines);
            Assert.True(prepared.Grade(new OrderAnswer(["a", "b", "c"])));
            Assert.False(prepared.Grade(new OrderAnswer(["b", "a", "c"])));
        }
    }

    [Fact]
    public void Json_DeserializesEveryQuestionType()
    {
        var category = ContentRepository.ParseCategory("""
            {
              "id": "x", "order": 1, "name": "X", "description": "", "icon": "X", "color": "#fff", "language": "cpp",
              "chapters": [{
                "id": "ch", "title": "C", "summary": "",
                "lessons": [{
                  "id": "l", "title": "L", "summary": "", "difficulty": 1,
                  "sections": [{ "title": "S", "body": "B" }],
                  "questions": [
                    { "type": "choice", "id": "q1", "prompt": "P", "choices": ["a", "b"], "answerIndex": 1, "explanation": "E" },
                    { "id": "q2", "type": "multi", "prompt": "P", "choices": ["a", "b"], "answerIndices": [0, 1], "explanation": "E" },
                    { "type": "bug", "id": "q3", "prompt": "P", "code": "a\nb", "bugLines": [2], "explanation": "E", "difficulty": 3 },
                    { "type": "fill", "id": "q4", "prompt": "P", "code": "___ x", "answers": ["int"], "explanation": "E" },
                    { "type": "order", "id": "q5", "prompt": "P", "lines": ["a", "b"], "explanation": "E" }
                  ]
                }],
                "challenge": {
                  "id": "d", "title": "D", "summary": "",
                  "questions": [{ "type": "choice", "id": "dq", "prompt": "P", "choices": ["a", "b"], "answerIndex": 0, "explanation": "E" }]
                }
              }]
            }
            """);

        var questions = category.Chapters[0].Lessons[0].Questions;
        Assert.IsType<ChoiceQuestion>(questions[0]);
        Assert.IsType<MultiChoiceQuestion>(questions[1]); // "type" pas en premier : accepté
        Assert.Equal(3, Assert.IsType<BugQuestion>(questions[2]).Difficulty);
        Assert.Empty(Assert.IsType<FillQuestion>(questions[3]).Distractors);
        Assert.IsType<OrderQuestion>(questions[4]);
        Assert.Equal("cpp", category.Language);
        Assert.Equal("d", category.Chapters[0].Challenge!.Id);
    }

    [Theory]
    [InlineData("""{ "type": "bug", "id": "q", "prompt": "P", "code": "a\nb", "bugLines": [3], "explanation": "E" }""", "bugLines hors limites")]
    [InlineData("""{ "type": "fill", "id": "q", "prompt": "P", "code": "___ ___", "answers": ["int"], "explanation": "E" }""", "2 trou(s)")]
    [InlineData("""{ "type": "fill", "id": "q", "prompt": "P", "code": "___", "answers": ["int"], "distractors": ["int"], "explanation": "E" }""", "leurre")]
    [InlineData("""{ "type": "multi", "id": "q", "prompt": "P", "choices": ["a", "b"], "answerIndices": [], "explanation": "E" }""", "answerIndices")]
    [InlineData("""{ "type": "order", "id": "q", "prompt": "P", "lines": ["a", "a"], "explanation": "E" }""", "lignes différentes")]
    public void Validator_RejectsInvalidQuestionsOfEachType(string questionJson, string expectedError)
    {
        var json = $$"""
            {
              "id": "x", "order": 1, "name": "X", "description": "", "icon": "X", "color": "#fff",
              "chapters": [{ "id": "ch", "title": "C", "summary": "",
                "lessons": [{ "id": "l", "title": "L", "summary": "", "difficulty": 1,
                  "sections": [{ "title": "S", "body": "B" }],
                  "questions": [{{questionJson}}] }] }]
            }
            """;

        var ex = Assert.Throws<InvalidDataException>(() => new ContentRepository([ContentRepository.ParseCategory(json)]));
        Assert.Contains(expectedError, ex.Message);
    }
}
