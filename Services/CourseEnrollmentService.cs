using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

// Real implementation of ICourseEnrollmentService, backed by TmsDbContext.
public class CourseEnrollmentService(TmsDbContext context, ILogger<CourseEnrollmentService> logger)
    : ICourseEnrollmentService
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
        // Select() projects directly into the DTO at the database level —
        // never loads navigation properties into memory.
        return context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<EnrollmentResponseDto>)t.Result, 
                TaskContinuationOptions.ExecuteSynchronously);
    }
}