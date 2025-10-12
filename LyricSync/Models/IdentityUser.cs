using Microsoft.AspNetCore.Identity;
using System.Diagnostics.Contracts;

namespace LyricSync.Models
{
    public class IdentityUser
    {
        public string Name { get; set; } = string.Empty;
    }
}
