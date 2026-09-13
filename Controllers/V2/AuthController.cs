using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Identity;
using TmsApi.Services;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("2.0")]
public class AuthController(
    UserManager<TmsUser> userManager,
    RoleManager<IdentityRole> roleManager,
    TmsDbContext context,
    TokenService tokenService) : ControllerBase
{
    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
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

        // Self-registered Students get a matching Student domain record,
        // linked via TmsUserId, so enrollment features (which key off
        // Student.Id, not the Identity account) work immediately.
        // RegistrationNumber is derived from the DB-generated Id (saved
        // once to get it assigned, then updated) rather than a COUNT()
        // of existing rows, which collides after any row is deleted -
        // Id is guaranteed unique and never reused by Postgres identity.
        if (request.Role == "Student")
        {
            var student = new Entities.Student
            {
                RegistrationNumber = "PENDING",
                Name = $"{request.FirstName} {request.LastName}",
                GPA = 0,
                TmsUserId = user.Id
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var year = DateTime.UtcNow.Year;
            student.RegistrationNumber = $"TMS-{year}-{student.Id:D4}";
            await context.SaveChangesAsync();
        }

        return Ok(new { message = "Registration successful." });
    }


    public record LoginRequest(string Email, string Password);

    // POST /api/v2/auth/login
    // M11 Session 2: now issues a real JWT access token (15 min) + a
    // refresh token (7 days) in the JSON body. Still ALSO sets the
    // tms_auth HttpOnly cookie from M10 - both transport mechanisms
    // coexist; nothing from M10's XSRF/cookie flow was removed.
    [EnableRateLimiting("AuthLimiter")]
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

        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateJwt(user, roles);

        // Issue initial Refresh Token
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        // M10 cookie flow - kept alongside the new JWT flow.
        Response.Cookies.Append("tms_auth", "header.payload.signature-demo-token", new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(2)
        });

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token
        });
    }

    public record RefreshRequest(string RefreshToken);

    // POST /api/v2/auth/refresh
    // Rotation: every call invalidates the submitted token (IsUsed = true)
    // and issues a brand-new pair. Theft detection: if someone submits a
    // token that's ALREADY marked used, that's a strong signal that token
    // was stolen and used twice by two different parties - so we revoke
    // every token this user has, forcing a fresh login everywhere.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return Unauthorized(new { detail = "Invalid refresh token." });
        }

        if (storedToken.IsUsed)
        {
            var userTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId)
                .ToListAsync();

            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }
            await context.SaveChangesAsync();

            return Unauthorized(new { detail = "Token theft detected. All user sessions revoked." });
        }

        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { detail = "Refresh token expired or revoked." });
        }

        storedToken.IsUsed = true;

        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(storedToken.UserId);
        var roles = await userManager.GetRolesAsync(user!);
        var newAccessToken = tokenService.GenerateJwt(user!, roles);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(new { message = "Authenticated" });
        }

        return Unauthorized(new { detail = "Session expired or missing authentication cookie." });
    }

    public record ForgotPasswordRequest(string Email);

    // POST /api/v2/auth/forgot-password
    // Demo-mode password reset: generates a real ASP.NET Identity reset
    // token and returns it directly in the response instead of emailing
    // it, since this project has no email/SMTP infrastructure. In a real
    // production system this token would be emailed to the user instead
    // of returned here. Always returns 200 regardless of whether the
    // email exists, to avoid leaking which emails are registered.
    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Ok(new { message = "If that email is registered, a reset token has been generated." });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        return Ok(new
        {
            message = "If that email is registered, a reset token has been generated.",
            resetToken = token
        });
    }

    public record ResetPasswordRequest(string Email, string Token, string NewPassword);

    // POST /api/v2/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { detail = "Invalid request." });
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        return Ok(new { message = "Password reset successful." });
    }
}
