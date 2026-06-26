using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    // Exercise 2 - Deferred execution experiment
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList();

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");

        return Ok(results);
    }

    // Exercise 2 - Translation failure experiment
    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA))
                .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    // Exercise 7 Part A - Intentional N+1 (bad pattern — for learning only)
    [HttpGet("n-plus-one")]
    public async Task<IActionResult> TestNPlusOne(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> N+1 DEMO: Loading students then querying each one separately...");

        // 1 query to load all students
        var students = await context.Students
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var s in students)
        {
            // 1 query PER student = N extra queries!
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, cancellationToken);

            Console.WriteLine($"{s.Name}: {count} enrollments");
        }

        Console.WriteLine(">>> N+1 DEMO: Done — check how many SQL queries ran!\n");
        return Ok("Check terminal for N+1 query count");
    }

    // Exercise 7 Part B - Fixed with single projection query (good pattern)
    [HttpGet("n-plus-one-fix")]
    public async Task<IActionResult> TestNPlusOneFix(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> FIX DEMO: Loading everything in ONE query...");

        // Single query — EF translates Count to a SQL subquery
        var report = await context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        foreach (var r in report)
            Console.WriteLine($"{r.Name}: {r.EnrollmentCount} enrollments");

        Console.WriteLine(">>> FIX DEMO: Done — only 1 SQL query ran!\n");
        return Ok(report);
    }

    private static bool IsHonorRoll(decimal gpa) => gpa >= 3.5m;
}
