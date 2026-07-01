using TmsApi.Entities;

namespace TmsApi.Services;

// Contract for course-related business logic and data access.
// The controller depends on this interface, not on the concrete
// CourseService class, so we can swap implementations or mock it in tests.
public interface ICourseService
{
    // Fetch a single course by its Id.
    // Returns null if no course with that Id exists — the controller
    // decides what HTTP status that null becomes (404 Not Found).
    Task<Course?> GetByIdAsync(int id, CancellationToken ct);

    // Create a new course and persist it to the database.
    // Returns the created course, including the database-generated Id.
    Task<Course> CreateAsync(Course course, CancellationToken ct);
}