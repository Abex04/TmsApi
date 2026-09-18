using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

// Flat, cross-course enrollment list for the instructor dashboard - the
// existing EnrollmentsController is scoped per-course (api/courses/{id}/
// enrollments) which can't answer "show me every enrollment across every
// course." Read-only, so a simple projected DTO is returned directly
// rather than routing through MediatR/services for this one list.
[ApiController]
[Route("api/enrollments")]
public class AllEnrollmentsController(TmsDbContext context) : ControllerBase
{
    public record EnrollmentListItem(
        int Id,
        int StudentId,
        string StudentName,
        int CourseId,
        string CourseName,
        string Status,
        DateTime EnrolledAt);

    // GET /api/enrollments
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        // OrderBy must run on the raw entity BEFORE projecting into the
        // record - EF Core can't translate ordering by a property of an
        // already-constructed DTO back into SQL.
        var enrollments = await context.Enrollments
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EnrollmentListItem(
                e.Id,
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Code,
                e.Status.ToString(),
                e.EnrolledAt))
            .ToListAsync(ct);

        return Ok(enrollments);
    }
}
