using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

// The enrollment-related operations the CQRS handlers need.
// Kept minimal — only what EnrollStudentHandler and GetStudentScheduleHandler
// actually use, nothing more.
public interface IEnrollmentService
{
    // Check if a student is already enrolled in a specific course
    // to prevent duplicate enrollments.
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct);

    // Persist a new enrollment to the database.
    Task AddAsync(Enrollment enrollment, CancellationToken ct);

    // Fetch all enrollments for a student, including the Course navigation
    // property so GetStudentScheduleHandler can project course details.
    Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct);
}