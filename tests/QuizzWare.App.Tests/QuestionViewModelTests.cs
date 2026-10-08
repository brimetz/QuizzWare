using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels;
using QuizzWare.ViewModels.Questions;

namespace QuizzWare.App.Tests;

/// <summary>Le déroulé de chaque type de question : composer la réponse, la vérifier, afficher la correction.</summary>
public class QuestionViewModelTests
{
    [Fact]
    public void Create_PicksTheViewModelMatchingTheQuestionType()
    {
        var choice = new ChoiceQuizQuestion(Questions.Choice(), ["A", "B"], 1);
        var bug = new BugQuizQuestion(Questions.Bug(), ["int a = 1;", "int b = a / 0;", "return b;"], new HashSet<int> { 1 });

        Assert.IsType<OptionsQuestionViewModel>(QuestionViewModel.Create(choice, null));
        Assert.IsType<BugQuestionViewModel>(QuestionViewModel.Create(bug, null));
    }

    [Fact]
    public void SingleChoice_KeepsOnlyTheLastTappedOption()
    {
        var vm = new OptionsQuestionViewModel(new ChoiceQuizQuestion(Questions.Choice(), ["A", "B", "C"], 1), null);
        Assert.False(vm.CanSubmit);

        vm.Options[0].TapCommand.Execute(null);
        vm.Options[2].TapCommand.Execute(null);

        Assert.True(vm.CanSubmit);
        Assert.Equal(new ChoiceAnswer(2), vm.BuildAnswer());
        Assert.False(vm.Options[0].IsSelected);
    }

    [Fact]
    public void Correction_MarksAnswersWithSymbols_NotOnlyColors()
    {
        var vm = new OptionsQuestionViewModel(new ChoiceQuizQuestion(Questions.Choice(), ["A", "B", "C"], 1), null);
        vm.Options[0].TapCommand.Execute(null);

        vm.ShowCorrection(isCorrect: false);

        Assert.Equal("✗", vm.Options[0].Marker);
        Assert.Equal("✓", vm.Options[1].Marker);
        Assert.Equal("C", vm.Options[2].Marker);
        Assert.Equal(Palette.Error, vm.Options[0].StrokeColor);
        Assert.Equal(Palette.Success, vm.Options[1].StrokeColor);
        Assert.True(vm.IsLocked);
    }

    [Fact]
    public void LockedQuestion_IgnoresFurtherTaps()
    {
        var vm = new OptionsQuestionViewModel(new ChoiceQuizQuestion(Questions.Choice(), ["A", "B", "C"], 1), null);
        vm.Options[1].TapCommand.Execute(null);
        vm.ShowCorrection(isCorrect: true);

        vm.Options[0].TapCommand.Execute(null);

        Assert.False(vm.Options[0].IsSelected);
        Assert.False(vm.CanSubmit);
    }

    [Fact]
    public void MultiChoice_TogglesEachOption()
    {
        var question = new MultiChoiceQuizQuestion(Questions.Choice(), ["A", "B", "C"], new HashSet<int> { 0, 2 });
        var vm = new OptionsQuestionViewModel(question, null);

        vm.Options[0].TapCommand.Execute(null);
        vm.Options[2].TapCommand.Execute(null);
        vm.Options[2].TapCommand.Execute(null);
        vm.Options[1].TapCommand.Execute(null);

        var answer = Assert.IsType<MultiChoiceAnswer>(vm.BuildAnswer());
        Assert.Equal([0, 1], answer.Indices.Order());
        Assert.Equal("☑", vm.Options[0].Marker);
        Assert.Equal("☐", vm.Options[2].Marker);
    }

    [Fact]
    public void Bug_SelectsATappedLine_AndHighlightsTheBuggyOnesAfterCorrection()
    {
        var question = new BugQuizQuestion(Questions.Bug(), ["int a = 1;", "int b = a / 0;", "return b;"], new HashSet<int> { 1 });
        var vm = new BugQuestionViewModel(question, "cpp");

        vm.Lines[2].TapCommand.Execute(null);
        Assert.Equal(new LineAnswer(2), vm.BuildAnswer());

        vm.ShowCorrection(question.Grade(vm.BuildAnswer()));

        Assert.Equal(Palette.ErrorSurface, vm.Lines[2].Background);
        Assert.Equal(Palette.SuccessSurface, vm.Lines[1].Background);
    }

    [Fact]
    public void Fill_PlacesTokensInOrder_AndUndoRemovesTheLastOne()
    {
        var source = new Core.Models.FillQuestion
        {
            Id = "q-trous", Prompt = "Complète", Explanation = "Parce que.", Code = "int ___ = ___;", Answers = ["x", "1"],
        };
        var question = new FillQuizQuestion(source, ["int ", " = ", ";"], ["1", "y", "x"], ["x", "1"]);
        var vm = new FillQuestionViewModel(question, "cpp");

        vm.Tokens[2].TapCommand.Execute(null); // x
        vm.Tokens[1].TapCommand.Execute(null); // y
        Assert.True(vm.CanSubmit);
        Assert.True(vm.Tokens[1].IsUsed);

        vm.UndoCommand.Execute(null);
        Assert.False(vm.Tokens[1].IsUsed);
        Assert.False(vm.CanSubmit);

        vm.Tokens[0].TapCommand.Execute(null); // 1
        Assert.Equal(["x", "1"], Assert.IsType<FillAnswer>(vm.BuildAnswer()).Tokens);
        Assert.Contains(vm.Lines[0].Text.Spans, s => s.Text == " x ");
    }

    [Fact]
    public void Order_MovesLinesBetweenPoolAndAnswer_KeepingThePoolOrder()
    {
        var source = new Core.Models.OrderQuestion
        {
            Id = "q-ordre", Prompt = "Remets dans l'ordre", Explanation = "Parce que.", Lines = ["a", "b", "c"],
        };
        var vm = new OrderQuestionViewModel(new OrderQuizQuestion(source, ["c", "a", "b"], ["a", "b", "c"]), "text");

        vm.Pool[1].TapCommand.Execute(null);           // a
        vm.Pool[0].TapCommand.Execute(null);           // c
        Assert.Equal(["a", "c"], vm.Answer.Select(l => l.Text));
        Assert.False(vm.CanSubmit);

        vm.Answer[0].TapCommand.Execute(null);         // a revient à sa place d'origine
        Assert.Equal(["a", "b"], vm.Pool.Select(l => l.Text));

        vm.Pool[0].TapCommand.Execute(null);
        vm.Pool[0].TapCommand.Execute(null);
        Assert.True(vm.CanSubmit);
        Assert.Equal(["c", "a", "b"], Assert.IsType<OrderAnswer>(vm.BuildAnswer()).Lines);

        vm.ShowCorrection(isCorrect: false);
        Assert.True(vm.ShowSolution);
        Assert.Equal(Palette.ErrorSurface, vm.Answer[0].Background);
    }
}
