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
using LyricSync.Services;

namespace LyricSync.Controllers
{
    public class SongsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SongsController> _logger;
        private readonly LyricTimingGenerator _timingGenerator;
        private readonly ISongUploadService _songUploadService;
        private readonly IFileStorageService _fileStorage;

        public SongsController(ApplicationDbContext context, ILogger<SongsController> logger, LyricTimingGenerator timingGenerator, ISongUploadService songUploadService, IFileStorageService fileStorage)
        {
            _context = context;
            _logger = logger;
            _timingGenerator = timingGenerator;
            _songUploadService = songUploadService;
            _fileStorage = fileStorage;
        }

        // GET: Songs
        [Authorize]
        public async Task<IActionResult> Index()
        {
            _logger.LogDebug("Loading songs index");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Admin"))
                return View(await _context.Song.Include(s=>s.Lyric).ToListAsync());

            var items = await _context.Song
                .Where(s => s.UploadedById == userId)
                .Include(s => s.Lyric)
                .ToListAsync();
            return View(items);
        }

        // GET: Songs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null id");
                return NotFound();
            }

            // include the Lyric navigation so we can show or link to the stored lyric
            var song = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(m => m.Id == id);
            if (song == null)
            {
                _logger.LogWarning("Details not found for id {Id}", id);
                return NotFound();
            }

            return View(song);
        }

        // GET: Songs/Lyrics/5
        public async Task<IActionResult> Lyrics(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Lyrics called with null id");
                return NotFound();
            }

            var song = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(s => s.Id == id);
            if (song == null)
            {
                _logger.LogWarning("Lyrics not found for id {Id}", id);
                return NotFound();
            }

            // If there is no stored Lyric and no inline Lyrics text, show NotFound to keep behavior simple
            if (song.Lyric == null && string.IsNullOrWhiteSpace(song.Lyrics))
            {
                _logger.LogInformation("No lyrics available for song {Id}", id);
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
        public async Task<IActionResult> Create([Bind("Title,Artist,Album,Lyrics,Genre")] Song song, IFormFile? mp3File, IFormFile? lyricsFile)
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

                // Use the song upload service to handle the heavy lifting
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var savedSong = await _songUploadService.UploadSongAsync(song, mp3File, lyricsFile, userId);

                _logger.LogInformation("Song saved to database with id {Id}", savedSong.Id);
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

            // include Lyric so the view can display and edit the stored lyrics
            var song = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(s => s.Id == id);
            if (song == null)
            {
                _logger.LogWarning("Edit not found for id {Id}", id);
                return NotFound();
            }

            // populate the NotMapped Lyrics property so the textarea shows existing lyric content
            song.Lyrics = song.Lyric?.Content ?? string.Empty;

            return View(song);
        }

        // POST: Songs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Song song, IFormFile? MP3File, string? ExistingFilePath, bool removeLyric)
        {
            if (id != song.Id)
            {
                _logger.LogWarning("Edit id mismatch: route id {RouteId} vs model id {ModelId}", id, song.Id);
                return NotFound();
            }

            try
            {
                // load the existing entity including Lyric so we can update relationships safely
                var existingSong = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(s => s.Id == id);
                if (existingSong == null)
                {
                    _logger.LogWarning("Edit not found for id {Id}", id);
                    return NotFound();
                }

                await _songUploadService.UpdateSongAsync(existingSong, song, MP3File, ExistingFilePath, removeLyric);
                _logger.LogInformation("Song {Id} updated", existingSong.Id);
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
            // load song and include lyric so we can remove associated data
            var song = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(s => s.Id == id);
            if (song == null)
            {
                _logger.LogWarning("DeleteConfirmed: song {Id} not found", id);
                return RedirectToAction(nameof(Index));
            }

            // delete MP3 file from disk if present
            try
            {
                await _songUploadService.DeleteSongFilesAsync(song);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete MP3 file for song {Id}", id);
                // continue - don't fail the whole delete because of file system issues
            }

            // remove associated lyric row if present
            if (song.Lyric != null)
            {
                _context.Lyric.Remove(song.Lyric);
            }

            // remove song
            _context.Song.Remove(song);
            _logger.LogInformation("Song {Id} removed from DB", id);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SongExists(int id)
        {
            return _context.Song.Any(e => e.Id == id);
        }

        // GET: Songs/SyncLyrics/5
        public async Task<IActionResult> SyncLyrics(int? id)
        {
            if (id == null)
                return NotFound();

            var song = await _context.Song.Include(s => s.Lyric).FirstOrDefaultAsync(s => s.Id == id);

            if (song == null)
                return NotFound();

            return View(song); // passes Song (with Lyrics) to the view
        }
    }
}
