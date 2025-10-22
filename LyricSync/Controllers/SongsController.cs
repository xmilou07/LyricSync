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
        public async Task<IActionResult> Index()
        {
            _logger.LogDebug("Loading songs index");
            return View(await _context.Song.ToListAsync());
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Songs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Artist,Album,Lyrics,Genre,UploadedById")] Song song, IFormFile mp3File, IFormFile lyricsFile)
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

                // Save MP3
                var mp3FileName = $"{Guid.NewGuid()}{Path.GetExtension(mp3File.FileName)}";
                var mp3Path = Path.Combine(musicFolder, mp3FileName);
                await using (var stream = new FileStream(mp3Path, FileMode.Create))
                {
                    await mp3File.CopyToAsync(stream);
                }
                song.FilePath = $"/uploads/music/{mp3FileName}";
                _logger.LogInformation("Saved MP3 to {Path}", song.FilePath);

                // Save lyrics file if provided
                if (lyricsFile != null && lyricsFile.Length > 0)
                {
                    var lyricsFileName = $"{Guid.NewGuid()}{Path.GetExtension(lyricsFile.FileName)}";
                    var lyricsPath = Path.Combine(lyricsFolder, lyricsFileName);
                    await using (var stream = new FileStream(lyricsPath, FileMode.Create))
                    {
                        await lyricsFile.CopyToAsync(stream);
                    }

                    song.Lyrics = string.IsNullOrWhiteSpace(song.Lyrics)
                        ? $"/uploads/lyrics/{lyricsFileName}"
                        : song.Lyrics + "\n" + $"/uploads/lyrics/{lyricsFileName}";

                    _logger.LogInformation("Saved lyrics to {Path}", $"/uploads/lyrics/{lyricsFileName}");
                }

                song.UploadedAt = DateTime.UtcNow;
                song.UploadedById = song.UploadedById; // preserve if provided, otherwise 0

                _context.Song.Add(song);
                await _context.SaveChangesAsync();

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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit called with null id");
                return NotFound();
            }

            var song = await _context.Song.FindAsync(id);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Artist,Album,Lyrics,FilePath,UploadedAt,UploadedById,Genre")] Song song)
        {
            if (id != song.Id)
            {
                _logger.LogWarning("Edit id mismatch: route id {RouteId} vs model id {ModelId}", id, song.Id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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

            var errors = ModelState
                .Where(kv => kv.Value.Errors.Count > 0)
                .Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value.Errors.Select(e => e.ErrorMessage))}");
            _logger.LogWarning("ModelState invalid: {Errors}", string.Join(" | ", errors));
            return View(song);
        }

        // GET: Songs/Delete/5
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
