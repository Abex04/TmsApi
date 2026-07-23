using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

// V2 CoursesController — uses ICachedCourseService instead of hitting
// the database directly. Cache stampede protection is built in.
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedCourseService) : ControllerBase
{
    // GET /api/v2/courses
    // Returns courses from cache — only one DB query fires even if
    // 50 requests arrive simultaneously on a cold cache.
    [HttpGet]
    public async Task<IActionResult> GetCourses(CancellationToken ct)
    {
        var courses = await cachedCourseService.GetAllCoursesAsync(ct);
        return Ok(new
        {
            data = courses,
            meta = new { totalCount = courses.Count },
            links = new
            {
                self = "/api/v2/courses",
                enroll = "/api/v2/enrollments"
            }
        });
    }
}
