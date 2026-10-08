using QuizzWare.Core.Diagnostics;
using QuizzWare.Core.Models;
using QuizzWare.Services;

namespace QuizzWare.App.Tests;

/// <summary>Préférences en mémoire, à la place du stockage de l'appareil.</summary>
internal sealed class FakePreferences : IPreferences
{
    public Dictionary<string, object?> Values { get; } = [];

    public bool ContainsKey(string key, string? sharedName = null) => Values.ContainsKey(key);
    public void Remove(string key, string? sharedName = null) => Values.Remove(key);
    public void Clear(string? sharedName = null) => Values.Clear();
    public void Set<T>(string key, T value, string? sharedName = null) => Values[key] = value;
    public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
        Values.TryGetValue(key, out var v) && v is T t ? t : defaultValue;
}

internal sealed class FakeDevice : IDeviceDescriber
{
    public DeviceDescription Describe() => new("1.2.0 (7)", "Android", "15", "Google Pixel 8");
}

internal sealed class FakeBrowser : IBrowserLauncher
{
    public List<Uri> Opened { get; } = [];

    public Task OpenAsync(Uri uri)
    {
        Opened.Add(uri);
        return Task.CompletedTask;
    }
}

internal sealed class NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }
}

internal static class Questions
{
    public static ChoiceQuestion Choice(string? code = null) => new()
    {
        Id = "q-choix", Prompt = "Qu'affiche ce code ?", Explanation = "Parce que.", Code = code,
        Choices = ["A", "B", "C"], AnswerIndex = 1,
    };

    public static BugQuestion Bug() => new()
    {
        Id = "q-bug", Prompt = "Trouve le bug", Explanation = "Parce que.",
        Code = "int a = 1;\nint b = a / 0;\nreturn b;", BugLines = [2], Language = "cpp",
    };
}

/// <summary>Un fichier temporaire supprimé à la fin du test.</summary>
internal sealed class TempFile : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"quizzware-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(Path))
            File.Delete(Path);
    }
}
