using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LyricSync.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveMusicAsync(IFormFile file);
        Task<string> SaveLyricsFileAsync(IFormFile file);
        Task DeleteFileAsync(string relativePath);
        string MapPath(string relativePath);
    }
}