using Microsoft.AspNetCore.Identity;

namespace TmsApi.Identity;

// Extends IdentityUser with the extra fields TMS actually needs.
// IdentityUser already provides Id, UserName, Email, PasswordHash,
// lockout fields, etc. - we're not duplicating any of that here.
public class TmsUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
}
