namespace QuizzWare.Core.Content;

public static class CodeText
{
    public static IReadOnlyList<string> SplitLines(string code) =>
        code.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
}
