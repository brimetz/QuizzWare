using System.Text.Json;

namespace QuizzWare.Core.Diagnostics;

/// <summary>
/// Journal local des erreurs (fichier JSON). Écrit de façon synchrone : au moment d'un plantage, il n'y a plus
/// le temps d'attendre une tâche. Ne lève jamais d'exception : un journal en panne ne doit pas aggraver un plantage.
/// </summary>
public sealed class ErrorLog(string filePath)
{
    /// <summary>Nombre de rapports conservés : les plus anciens sont oubliés.</summary>
    public const int Capacity = 20;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly Lock _lock = new();

    public void Append(ErrorReport report)
    {
        lock (_lock)
        {
            var reports = ReadAll();
            reports.Add(report);
            Write(reports.TakeLast(Capacity).ToList());
        }
    }

    public IReadOnlyList<ErrorReport> All()
    {
        lock (_lock)
            return ReadAll();
    }

    /// <summary>Plantages que l'utilisateur n'a encore ni envoyés ni écartés.</summary>
    public IReadOnlyList<ErrorReport> PendingCrashes() => All().Where(r => r.IsCrash && !r.Handled).ToList();

    public void MarkHandled(IEnumerable<string> ids)
    {
        var set = ids.ToHashSet();
        lock (_lock)
            Write(ReadAll().Select(r => set.Contains(r.Id) ? r with { Handled = true } : r).ToList());
    }

    public void Clear()
    {
        lock (_lock)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception)
            {
                // Rien à faire : au pire, le journal reste.
            }
        }
    }

    private List<ErrorReport> ReadAll()
    {
        try
        {
            return File.Exists(filePath)
                ? JsonSerializer.Deserialize<List<ErrorReport>>(File.ReadAllText(filePath), JsonOptions) ?? []
                : [];
        }
        catch (Exception)
        {
            return []; // Fichier illisible : on repart d'un journal vide.
        }
    }

    private void Write(List<ErrorReport> reports)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
            var temp = filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(reports, JsonOptions));
            File.Move(temp, filePath, overwrite: true);
        }
        catch (Exception)
        {
            // Disque plein, droits… : on renonce plutôt que de planter à nouveau.
        }
    }
}
