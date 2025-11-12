using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LyricSync.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _uploadsRoot;
        private readonly string _musicFolder;
        private readonly string _lyricsFolder;

        public FileStorageService()
        {
            _uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _musicFolder = Path.Combine(_uploadsRoot, "music");
            _lyricsFolder = Path.Combine(_uploadsRoot, "lyrics");

            Directory.CreateDirectory(_musicFolder);
            Directory.CreateDirectory(_lyricsFolder);
        }

        public async Task<string> SaveMusicAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(_musicFolder, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/music/{fileName}";
        }

        public async Task<string> SaveLyricsFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(_lyricsFolder, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/lyrics/{fileName}";
        }

        public Task DeleteFileAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.CompletedTask;

            var relative = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
            try
            {
                if (File.Exists(full))
                    File.Delete(full);
            }
            catch
            {
                // swallow - callers will log as needed
            }

            return Task.CompletedTask;
        }

        public string MapPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            var relative = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
        }
    }
}