using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers.V1;

// V1 is the frozen contract — existing clients depend on this shape.
// Never rename fields, remove fields, or add required fields here.
// The route template uses v{version:apiVersion} so ASP.NET Core's
// versioning middleware routes requests to the right controller automatically.
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("1.0")]
public class CoursesController(TmsDbContext context) : ControllerBase
{
    // GET /api/v1/courses
    // Returns the V1 TMS contract: items array + paging fields at the root.
    // This is the shape tablets and Angular list screens depend on.
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Clamp page and pageSize to safe values
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = context.Courses.AsNoTracking();

        // Count BEFORE paging — same rule as Module 6
        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                EnrollmentCount = c.Enrollments.Count
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // V1 shape: flat root object with items + paging metadata
        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages,
            hasNext = page < totalPages,
            hasPrevious = page > 1
        });
    }
}