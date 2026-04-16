using Microsoft.AspNetCore.Identity;

namespace ContactManager.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
