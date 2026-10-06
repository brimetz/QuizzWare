using QuizzWare.Core.Models;

namespace QuizzWare.Core.Progress;

/// <summary>
/// Révision espacée par boîtes de Leitner. Chaque question répondue a une fiche :
/// une bonne réponse (à échéance) la fait monter d'une boîte et espace la prochaine révision,
/// une erreur la renvoie en boîte 1, à revoir dès le lendemain.
/// </summary>
public static class SpacedRepetition
{
    /// <summary>Intervalle (en jours) avant la prochaine révision, pour les boîtes 1 à 6.</summary>
    public static readonly IReadOnlyList<int> IntervalsDays = [1, 3, 7, 14, 30, 60];

    public static int MaxBox => IntervalsDays.Count;

    /// <summary>Boîte d'une question réussie du premier coup : on la reverra dans quelques jours.</summary>
    public const int FirstSuccessBox = 2;

    public static ReviewCard Update(ReviewCard? card, bool isCorrect, DateOnly today)
    {
        if (!isCorrect)
            return new ReviewCard { Box = 1, Due = today.AddDays(IntervalsDays[0]), LastSeen = today, Lapses = (card?.Lapses ?? 0) + 1 };

        if (card is null)
            return Schedule(new ReviewCard { Box = FirstSuccessBox }, today);

        // Réussir une question avant son échéance ne la fait pas monter : on ne récompense pas le bachotage.
        if (card.Due > today)
        {
            card.LastSeen = today;
            return card;
        }

        card.Box = Math.Min(card.Box + 1, MaxBox);
        return Schedule(card, today);
    }

    public static bool IsMastered(ReviewCard card) => card.Box == MaxBox;

    private static ReviewCard Schedule(ReviewCard card, DateOnly today)
    {
        card.Due = today.AddDays(IntervalsDays[card.Box - 1]);
        card.LastSeen = today;
        return card;
    }
}
