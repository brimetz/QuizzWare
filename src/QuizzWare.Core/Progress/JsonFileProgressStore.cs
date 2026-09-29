using System.Text.Json;
using QuizzWare.Core.Models;

namespace QuizzWare.Core.Progress;

/// <summary>Sauvegarde la progression dans un fichier JSON local.</summary>
public sealed class JsonFileProgressStore(string filePath) : IProgressStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<UserProgress> LoadAsync()
    {
        if (!File.Exists(filePath))
            return new UserProgress();

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<UserProgress>(stream, JsonOptions) ?? new UserProgress();
        }
        catch (JsonException)
        {
            // Fichier corrompu : on le met de côté plutôt que de bloquer l'application.
            File.Move(filePath, filePath + ".corrupted", overwrite: true);
            return new UserProgress();
        }
    }

    public async Task SaveAsync(UserProgress progress)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

        // Écriture dans un fichier temporaire puis remplacement, pour ne jamais laisser un fichier à moitié écrit.
        var tempPath = filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, progress, JsonOptions);
        File.Move(tempPath, filePath, overwrite: true);
    }
}
