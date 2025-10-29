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

            modelBuilder.Entity<Song>()
                .HasOne(s => s.Lyric)
                .WithOne(l => l.Song)
                .HasForeignKey<Lyric>(l => l.Id);
        }
    }
}
