using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportingController(TmsDbContext context) : ControllerBase
{
    // 1. How many active students have GPA >= 3.0?
    [HttpGet("active-students-count")]
    public async Task<IActionResult> GetActiveStudentsCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        return Ok(new { ActiveStudentsWithGoodGPA = count });
    }

    // 2. Which courses have the most enrollments, sorted descending?
    [HttpGet("courses-by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollment()
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        return Ok(list);
    }

    // 3. What is the average GPA per course?
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();
        return Ok(list);
    }

    // 4. Which students have zero enrollments?
    [HttpGet("students-without-enrollments")]
    public async Task<IActionResult> GetStudentsWithoutEnrollments()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();
        return Ok(list);
    }

    // Exercise 3 - Pagination: paged list of students
    [HttpGet("students")]
    public async Task<IActionResult> GetStudentsPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var students = await context.Students
            .OrderBy(s => s.Name)                          // stable sort — required before Skip/Take
            .Skip((page - 1) * pageSize)                   // skip previous pages
            .Take(pageSize)                                 // take only this page
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            Data = students
        });
    }

    // Exercise 3 - Top 5 courses by enrollment count
    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses(CancellationToken cancellationToken = default)
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)    // most enrolled first
            .Take(5)                                        // top 5 only
            .ToListAsync(cancellationToken);

        return Ok(list);
    }
}
