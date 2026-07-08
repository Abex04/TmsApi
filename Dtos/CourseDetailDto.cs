namespace TmsApi.Dtos;

// The detail shape returned by GET /api/courses/{id}.
// Includes everything CourseResponseDto has, PLUS a Links array —
// telling the client what actions are available for this specific course.
// The list/page endpoint keeps using plain CourseResponseDto (no links per item —
// a 50-item list with 5 links each would be mostly noise).
public record CourseDetailDto
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required int MaxCapacity { get; init; }
    public required int EnrollmentCount { get; init; }
    public required IReadOnlyList<LinkDto> Links { get; init; }
}