using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace QuizzWare.ViewModels.Code;

/// <summary>
/// Coloration syntaxique volontairement simple, ligne par ligne : mots-clés, types, chaînes, nombres, commentaires.
/// Suffisant pour des extraits de quizz, sans dépendance externe.
/// </summary>
public static class SyntaxHighlighter
{
    private static readonly string[] CFamilyKeywords =
    [
        "if", "else", "for", "while", "do", "switch", "case", "default", "break", "continue", "return",
        "struct", "enum", "union", "typedef", "const", "static", "extern", "volatile", "sizeof", "goto",
    ];

    private static readonly Dictionary<string, HashSet<string>> Keywords = new()
    {
        ["c"] = [.. CFamilyKeywords, "NULL", "#include", "#define"],
        ["cpp"] =
        [
            .. CFamilyKeywords, "class", "public", "private", "protected", "virtual", "override", "final",
            "template", "typename", "namespace", "using", "new", "delete", "this", "nullptr", "true", "false",
            "constexpr", "noexcept", "auto", "mutable", "explicit", "operator", "friend", "try", "catch", "throw",
            "static_cast", "dynamic_cast", "const_cast", "reinterpret_cast", "decltype", "#include", "#define",
        ],
        ["csharp"] =
        [
            "if", "else", "for", "foreach", "in", "while", "do", "switch", "case", "default", "break", "continue",
            "return", "class", "record", "struct", "interface", "enum", "public", "private", "protected", "internal",
            "static", "readonly", "const", "new", "this", "null", "true", "false", "var", "async", "await", "using",
            "namespace", "virtual", "override", "abstract", "sealed", "is", "as", "try", "catch", "finally", "throw",
            "get", "set", "init", "yield", "out", "ref",
        ],
        ["python"] =
        [
            "def", "class", "return", "if", "elif", "else", "for", "while", "in", "not", "and", "or", "is", "None",
            "True", "False", "import", "from", "as", "with", "try", "except", "finally", "raise", "lambda", "yield",
            "async", "await", "pass", "break", "continue", "global", "nonlocal",
        ],
        ["sql"] =
        [
            "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "JOIN", "INNER", "LEFT", "RIGHT", "ON",
            "AS", "AND", "OR", "NOT", "NULL", "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "CREATE",
            "TABLE", "INDEX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "BEGIN", "COMMIT", "ROLLBACK", "COUNT",
            "DISTINCT", "LIMIT", "IN", "IS", "EXISTS", "UNIQUE",
        ],
        ["java"] =
        [
            "if", "else", "for", "while", "do", "switch", "case", "default", "break", "continue", "return",
            "class", "interface", "record", "enum", "extends", "implements", "public", "private", "protected",
            "static", "final", "abstract", "new", "this", "super", "null", "true", "false", "var", "try", "catch",
            "finally", "throw", "throws", "import", "package", "void", "int", "long", "boolean", "double", "char",
            "synchronized", "sealed", "permits",
        ],
        ["go"] =
        [
            "func", "package", "import", "var", "const", "type", "struct", "interface", "map", "chan", "go",
            "select", "defer", "return", "if", "else", "for", "range", "switch", "case", "default", "break",
            "continue", "nil", "true", "false", "error", "string", "int", "bool",
        ],
        ["rust"] =
        [
            "fn", "let", "mut", "const", "struct", "enum", "impl", "trait", "pub", "use", "mod", "match", "if",
            "else", "for", "while", "loop", "return", "self", "Self", "Some", "None", "Ok", "Err", "true", "false",
            "async", "await", "move", "ref", "where", "unsafe", "dyn",
        ],
        ["javascript"] =
        [
            "const", "let", "var", "function", "return", "if", "else", "for", "while", "new", "true", "false",
            "null", "undefined", "async", "await", "this", "class", "import", "export", "from",
        ],
        ["yaml"] =
        [
            "apiVersion", "kind", "metadata", "name", "namespace", "labels", "spec", "replicas", "selector",
            "matchLabels", "template", "containers", "image", "ports", "port", "targetPort", "containerPort",
            "env", "valueFrom", "resources", "requests", "limits", "readinessProbe", "livenessProbe",
            "startupProbe", "httpGet", "path", "true", "false",
        ],
        ["shell"] =
        [
            "FROM", "RUN", "CMD", "COPY", "WORKDIR", "ENTRYPOINT", "ENV", "EXPOSE", "ARG", "AS",
            "apiVersion", "kind", "metadata", "spec", "if", "then", "fi", "for", "do", "done", "echo",
        ],
    };

    private static readonly HashSet<string> CFamilyTypes =
    [
        "int", "long", "short", "char", "bool", "double", "float", "void", "unsigned", "signed", "size_t",
        "string", "vector", "array", "map", "unique_ptr", "shared_ptr", "weak_ptr", "std", "uint8_t",
        "int32_t", "int64_t", "uint32_t", "uint64_t", "object", "decimal", "Task", "List", "Dictionary",
    ];

    private static readonly ConcurrentDictionary<string, Regex> Tokenizers = new();

    /// <summary>
    /// Police à chasse fixe. À préciser sur chaque Span : sous Windows, les spans n'héritent pas de la police du Label.
    /// Garder synchronisé avec la ressource MonoFont de Styles.xaml.
    /// </summary>
    public static string MonoFont { get; } =
        DeviceInfo.Platform == DevicePlatform.Android ? "monospace"
        : DeviceInfo.Platform == DevicePlatform.iOS ? "Menlo"
        : "Consolas";

    public static Span CreateSpan(string text, Color color) =>
        new() { Text = text, TextColor = color, FontFamily = MonoFont, FontSize = Palette.CodeFontSize };

    public static FormattedString Highlight(string line, string? language)
    {
        var formatted = new FormattedString();
        Append(formatted, line, language);
        return formatted;
    }

    /// <summary>Ajoute les spans colorés de <paramref name="text"/> à <paramref name="target"/>.</summary>
    public static void Append(FormattedString target, string text, string? language)
    {
        if (text.Length == 0)
            return;

        language = language?.ToLowerInvariant();
        if (language is null || !Keywords.TryGetValue(language, out var keywords))
        {
            target.Spans.Add(CreateSpan(text, Palette.CodeText));
            return;
        }

        var caseInsensitive = language == "sql";
        var plain = new StringBuilder();

        void Flush()
        {
            if (plain.Length == 0)
                return;
            target.Spans.Add(CreateSpan(plain.ToString(), Palette.CodeText));
            plain.Clear();
        }

        void Add(string value, Color color)
        {
            Flush();
            target.Spans.Add(CreateSpan(value, color));
        }

        foreach (Match m in Tokenizer(language).Matches(text))
        {
            if (m.Groups["comment"].Success)
                Add(m.Value, Palette.CodeComment);
            else if (m.Groups["string"].Success)
                Add(m.Value, Palette.CodeString);
            else if (m.Groups["number"].Success)
                Add(m.Value, Palette.CodeNumber);
            else if (m.Groups["word"].Success && keywords.Contains(caseInsensitive ? m.Value.ToUpperInvariant() : m.Value))
                Add(m.Value, Palette.CodeKeyword);
            else if (m.Groups["word"].Success && language is "c" or "cpp" or "csharp" && CFamilyTypes.Contains(m.Value))
                Add(m.Value, Palette.CodeType);
            else
                plain.Append(m.Value);
        }

        Flush();
    }

    private static Regex Tokenizer(string language) => Tokenizers.GetOrAdd(language, lang =>
    {
        var comment = lang switch
        {
            "python" or "shell" or "yaml" => "#.*",
            "sql" => "--.*",
            _ => "//.*",
        };
        var directive = lang is "c" or "cpp" ? "#\\w+|" : "";
        return new Regex(
            $"(?<comment>{comment})|(?<string>\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*')|(?<number>\\b\\d[\\d'.]*[uUlLfF]*\\b)|(?<word>{directive}[A-Za-z_]\\w*)|(?<other>.)",
            RegexOptions.Compiled);
    });
}
