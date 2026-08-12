namespace TmsApi.Application.Transcripts;

// State machine: Queued -> Processing -> (Ready | Failed)
// No path back to Queued from Failed - a failed transcript needs
// a brand new POST with a fresh report id, keeping status honest.
public enum TranscriptState { Queued, Processing, Ready, Failed }

// The incoming request. ReportId starts null; the controller assigns
// one and attaches it via WithReportId before the request is queued.
public record TranscriptRequest(int StudentId, string? ReportId = null)
{
    public TranscriptRequest WithReportId(string id) => this with { ReportId = id };
}

// The full status record returned by GET /status and stored per report id.
public record TranscriptStatus(
    string ReportId,
    int StudentId,
    TranscriptState State,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    string? DownloadUrl = null,
    string? ErrorMessage = null);
