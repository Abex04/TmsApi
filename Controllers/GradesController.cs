using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/grades")]
public class GradesController : ControllerBase
{
    public record GradePayload(int StudentId, int CourseId, decimal Score);
    public record GradeResult(string Id, bool Success);

    // Artificial 2-second delay simulates a real slow grading-service call —
    // this is what lets us actually observe exhaustMap dropping rage-clicks
    // in the Network tab instead of every click racing to completion instantly.
    [HttpPost]
    public async Task<IActionResult> PostGrade(GradePayload payload, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var id = Guid.NewGuid().ToString("N")[..10];
        return Ok(new GradeResult(id, true));
    }
}
