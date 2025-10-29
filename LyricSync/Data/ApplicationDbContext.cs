using LyricSync.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LyricSync.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {    }

        // Songs table
        public DbSet<Song> Song { get; set; }

        // Lyrics table
        public DbSet<Lyric> Lyric { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-one where Song has the foreign key LyricsId -> Lyric.Id
            modelBuilder.Entity<Song>()
                .HasOne(s => s.Lyric)
                .WithOne(l => l.Song)
                .HasForeignKey<Song>(s => s.LyricsId);

            modelBuilder.Entity<Song>()
                .Property(s => s.LyricsId)
                .IsRequired(false);
        }
    }
}
