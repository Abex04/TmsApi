using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

// What a client sends to enroll a student into a course.
// The courseId itself comes from the URL route, not this body —
// this DTO only needs to say WHO is enrolling.
public record EnrollStudentRequest
{
    // Must be a positive integer — protects against 0 or negative StudentIds.
    [Range(1, int.MaxValue, ErrorMessage = "StudentId must be a positive integer.")]
    public required int StudentId { get; init; }
}