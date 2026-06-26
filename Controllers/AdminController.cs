using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(TmsDbContext context) : ControllerBase
{
    // Soft delete a student — sets IsDeleted = true
    [HttpDelete("students/{id}")]
    public async Task<IActionResult> SoftDeleteStudent(int id, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
            return NotFound(new { Message = $"Student {id} not found" });

        student.IsDeleted = true;

        // Set the shadow property LastUpdated
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = $"Student {id} soft deleted" });
    }

    // Show ALL students including soft-deleted ones (admin only)
    [HttpGet("students")]
    public async Task<IActionResult> GetAllStudentsIncludeDeleted(CancellationToken cancellationToken)
    {
        var students = await context.Students
            .IgnoreQueryFilters()  // bypasses the HasQueryFilter
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    // Bulk archive enrollments older than a cutoff date
    [HttpPost("enrollments/archive")]
    public async Task<IActionResult> ArchiveOldEnrollments(
        [FromQuery] DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        // Convert to UTC to avoid PostgreSQL timezone issues
        var utcCutoff = DateTime.SpecifyKind(cutoffDate, DateTimeKind.Utc);

        var count = await context.Enrollments
            .Where(e => e.EnrolledAt < utcCutoff && !e.IsArchived)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.IsArchived, true),
                cancellationToken);

        return Ok(new { ArchivedCount = count, Message = $"Archived {count} enrollments" });
    }

    // Show archived enrollments
    [HttpGet("enrollments/archived")]
    public async Task<IActionResult> GetArchivedEnrollments(CancellationToken cancellationToken)
    {
        var enrollments = await context.Enrollments
            .Where(e => e.IsArchived)
            .ToListAsync(cancellationToken);

        return Ok(enrollments);
    }
}
