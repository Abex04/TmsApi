using TmsApi.Dtos;

namespace TmsApi.Services;

// Contract for course-related business logic and data access.
// Note: this interface now speaks entirely in DTOs (CourseResponseDto,
// CreateCourseRequest), never in the raw Course entity. This keeps the
// EF entity — and its navigation properties — from ever leaking past
// the service boundary.
public interface ICourseService
{
    // Fetch a single course by its Id, projected into the safe response shape.
    // Returns null if no course with that Id exists.
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);

    // Create a new course from validated request data, and return
    // the created course in the same safe response shape.
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
}