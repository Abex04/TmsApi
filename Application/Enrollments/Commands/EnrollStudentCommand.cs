using MediatR;
using TmsApi.Application.Common;

namespace TmsApi.Application.Enrollments.Commands;

// A command represents an intention to change state — "enroll this student
// in this course." MediatR uses the IRequest<> type signature to find the
// right handler (EnrollStudentHandler) automatically.
// The return type Result<EnrollmentCreated, EnrollmentError> forces the
// controller to handle both success and failure explicitly.
public record EnrollStudentCommand(int StudentId, string CourseCode)
    : IRequest<Result<EnrollmentCreated, EnrollmentError>>;

// The success payload returned when enrollment succeeds.
// Kept small — only what the client needs to confirm the enrollment.
public record EnrollmentCreated(int EnrollmentId, int StudentId, string CourseCode);
