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

        // keep this for form binding; lyrics will be stored in the Lyric table
        [NotMapped]
        public string Lyrics { get; set; } = string.Empty;

        [Required]
        public string MP3File { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }

        public int UploadedById { get; set; }

        [Required]
        public string Genre { get; set; } = string.Empty;

        [NotMapped]
        public IFormFile? MP3Upload { get; set; }

        // navigation property for the one-to-one relationship to Lyric
        public Lyric? Lyric { get; set; }
    }
}