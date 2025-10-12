using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LyricSync.Data
{
    // ApplicationDbContext inherits from IdentityDbContext, which sets up all tables needed for Microsoft Identity.
    // If you want to extend IdentityUser with custom properties, create a class inheriting from IdentityUser and use it here.
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add DbSet properties for your own entities (e.g., Song) below.
        // public DbSet<Song> Songs { get; set; }
    }
}
