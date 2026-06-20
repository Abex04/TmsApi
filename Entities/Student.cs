namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; }                    // surrogate primary key
    public required string RegistrationNumber { get; set; } // natural key — human-readable
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property — one student can have many enrollments
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}