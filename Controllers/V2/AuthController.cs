using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("2.0")]
public class AuthController : ControllerBase
{
    // POST /api/v2/auth/login
    // M10 Session 2: writes the auth token into an HttpOnly cookie instead
    // of returning it in the response body. Client-side JavaScript - including
    // a malicious XSS payload - is physically incapable of reading an HttpOnly
    // cookie, which is what makes this safer than localStorage.
    [HttpPost("login")]
    public IActionResult Login(
        [FromBody] LoginRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        // Demo account only - real password hashing and ASP.NET Core Identity
        // are built in Module 12. This session is purely about the cookie
        // transport mechanism.
        if (request.Username == "admin" && request.Password == "Password123!")
        {
            var dummyJwt = "header.payload.signature-demo-token";

            Response.Cookies.Append("tms_auth", dummyJwt, new CookieOptions
            {
                HttpOnly = true,                      // JavaScript cannot read this - the whole point.
                Secure = !env.IsDevelopment(),         // HTTPS required in prod; HTTP permitted locally.
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Ok(new UserProfileDto("System Admin", "Admin"));
        }

        return Unauthorized(new { detail = "Invalid username or password." });
    }

    // GET /api/v2/auth/me
    // The browser attaches the tms_auth cookie automatically on same-site
    // requests - we never need the client to remember or resend a token.
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(new UserProfileDto("System Admin", "Admin"));
        }

        return Unauthorized(new { detail = "Session expired or missing authentication cookie." });
    }
}
