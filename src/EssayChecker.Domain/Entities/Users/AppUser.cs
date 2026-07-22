using Microsoft.AspNetCore.Identity;

namespace EssayChecker.Domain.Entities.Users;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastLoginDate { get; set; }
}
