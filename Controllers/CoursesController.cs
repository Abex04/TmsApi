using Microsoft.AspNetCore.Mvc;
using TmsApi.Entities;
using TmsApi.Services;

namespace TmsApi.Controllers;

// [ApiController] enables automatic model validation, automatic 400 responses
// on bad input, and other conventions expected of a REST API controller.
// [Route("api/courses")] — plural, resource-named, no verbs in the URL.
[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    // GET /api/courses/{id}
    // {id:int} route constraint — a non-integer id (e.g. "abc") is rejected
    // by the routing layer itself, before this method ever runs.
    // Name = nameof(GetCourseById) lets CreateCourse (below) build a Location
    // header that points back at this exact route.
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        // If the service found nothing, respond 404 Not Found.
        // Otherwise, wrap the course in a 200 OK response.
        return course is not null ? Ok(course) : NotFound();
    }

    // POST /api/courses
    [HttpPost]
    public async Task<IActionResult> CreateCourse(Course course, CancellationToken ct)
    {
        var result = await courseService.CreateAsync(course, ct);

        // 201 Created is the correct REST status for "a new resource was made."
        // CreatedAtAction automatically builds the Location header by resolving
        // the named route "GetCourseById" with the new course's Id.
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}