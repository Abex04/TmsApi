namespace TmsApi.Application.Hubs;

// Strongly-typed hub client contract. Because TmsHub extends Hub<ITmsHubClient>,
// every method here becomes available on Clients.All, Clients.Group(...), etc.
// with full compile-time type safety - no magic strings for event names.
public interface ITmsHubClient
{
    Task ReceiveTranscriptReady(string reportId, string downloadUrl);
    Task ReceiveCourseUpdate(string courseCode, string message);
    Task ReceiveGradePosted(string courseCode, int studentId, decimal grade);
}
