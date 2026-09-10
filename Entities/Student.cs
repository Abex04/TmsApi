namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;   // soft delete flag

    // Links this Student record to the TmsUser (Identity) account used to
    // log in. Nullable because pre-existing seed Students (Alice Smith,
    // Bob Jones, etc.) have no corresponding login account. Set
    // automatically at registration time for new self-registered students.
    public string? TmsUserId { get; set; }

    // Navigation property
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
