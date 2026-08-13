using Microsoft.AspNetCore.SignalR;
using TmsApi.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Application.Notifications;

namespace TmsApi.Notifications;

public class SignalRTranscriptNotificationService(IHubContext<TmsHub, ITmsHubClient> hubContext)
    : ITranscriptNotificationService
{
    public async Task NotifyTranscriptReadyAsync(int studentId, string reportId, string downloadUrl)
    {
        // Sent to the student's own group only - never Clients.All. This is
        // what keeps one student's transcript notification from leaking to
        // every other connected client.
        await hubContext.Clients
            .Group(GroupNames.Student(studentId.ToString()))
            .ReceiveTranscriptReady(reportId, downloadUrl);
    }
}
