using QuizzWare.Core.Models;

namespace QuizzWare.Core.Progress;

public interface IProgressStore
{
    Task<UserProgress> LoadAsync();
    Task SaveAsync(UserProgress progress);
}
