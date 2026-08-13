namespace TmsApi.Application.Notifications;

// Abstraction boundary: Infrastructure (where TranscriptWorker lives) depends
// on this interface, never on SignalR directly. The concrete SignalR
// implementation lives in the outer layer where TmsHub is visible.
public interface ITranscriptNotificationService
{
    Task NotifyTranscriptReadyAsync(int studentId, string reportId, string downloadUrl);
}
