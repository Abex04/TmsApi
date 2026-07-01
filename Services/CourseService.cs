using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

// Real implementation of ICourseService, backed by TmsDbContext (PostgreSQL via EF Core).
// Every method here returns/accepts DTOs — the Course entity itself never
// leaves this class, which is what prevents the circular-reference JSON crash.
public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        // AsNoTracking() = read-only fetch, no change-tracking overhead.
        // Select(...) projects straight into CourseResponseDto at the database level —
        // EF translates c.Enrollments.Count into a SQL COUNT(*) subquery,
        // so we never load the full enrollments list just to count it.
        return context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        // Build a real Course entity from the validated request data.
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        // Stage the insert, then actually execute it against PostgreSQL.
        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);

        // Log a single breadcrumb per write — not per read — to keep production logs useful, not noisy.
        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);

        // Re-fetch through GetByIdAsync so the response uses the exact same
        // projection as every other read — one single source of truth for
        // "what a course DTO looks like."
        // The '!' is safe here: we just inserted and saved this course,
        // so we know for certain it exists and GetByIdAsync will find it.
        return (await GetByIdAsync(course.Id, ct))!;
    }
}