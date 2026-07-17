using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

// [Tags("Courses")] at the class level groups ALL course endpoints together
// in Scalar's left-hand index. Never put [Tags] on individual actions —
// that breaks the grouping and scatters endpoints into a flat list.
// [Produces("application/json")] declares the response content type for
// Scalar's "Try It" panel.
// The class-level 500 ProducesResponseType means every action inherits it —
// no need to repeat it on each action individually.
[ApiController]
[Route("api/courses")]
[Tags("Courses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class CoursesController(ICourseService courseService, LinkGenerator linkGenerator) : ControllerBase
{
    // GET /api/courses?page=1&pageSize=10&search=fund&orderBy=Code&descending=true
    // [FromQuery] binds PagedRequest from the query string, not the request body —
    // correct for a GET, since GET requests conventionally carry no body.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List courses with pagination")]
    [EndpointDescription("Returns a paginated, optionally filtered list of TMS courses. PageSize is capped at 50.")]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await courseService.GetCoursesAsync(request, ct);
        return Ok(result);
    }

    // GET /api/courses/{id}
    // Returns CourseDetailDto — the richer shape with a Links array, telling
    // the client what it can do next with this specific course (HATEOAS).
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription("Returns course details with HATEOAS links. Returns 404 if the course does not exist.")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound();
        }

        // GetPathByName builds a URL from the SAME routing metadata the framework
        // uses to match incoming requests — never a hand-typed string. If a route
        // is renamed later, this call automatically reflects the new path.
        var selfHref = linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id });
        var enrollmentsHref = linkGenerator.GetPathByName(HttpContext, "ListCourseEnrollments", new { courseId = id });

        var links = new List<LinkDto>
        {
            new(selfHref!, "self", "GET"),
            new(selfHref!, "update", "PUT"),
            new(selfHref!, "delete", "DELETE"),
            new(enrollmentsHref!, "enrollments", "GET")
        };

        // The conditional link: only add "enroll" if there's still room.
        // This is what makes HATEOAS earn its cost — the Angular team can check
        // "does this course have an enroll link?" instead of duplicating the
        // capacity rule in TypeScript.
        if (course.EnrollmentCount < course.MaxCapacity)
        {
            links.Add(new LinkDto(enrollmentsHref!, "enroll", "POST"));
        }

        var detail = new CourseDetailDto
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            Links = links
        };

        return Ok(detail);
    }

    // POST /api/courses
    // Binding to CreateCourseRequest (instead of the raw Course entity) means
    // the client can never set fields we don't want them setting, and every
    // field is validated before this method body runs.
    [HttpPost]
    [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription("Creates a course with a unique code. Returns 409 if the course code already exists.")]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
    {
        // Check the business rule BEFORE touching the database with an insert.
        // This avoids a raw database exception (500) when the unique index on
        // Code would otherwise reject a duplicate — we catch it ourselves and
        // return a clean, predictable 409 Conflict instead.
        if (await courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail = $"A course with code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await courseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}