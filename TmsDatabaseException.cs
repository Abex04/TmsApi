/// <summary>
/// Simulates a database failure for ProblemDetails testing.
/// Will be replaced with real database exceptions in Module 5.
/// </summary>
public class TmsDatabaseException(string message) : Exception(message);