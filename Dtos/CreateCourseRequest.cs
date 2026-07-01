using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

// Defines exactly what a client is allowed to send when creating a course.
// [ApiController] on the controller automatically validates incoming JSON
// against these Data Annotations, returning 400 Bad Request with field-level
// error details if any rule fails — no manual "if" checks needed.
public record CreateCourseRequest
{
    [Required]
    [RegularExpression(@"^[A-Z]{3}-\d{3}$",
        ErrorMessage = "Code must follow the pattern XXX-000 (e.g., CSE-101).")]
    public required string Code { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }

    [Range(1, 200)]
    public int MaxCapacity { get; init; }
}