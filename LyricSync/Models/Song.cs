using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace LyricSync.Models
{
    public class Song
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Artist { get; set; } = string.Empty;

        [Required]
        public string Album { get; set; } = string.Empty;

        [Required]
        public string Lyrics { get; set; } = string.Empty;

        // FilePath is set server-side after successful upload; do not require it from the client
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }

        public int UploadedById { get; set; }

        [Required]
        public string Genre { get; set; } = string.Empty;

        // NotMapped file used for uploads in the create form
        [NotMapped]
        [Required(ErrorMessage = "Please upload an MP3 file.")]
        public IFormFile MP3File { get; set; }
    }
}