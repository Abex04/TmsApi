using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Hubs;

namespace TmsApi.Hubs;

public class TmsHub : Hub<ITmsHubClient>
{
    // Auto-joins the connecting client to their own "student-{id}" group,
    // using studentId from the connection query string. This is the
    // pre-auth stand-in for Context.UserIdentifier, which lands once
    // JWT auth is added later in the curriculum.
    public override async Task OnConnectedAsync()
    {
        var studentId = Context.GetHttpContext()?.Request.Query["studentId"].ToString();
        if (!string.IsNullOrWhiteSpace(studentId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Student(studentId));
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinCourseGroup(string courseCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Course(courseCode));
    }

    public async Task LeaveCourseGroup(string courseCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Course(courseCode));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR removes the connection from all groups automatically.
        await base.OnDisconnectedAsync(exception);
    }
}

// Centralising group-name formatting here means a typo like "studnet-"
// gets caught in code review, not in silent production failure.
public static class GroupNames
{
    public static string Student(string studentId) => $"student-{studentId}";
    public static string Course(string courseCode) => $"course-{courseCode}";
}
