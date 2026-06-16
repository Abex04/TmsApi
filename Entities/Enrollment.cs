using System;

namespace TmsApi.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }          // foreign key pointing to Student
    public int CourseId { get; set; }           // foreign key pointing to Course
    public decimal? Grade { get; set; }         // Nullable — student may be currently enrolled
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Navigation properties back to entities
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}