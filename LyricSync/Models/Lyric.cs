using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LyricSync.Models
{
    public class Lyric
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;


        // navigation back to Song
        public Song? Song { get; set; }
    }
}
