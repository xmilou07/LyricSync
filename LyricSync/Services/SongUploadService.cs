using System;
using System.Linq;
using System.Threading.Tasks;
using LyricSync.Data;
using LyricSync.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LyricSync.Services
{
    public class SongUploadService : ISongUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly LyricTimingGenerator _timingGenerator;

        public SongUploadService(ApplicationDbContext context, IFileStorageService fileStorage, LyricTimingGenerator timingGenerator)
        {
            _context = context;
            _fileStorage = fileStorage;
            _timingGenerator = timingGenerator;
        }

        public async Task<Song> UploadSongAsync(Song song, IFormFile mp3File, IFormFile? lyricsFile, string userId)
        {
            // Save MP3
            var mp3Relative = await _fileStorage.SaveMusicAsync(mp3File);
            song.MP3File = mp3Relative;
            song.UploadedAt = DateTime.UtcNow;
            song.UploadedById = userId ?? string.Empty;

            // validate before adding, but caller should ensure modelstate
            _context.Song.Add(song);
            await _context.SaveChangesAsync();

            // handle lyrics from file or textarea
            string? lyricsContentFromFile = null;
            if (lyricsFile != null && lyricsFile.Length > 0)
            {
                lyricsContentFromFile = await ReadFileContentAsync(lyricsFile);
                await _fileStorage.SaveLyricsFileAsync(lyricsFile); // store physical file for reference
            }

            var contentToStore = (lyricsContentFromFile ?? song.Lyrics)?.Trim();
            if (!string.IsNullOrWhiteSpace(contentToStore))
            {
                var fullMp3Path = _fileStorage.MapPath(song.MP3File);
                var duration = _timingGenerator.GetAudioDurationSeconds(fullMp3Path);

                var storedContent = contentToStore;
                if (duration.HasValue)
                {
                    try
                    {
                        var lrc = _timingGenerator.GenerateLrcFromLines(contentToStore, duration.Value);
                        if (!string.IsNullOrWhiteSpace(lrc))
                            storedContent = lrc;
                    }
                    catch
                    {
                        // ignore, controller will log if needed
                    }
                }

                var lyric = new Lyric { Content = storedContent };
                _context.Lyric.Add(lyric);
                await _context.SaveChangesAsync();

                song.LyricsId = lyric.Id;
                _context.Song.Update(song);
                await _context.SaveChangesAsync();
            }

            return song;
        }

        public async Task UpdateSongAsync(Song existingSong, Song incomingSong, IFormFile? mp3File, string? existingFilePath, bool removeLyric)
        {
            if (mp3File != null && mp3File.Length > 0)
            {
                var mp3Relative = await _fileStorage.SaveMusicAsync(mp3File);
                existingSong.MP3File = mp3Relative;
            }
            else
            {
                existingSong.MP3File = existingFilePath ?? existingSong.MP3File;
            }

            existingSong.Title = incomingSong.Title;
            existingSong.Artist = incomingSong.Artist;
            existingSong.Album = incomingSong.Album;
            existingSong.Genre = incomingSong.Genre;
            existingSong.UploadedAt = incomingSong.UploadedAt != default ? incomingSong.UploadedAt : existingSong.UploadedAt;

            var incomingLyrics = incomingSong.Lyrics?.Trim();

            if (removeLyric == true)
            {
                if (existingSong.Lyric != null)
                {
                    _context.Lyric.Remove(existingSong.Lyric);
                    existingSong.Lyric = null;
                    existingSong.LyricsId = null;
                }
            }
            else if (existingSong.Lyric != null)
            {
                existingSong.Lyric.Content = incomingLyrics ?? string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(incomingLyrics))
            {
                existingSong.Lyric = new Lyric { Content = incomingLyrics };
            }

            _context.Update(existingSong);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSongFilesAsync(Song song)
        {
            if (!string.IsNullOrWhiteSpace(song.MP3File))
            {
                await _fileStorage.DeleteFileAsync(song.MP3File);
            }

            if (song.Lyric != null)
            {
                // no file for lyric rows unless lyricsFile stored; keep behavior identical
            }
        }

        private static async Task<string> ReadFileContentAsync(IFormFile file)
        {
            file.OpenReadStream().Position = 0;
            using var reader = new System.IO.StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync();
        }
    }
}