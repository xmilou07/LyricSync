using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LyricSync.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LyricSync.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LyricsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LyricsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/lyrics/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var song = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(s => s.Id == id);
            if (song == null)
                return NotFound();

            var content = song.Lyric?.Content?.Replace("\r", "") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
                return Ok(Array.Empty<object>());

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

            // regex to match [mm:ss.xx] timestamps (supports multiple timestamps per line)
            var tsRegex = new Regex(@"\[(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?\]", RegexOptions.Compiled);

            var results = new List<(double? Time, string Text)>();
            bool anyTimestamp = false;

            foreach (var line in lines)
            {
                var matches = tsRegex.Matches(line);
                if (matches.Count > 0)
                {
                    anyTimestamp = true;
                    // text after last timestamp
                    var lastMatch = matches[matches.Count - 1];
                    var text = line.Substring(lastMatch.Index + lastMatch.Length).Trim();
                    foreach (Match m in matches)
                    {
                        if (int.TryParse(m.Groups[1].Value, out var min) && int.TryParse(m.Groups[2].Value, out var sec))
                        {
                            var ms = 0;
                            if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var frac))
                            {
                                // convert fraction to milliseconds depending on digits
                                ms = frac;
                                if (m.Groups[3].Value.Length == 1) ms *= 100; // .5 -> 500ms
                                else if (m.Groups[3].Value.Length == 2) ms *= 10; // .50 -> 500ms
                            }

                            var totalSeconds = min * 60 + sec + ms / 1000.0;
                            results.Add((totalSeconds, string.IsNullOrEmpty(text) ? string.Empty : text));
                        }
                    }
                }
                else
                {
                    // no timestamp on this line; keep text with null time for now
                    results.Add((null, line));
                }
            }

            if (!anyTimestamp)
            {
                // return lines with null times; client will assign times based on audio.duration
                var payload = results.Select(r => new { time = (double?)null, text = r.Text });
                return Ok(payload);
            }

            // sort by time
            var ordered = results.OrderBy(r => r.Time).Select(r => new { time = r.Time, text = r.Text }).ToList();
            return Ok(ordered);
        }
    }
}
