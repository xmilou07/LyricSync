using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using LyricSync.Models;

namespace LyricSync.Services
{
    public interface ISongUploadService
    {
        Task<Song> UploadSongAsync(Song song, IFormFile mp3File, IFormFile? lyricsFile, string userId);
        Task UpdateSongAsync(Song existingSong, Song incomingSong, IFormFile? mp3File, string? existingFilePath, bool removeLyric);
        Task DeleteSongFilesAsync(Song song);
    }
}