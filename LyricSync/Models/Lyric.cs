using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LyricSync.Models
{
    public class Lyric
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public Song? Song { get; set; }
    }
}
