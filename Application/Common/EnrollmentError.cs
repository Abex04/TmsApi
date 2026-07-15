namespace TmsApi.Application.Common;

// Represents the different ways an enrollment business operation can fail.
// Each static factory method produces a named, typed error with:
// - Code: stable machine-readable identifier (Angular writes if/switch on this)
// - Message: human-readable description for the ProblemDetails response body
// Using a typed error instead of throwing exceptions means "course is full"
// is treated as an expected outcome, not a bug — keeping stack traces clean.
public sealed record EnrollmentError(string Code, string Message)
{
    // Course with the given code does not exist in the database
    public static EnrollmentError CourseNotFound(string code) =>
        new("course_not_found", $"Course '{code}' was not found.");

    // Course exists but has no remaining capacity
    public static EnrollmentError CourseFull(string title, int capacity) =>
        new("course_full", $"Course '{title}' is full (capacity {capacity}).");

    // Student is already enrolled in this course
    public static EnrollmentError AlreadyEnrolled(int studentId, string code) =>
        new("already_enrolled", $"Student {studentId} is already enrolled in {code}.");
}