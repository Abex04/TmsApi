using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

// [ApiController] enables automatic model validation — if a CreateCourseRequest
// fails its Data Annotations (Required, Range, RegularExpression, etc.),
// ASP.NET Core returns 400 Bad Request automatically, before CreateCourse runs.
[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    // GET /api/courses/{id}
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        return course is not null ? Ok(course) : NotFound();
    }

    // POST /api/courses
    // Binding to CreateCourseRequest (instead of the raw Course entity) means
    // the client can never set fields we don't want them setting, and every
    // field is validated before this method body runs.
    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
    {
        var result = await courseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}