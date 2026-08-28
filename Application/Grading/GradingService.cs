namespace TmsApi.Application.Grading;

// Pure mapping logic - no database, no side effects. This is exactly the
// kind of code unit tests are cheapest and most valuable for: fast,
// deterministic, and pins the business rule so a future refactor can't
// silently invert a comparison (e.g. > becoming <) without a test failing.
public class GradingService
{
    public const decimal DistinctionThreshold = 70m;
    public const decimal PassThreshold = 50m;

    // Pure mapping: one score against one maximum.
    public GradeLevel CalculateLetterGrade(decimal score, decimal maxScore)
    {
        if (maxScore <= 0m || score < 0m || score > maxScore)
            return GradeLevel.Invalid;

        var pct = score / maxScore * 100m;

        return pct >= DistinctionThreshold ? GradeLevel.Distinction
            : pct >= PassThreshold ? GradeLevel.Pass
            : GradeLevel.Fail;
    }

    // Single-decimal path: maps an Enrollment.Grade percentage to a GradeLevel.
    // Enrollment.Grade is nullable (decimal?) - null means no grade recorded yet.
    public GradeLevel CalculateFromEnrollmentGrade(decimal? enrollmentGradePercent)
    {
        if (enrollmentGradePercent is null) return GradeLevel.Invalid;
        return CalculateLetterGrade(enrollmentGradePercent.Value, maxScore: 100m);
    }
}
