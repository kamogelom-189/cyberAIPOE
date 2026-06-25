using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows; 

namespace CyberBotWPF;

public static class VoiceService
{
    private const string WavRelativePath = "assets/greeting.wav";

    public static bool Play()
    {
        if (!OperatingSystem.IsWindows()) return false;

        string wavPath = Path.Combine(AppContext.BaseDirectory, WavRelativePath);
        
        // DEBUG CHECK: If the file is missing, tell us exactly where it's looking!
        if (!File.Exists(wavPath)) 
        {
            MessageBox.Show($"Audio file missing!\nLooking in: {wavPath}", "Audio Debug Info");
            return false;
        }

        try
        {
            Task.Run(() =>
            {
                try
                {
                    using var player = new System.Media.SoundPlayer(wavPath);
                    player.PlaySync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Playback error: {ex.Message}");
                }
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}