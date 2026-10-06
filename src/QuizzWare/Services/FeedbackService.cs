using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using QuizzWare.Core.Models;

namespace QuizzWare.Services;

public enum FeedbackKind
{
    Correct,
    Wrong,
    Complete,
}

/// <summary>Retours sensoriels d'une réponse : son et vibration, selon les réglages de l'utilisateur.</summary>
public sealed class FeedbackService(IAudioManager audio, ILogger<FeedbackService> logger)
{
    private readonly Dictionary<FeedbackKind, IAudioPlayer> _players = [];

    public async Task PlayAsync(FeedbackKind kind, UserSettings settings)
    {
        if (settings.HapticsEnabled)
            Vibrate(kind);
        if (settings.SoundEnabled)
            await PlaySoundAsync(kind);
    }

    private void Vibrate(FeedbackKind kind)
    {
        try
        {
            switch (kind)
            {
                case FeedbackKind.Correct:
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                    break;
                case FeedbackKind.Wrong:
                    // Une vibration plus longue, perceptible même téléphone en main.
                    if (Vibration.Default.IsSupported)
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(180));
                    break;
                case FeedbackKind.Complete:
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Plateforme sans vibreur (PC) ou permission absente : le retour visuel suffit.
            logger.LogDebug(ex, "Vibration indisponible");
        }
    }

    private async Task PlaySoundAsync(FeedbackKind kind)
    {
        try
        {
            if (!_players.TryGetValue(kind, out var player))
            {
                var file = kind switch
                {
                    FeedbackKind.Correct => "sons/correct.wav",
                    FeedbackKind.Wrong => "sons/wrong.wav",
                    _ => "sons/complete.wav",
                };
                player = audio.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(file));
                _players[kind] = player;
            }

            if (player.IsPlaying)
                player.Stop();
            player.Play();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lecture du son {Kind} impossible", kind);
        }
    }
}
