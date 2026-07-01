using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

// Nested resource route: enrollments only exist in the context of a course.
// {courseId:int} here must match the "courseId" parameter name in every
// action below exactly, or ASP.NET Core won't bind it correctly.
[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(
    ICourseService courseService,
    ICourseEnrollmentService enrollmentService) : ControllerBase
{
    // GET /api/courses/{courseId}/enrollments/{id}
    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(courseId, id, ct);
        return enrollment is not null ? Ok(enrollment) : NotFound();
    }

    // POST /api/courses/{courseId}/enrollments
    [HttpPost]
    public async Task<IActionResult> EnrollStudent(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        // Step 1: Does the parent course even exist?
        // This check MUST come before the capacity check — a 404 always
        // takes priority over a 409, since you can't have a business-rule
        // conflict about a resource that isn't there.
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null)
        {
            return NotFound();
        }

        // Step 2: Is the course already full?
        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Step 3: Both checks passed — create the enrollment.
        var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);
        return CreatedAtAction(nameof(GetEnrollment), new { courseId, id = enrollment.Id }, enrollment);
    }
}