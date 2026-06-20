namespace TmsApi.Entities;

public class Course
{
    public int Id { get; set; }                // surrogate primary key
    public required string Code { get; set; } // natural key — human-readable
    public required string Title { get; set; }
    public int Capacity { get; set; }

    // Navigation property — one course can have many enrollments
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}