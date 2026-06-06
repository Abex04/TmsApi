// Fixed version — uses IServiceScopeFactory instead of IEnrollmentService directly
// The singleton holds a factory, not a scoped service
public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        // Create a short-lived scope each time the worker runs
        using var scope = scopeFactory.CreateScope();

        // Resolve the scoped service from the new scope's provider
        var svc = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Use the service — scope disposes automatically when 'using' block ends
        Console.WriteLine("Processing batch with fresh scoped service...");
    }
}