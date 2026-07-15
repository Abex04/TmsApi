using TmsApi.Entities;

namespace TmsApi.Application.Interfaces;

// The course-related operations the CQRS handlers need.
// Depends on an interface (not TmsDbContext directly) so handlers
// can be unit-tested without a real database connection.
public interface ICourseService
{
    // Fetch a course by its code, including its Enrollments collection
    // so the handler can check capacity without a second query.
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct);
}
