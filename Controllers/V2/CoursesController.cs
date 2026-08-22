using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(
    ICourseService courseService,
    TmsDbContext context,
    IAuthorizationService authorizationService) : ControllerBase
{
    // GET /api/v2/courses - unchanged, stays anonymous. Locking this down
    // would break the already-verified course-catalog read flow from
    // M10 Session 3.
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await courseService.GetCoursesAsync(
            new PagedRequest
            {
                Page = page,
                PageSize = pageSize
            },
            ct);
        var hasNext = result.HasNext;
        var hasPrevious = result.HasPrevious;
        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages,
                hasNext,
                hasPrevious
            },
            links = new
            {
                self = $"/api/v2/courses?page={result.Page}&pageSize={result.PageSize}",
                next = hasNext
                    ? $"/api/v2/courses?page={result.Page + 1}&pageSize={result.PageSize}"
                    : (string?)null,
                prev = hasPrevious
                    ? $"/api/v2/courses?page={result.Page - 1}&pageSize={result.PageSize}"
                    : (string?)null,
                enroll = "/api/v2/enrollments"
            }
        });
    }

    // PUT /api/v2/courses/{id}
    // M11 Session 3: resource-based authorization. [Authorize] confirms
    // the caller has SOME valid role; AuthorizeAsync(User, course,
    // "CanEditCourse") then confirms THIS caller owns THIS specific
    // course (or is an Admin, who owns everything). 403 Forbid() is
    // returned when the caller is authenticated but not permitted -
    // distinct from 401 Unauthorized, which means not authenticated at all.
    [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest dto, CancellationToken ct)
    {
        var course = await context.Courses.FindAsync([id], ct);
        if (course is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, course, "CanEditCourse");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        course.Title = dto.Title;
        await context.SaveChangesAsync(ct);

        return NoContent();
    }
}
