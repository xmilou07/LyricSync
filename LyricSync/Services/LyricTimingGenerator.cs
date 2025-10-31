using System;
using System.Linq;
using System.Text;
using TagLib;

namespace LyricSync.Services
{
    public class LyricTimingGenerator
    {
        public double? GetAudioDurationSeconds(string fullPath)
        {
            try
            {
                using var tfile = TagLib.File.Create(fullPath);
                return tfile.Properties.Duration.TotalSeconds;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateLrcFromLines(string lyrics, double durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(lyrics) || durationSeconds <= 0)
                return lyrics ?? string.Empty;

            var lines = lyrics.Replace("\r", "").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToArray();

            if (lines.Length == 0)
                return string.Empty;

            var usable = Math.Max(durationSeconds - 0.5, 0.5);
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                // Distribute lines across the duration
                var t = (i / (double)lines.Length) * usable;
                var minutes = (int)(t / 60);
                var seconds = (int)(t % 60);
                var centis = (int)((t - Math.Floor(t)) * 100);
                sb.AppendFormat("[{0:D2}:{1:D2}.{2:D2}]{3}\n", minutes, seconds, centis, lines[i]);
            }

            return sb.ToString();
        }
    }
}
