namespace TmsApi.Dtos;

// The safe wire-format shape of an Enrollment as returned to API clients.
public record EnrollmentResponseDto(
    int Id,
    int CourseId,
    int StudentId,
    DateTime EnrolledAt);