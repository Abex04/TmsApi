using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

// Contract for the cache-aware course service.
// Controllers and handlers depend on this interface, not on the
// concrete CachedCourseService, so the cache layer can be swapped
// or bypassed in tests without touching the controller.
public interface ICachedCourseService
{
    // Returns all courses from cache, falling back to the database on miss.
    // Stampede-safe: only one DB query fires even under concurrent load.
    Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct);

    // Invalidates all course cache entries by tag.
    // Call this after any write (create, update, delete) that affects courses.
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}
