using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/students")]
[ApiVersion("2.0")]
[Authorize]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    // GET /api/v2/students/me
    // Resolves the logged-in Student's domain record from their JWT
    // identity ("sub" claim = TmsUser.Id). Needed because enrollment
    // features (Enroll, GetSchedule) operate on the numeric Student.Id,
    // not the Identity account - the frontend can't get that any other way.
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentStudent()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { detail = "Invalid or missing identity." });
        }

        var student = await context.Students
            .FirstOrDefaultAsync(s => s.TmsUserId == userId);

        if (student is null)
        {
            return NotFound(new { detail = "No student record linked to this account." });
        }

        return Ok(new
        {
            id = student.Id,
            registrationNumber = student.RegistrationNumber,
            name = student.Name,
            gpa = student.GPA
        });
    }
}
