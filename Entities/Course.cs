namespace TmsApi.Entities;

public class Course
{
    public int Id { get; set; }                // surrogate primary key
    public required string Code { get; set; }   // natural key — human-readable, must be unique
    public required string Title { get; set; }
    public int MaxCapacity { get; set; }         // renamed from Capacity in Module 6 for naming convention alignment

    // Navigation property — one course can have many enrollments
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
