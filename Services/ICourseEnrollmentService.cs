using TmsApi.Dtos;

namespace TmsApi.Services;

// Contract for enrollment-related business logic and data access, scoped to
// "enrolling a student into a specific course." Named CourseEnrollmentService
// (not EnrollmentService) to avoid colliding with the existing M4
// EnrollmentService, which is a separate in-memory service used elsewhere.
public interface ICourseEnrollmentService
{
    // Fetch a single enrollment by its Id, scoped to a specific course.
    // Returns null if no matching enrollment exists.
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);

    // Create a new enrollment for a student into a course, and return it
    // in the safe response shape.
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);
}