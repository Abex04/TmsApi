namespace TmsApi.Dtos;

// The shape of a Course as it appears to API clients (the "wire" format).
// This is intentionally different from the Course entity:
// - No Enrollments navigation property (avoids circular-reference JSON crashes)
// - Just a simple EnrollmentCount number instead
// A record is used because DTOs are immutable snapshots of data, not
// mutable objects — once created, a response DTO's values should never change.
public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount);