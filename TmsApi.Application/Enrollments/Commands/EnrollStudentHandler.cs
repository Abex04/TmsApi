using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

// The handler contains ALL the business logic for enrolling a student.
// The controller knows nothing about these rules — it just sends the command
// and translates the Result into the right HTTP status code.
// Notice: no try/catch, no throws for expected failures, no HTTP concerns.
// Every code path produces a typed Result — readable in 30 seconds.
public class EnrollStudentHandler(
    IEnrollmentService enrollmentService,
    ICourseService courseService)
    : IRequestHandler<EnrollStudentCommand, Result<EnrollmentCreated, EnrollmentError>>
{
    public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle(
        EnrollStudentCommand command, CancellationToken ct)
    {
        // Rule 1: Does the course exist?
        // Includes Enrollments so we can check capacity without a second query.
        var course = await courseService.GetByCodeAsync(command.CourseCode, ct);
        if (course is null)
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseNotFound(command.CourseCode));

        // Rule 2: Is the course full?
        if (course.Enrollments.Count >= course.MaxCapacity)
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseFull(course.Title, course.MaxCapacity));

        // Rule 3: Is the student already enrolled?
        if (await enrollmentService.ExistsAsync(command.StudentId, command.CourseCode, ct))
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.AlreadyEnrolled(command.StudentId, command.CourseCode));

        // All rules passed — create the enrollment
        var enrollment = new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };

        await enrollmentService.AddAsync(enrollment, ct);

        return Result<EnrollmentCreated, EnrollmentError>.Success(
            new EnrollmentCreated(enrollment.Id, enrollment.StudentId, course.Code));
    }
}