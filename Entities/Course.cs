namespace TmsApi.Entities;

public class Course
{
    public int Id { get; set; }                // surrogate primary key
    public required string Code { get; set; }   // natural key — human-readable, must be unique
    public required string Title { get; set; }
    public int MaxCapacity { get; set; }         // renamed from Capacity in Module 6 for naming convention alignment

    // M11 Session 3: nullable because existing seeded courses predate
    // this column and have no assigned instructor yet. Matches the
    // AspNetUsers.Id (TmsUser.Id) of the instructor who owns this course -
    // this is what CourseInstructorHandler compares against the caller's
    // "sub" claim to decide edit permission.
    public string? InstructorId { get; set; }

    // Navigation property — one course can have many enrollments
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
