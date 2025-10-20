using Microsoft.AspNetCore.Identity;

namespace LyricSync.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
    }
}
