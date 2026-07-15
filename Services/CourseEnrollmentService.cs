using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;
using TmsApi.Application.Interfaces;

namespace TmsApi.Services;

// Real implementation of ICourseEnrollmentService, backed by TmsDbContext.
// Also implements Application.Interfaces.IEnrollmentService so CQRS handlers
// can depend on the interface without knowing about EF Core directly.
public class CourseEnrollmentService(TmsDbContext context, ILogger<CourseEnrollmentService> logger)
    : ICourseEnrollmentService, Application.Interfaces.IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct)
    {
        // Scoped to both the enrollment's own Id AND its parent CourseId —
        // matches the nested route /api/courses/{courseId}/enrollments/{id}.
        return context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        // Build the new Enrollment entity. EnrolledAt is stamped server-side
        // with UtcNow — we never trust a client to send us a timestamp.
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrolled student {StudentId} into course {CourseId} (enrollment {EnrollmentId})",
            request.StudentId, courseId, enrollment.Id);

        // Re-fetch through GetByIdAsync so the response uses the same
        // projection logic as every other read.
        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    public Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        // AsNoTracking() — read-only fetch, no change-tracking overhead.
        // Where() scopes to just this course's enrollments.
        // Select() projects directly into the DTO at the database level.
        return context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<EnrollmentResponseDto>)t.Result,
                TaskContinuationOptions.ExecuteSynchronously);
    }

    // Check if a student is already enrolled in a course by course code.
    // Used by EnrollStudentHandler to prevent duplicate enrollments.
    public Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);

    // Persist a new enrollment — used by EnrollStudentHandler.
    // SaveChangesAsync is called here so the handler stays clean.
    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrolled student {StudentId} into course {CourseId} (enrollment {EnrollmentId})",
            enrollment.StudentId, enrollment.CourseId, enrollment.Id);
    }

    // Fetch all enrollments for a student, including the Course navigation
    // property so GetStudentScheduleHandler can project course details.
    public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);
}