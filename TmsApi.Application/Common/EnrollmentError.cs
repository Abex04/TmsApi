namespace TmsApi.Application.Common;

// Represents the different ways an enrollment business operation can fail.
// Each static factory method produces a named, typed error with:
// - Code: stable machine-readable identifier (Angular writes if/switch on this)
// - Message: human-readable description for the ProblemDetails response body
public sealed record EnrollmentError(string Code, string Message)
{
    public static EnrollmentError CourseNotFound(string code) =>
        new("course_not_found", $"Course '{code}' was not found.");

    public static EnrollmentError CourseFull(string title, int capacity) =>
        new("course_full", $"Course '{title}' is full (capacity {capacity}).");

    public static EnrollmentError AlreadyEnrolled(int studentId, string code) =>
        new("already_enrolled", $"Student {studentId} is already enrolled in {code}.");
}