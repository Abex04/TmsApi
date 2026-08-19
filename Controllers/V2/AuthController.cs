using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Identity;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("2.0")]
public class AuthController(
    UserManager<TmsUser> userManager,
    RoleManager<IdentityRole> roleManager) : ControllerBase
{
    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role);

    // POST /api/v2/auth/register
    // M11 Session 1: real account creation via UserManager, replacing
    // M10's single hardcoded admin/Password123! demo credential.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            // Prevent account enumeration - don't reveal whether this
            // email is already registered.
            return Ok(new { message = "Registration request received." });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(request.Role));
        }
        await userManager.AddToRoleAsync(user, request.Role);

        return Ok(new { message = "Registration successful." });
    }

    public record LoginRequest(string Email, string Password);

    // POST /api/v2/auth/login
    // M11 Session 1: real UserManager-backed authentication with lockout
    // protection. Still issues the tms_auth HttpOnly cookie on success,
    // same as the M10 flow - so credentialsInterceptor, XSRF middleware,
    // and everything downstream that depends on that cookie keeps working
    // unchanged.
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new { detail = "Account locked due to multiple failed login attempts. Try again in 15 minutes." });
        }

        var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        // Reset failed attempt counter on successful login
        await userManager.ResetAccessFailedCountAsync(user);

        var dummyJwt = "header.payload.signature-demo-token";
        Response.Cookies.Append("tms_auth", dummyJwt, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(2)
        });

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName
        });
    }

    // GET /api/v2/auth/me
    // Unchanged from M10 - still just checks for the cookie's presence.
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(new { message = "Authenticated" });
        }

        return Unauthorized(new { detail = "Session expired or missing authentication cookie." });
    }
}
