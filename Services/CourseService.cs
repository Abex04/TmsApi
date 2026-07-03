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
   // Checks whether a course with this exact code already exists.
    // AnyAsync translates to SQL "SELECT EXISTS(SELECT 1 ... LIMIT 1)" —
    // it stops at the first match instead of loading a full row, making
    // this the fastest way to answer a yes/no existence question.
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct); 
        public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        // Step 1: Start with an IQueryable — nothing is sent to the database yet.
        // AsNoTracking() because this is a read-only listing operation.
        IQueryable<Course> query = context.Courses.AsNoTracking();

        // Step 2: Filter FIRST, before counting or paging.
        // EF.Functions.ILike is PostgreSQL's case-insensitive LIKE — this means
        // a search for "fund" correctly matches "Web Development Fundamentals"
        // regardless of case.
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
                EF.Functions.ILike(c.Code, $"%{request.Search}%"));
        }

        // Step 3: Count BEFORE paging. This produces one SELECT COUNT(*) against
        // the filtered (but not yet paged) query — the TRUE total, not just
        // the count of one page. Counting after Skip/Take would be a bug.
        var totalCount = await query.CountAsync(ct);

        // Step 4: Sort next. Whitelist which OrderBy values are safe to use —
        // never let an arbitrary client-supplied string drive the query directly.
        // Anything not recognized silently falls back to sorting by Title.
        query = request.OrderBy switch
        {
            "Code" => request.Descending
                ? query.OrderByDescending(c => c.Code)
                : query.OrderBy(c => c.Code),
            "MaxCapacity" => request.Descending
                ? query.OrderByDescending(c => c.MaxCapacity)
                : query.OrderBy(c => c.MaxCapacity),
            _ => request.Descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title)
        };

        // Step 5 + 6: Page (Skip/Take), THEN project into the DTO — all still
        // inside the IQueryable chain, so EF Core translates the whole thing
        // into a single efficient SQL SELECT ... LIMIT ... OFFSET statement.
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .ToListAsync(ct);

        return new PagedResponse<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}