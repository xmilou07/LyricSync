using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LyricSync.Data;
using LyricSync.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LyricSync.Controllers
{
    public class SongsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SongsController> _logger;

        public SongsController(ApplicationDbContext context, ILogger<SongsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Songs
        [Authorize]
        public async Task<IActionResult> Index()
        {
            _logger.LogDebug("Loading songs index");
            return View(await _context.Song.Include(s => s.Lyric).ToListAsync());
        }

        // GET: Songs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null id");
                return NotFound();
            }

            var song = await _context.Song.FirstOrDefaultAsync(m => m.Id == id);
            if (song == null)
            {
                _logger.LogWarning("Details not found for id {Id}", id);
                return NotFound();
            }

            return View(song);
        }

        // GET: Songs/Create
        [Authorize]
        public IActionResult Create() => View();

        // POST: Songs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Title,Artist,Album,Lyrics,Genre,UploadedById")] Song song, IFormFile? mp3File, IFormFile? lyricsFile)
        {
            _logger.LogDebug("Create POST for Title='{Title}'", song?.Title);

            try
            {
                if (mp3File == null || mp3File.Length == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please upload an MP3 file.");
                    _logger.LogWarning("No MP3 file uploaded");
                    return View(song);
                }

                var contentType = mp3File.ContentType ?? string.Empty;
                if (!contentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase) &&
                    !mp3File.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Uploaded file does not appear to be an audio file.");
                    _logger.LogWarning("Invalid file type: {ContentType} / {FileName}", contentType, mp3File.FileName);
                    return View(song);
                }

                // Prepare folders
                var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                var musicFolder = Path.Combine(uploadsRoot, "music");
                var lyricsFolder = Path.Combine(uploadsRoot, "lyrics");
                Directory.CreateDirectory(musicFolder);
                Directory.CreateDirectory(lyricsFolder);

                // Save MP3 and set DB field MP3File
                var mp3FileName = $"{Guid.NewGuid()}{Path.GetExtension(mp3File.FileName)}";
                var mp3Path = Path.Combine(musicFolder, mp3FileName);
                await using (var stream = new FileStream(mp3Path, FileMode.Create))
                {
                    await mp3File.CopyToAsync(stream);
                }

                song.MP3File = $"/uploads/music/{mp3FileName}";
                _logger.LogInformation("Saved MP3 to {Path}", song.MP3File);

                // Save optional lyrics file to disk and read its content
                string? lyricsContentFromFile = null;
                if (lyricsFile != null && lyricsFile.Length > 0)
                {
                    var lyricsFileName = $"{Guid.NewGuid()}{Path.GetExtension(lyricsFile.FileName)}";
                    var lyricsPath = Path.Combine(lyricsFolder, lyricsFileName);
                    await using (var stream = new FileStream(lyricsPath, FileMode.Create))
                    {
                        await lyricsFile.CopyToAsync(stream);
                    }

                    _logger.LogInformation("Saved lyrics file to {Path}", $"/uploads/lyrics/{lyricsFileName}");

                    // read uploaded lyrics text
                    lyricsFile.OpenReadStream().Position = 0;
                    using var reader = new StreamReader(lyricsFile.OpenReadStream());
                    lyricsContentFromFile = await reader.ReadToEndAsync();
                }

                song.UploadedAt = DateTime.UtcNow;
                // song.UploadedById = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); 

                // Now that server-only field MP3File is set, revalidate the full model
                ModelState.Clear();
                if (!TryValidateModel(song))
                {
                    var errors = ModelState
                        .Where(kv => kv.Value.Errors.Count > 0)
                        .Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value.Errors.Select(e => e.ErrorMessage))}");
                    _logger.LogWarning("Model invalid after file handling: {Errors}", string.Join(" | ", errors));
                    return View(song);
                }

                _context.Song.Add(song);
                await _context.SaveChangesAsync();

                // After saving song, persist lyrics (from textarea or lyrics file) to Lyric table
                var contentToStore = (lyricsContentFromFile ?? song.Lyrics)?.Trim();
                if (!string.IsNullOrWhiteSpace(contentToStore))
                {
                    var lyric = new Lyric
                    {
                        Content = contentToStore
                    };
                    _context.Lyric.Add(lyric);
                    await _context.SaveChangesAsync();

                    // link the saved lyric to the previously saved song (Song.LyricsId -> Lyric.Id)
                    song.LyricsId = lyric.Id;
                    _context.Song.Update(song);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Stored lyrics for song {SongId} in Lyric table", song.Id);
                }

                _logger.LogInformation("Song saved to database with id {Id}", song.Id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the song for Title='{Title}'", song?.Title);
                ModelState.AddModelError(string.Empty, "An error occurred while uploading the song. See logs for details.");
#if DEBUG
                ModelState.AddModelError(string.Empty, ex.Message);
#endif
                return View(song);
            }
        }

        // GET: Songs/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit called with null id");
                return NotFound();
            }

            var song = await _context.Song  .FindAsync(id);
            if (song == null)
            {
                _logger.LogWarning("Edit not found for id {Id}", id);
                return NotFound();
            }
            return View(song);
        }

        // POST: Songs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Song song, IFormFile? MP3File, string? ExistingFilePath)
        {
            if (id != song.Id)
            {
                _logger.LogWarning("Edit id mismatch: route id {RouteId} vs model id {ModelId}", id, song.Id);
                return NotFound();
            }

           
            try
            {
                if (MP3File != null && MP3File.Length > 0)
                {
                    var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    var musicFolder = Path.Combine(uploadsRoot, "music");
                    Directory.CreateDirectory(musicFolder);
                    var mp3FileName = $"{Guid.NewGuid()}{Path.GetExtension(MP3File.FileName)}";
                    var mp3Path = Path.Combine(musicFolder, mp3FileName);
                    await using (var stream = new FileStream(mp3Path, FileMode.Create))
                    {
                        await MP3File.CopyToAsync(stream);
                    }
                    song.MP3File = $"/uploads/music/{mp3FileName}";
                    _logger.LogInformation("Updated MP3 file for song {Id}", song.Id);
                }
                else
                {
                    song.MP3File = ExistingFilePath ?? song.MP3File;
                }

                    _context.Update(song);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Song {Id} updated", song.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SongExists(song.Id))
                    {
                        _logger.LogWarning("Edit concurrency: song {Id} not found", song.Id);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

         

        // GET: Songs/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete called with null id");
                return NotFound();
            }

            var song = await _context.Song.FirstOrDefaultAsync(m => m.Id == id);
            if (song == null)
            {
                _logger.LogWarning("Delete not found for id {Id}", id);
                return NotFound();
            }

            return View(song);
        }

        // POST: Songs/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var song = await _context.Song.FindAsync(id);
            if (song != null)
            {
                _context.Song.Remove(song);
                _logger.LogInformation("Song {Id} removed from DB", id);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SongExists(int id)
        {
            return _context.Song.Any(e => e.Id == id);
        }
    }
}
