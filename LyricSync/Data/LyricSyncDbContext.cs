using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LyricSync.Models;

namespace LyricSync.Data
{
    public class LyricSyncDbContext : IdentityDbContext<ApplicationUser>
    {
        public LyricSyncDbContext(DbContextOptions<LyricSyncDbContext> options)
            : base(options)
        {
        }

        public DbSet<Song> Songs { get; set; }
        public DbSet<Lyric> Lyrics { get; set; }

    }
}
