using TmsApi.Application.Transcripts;

namespace TmsApi.Infrastructure.Transcripts;

public interface ITranscriptStatusStore
{
    Task<TranscriptStatus> CreateAsync(string reportId, int studentId, CancellationToken ct);
    Task MarkProcessingAsync(string reportId, CancellationToken ct);
    Task MarkReadyAsync(string reportId, string downloadUrl, CancellationToken ct);
    Task MarkFailedAsync(string reportId, string error, CancellationToken ct);
    Task<TranscriptStatus?> GetAsync(string reportId, CancellationToken ct);

    // Idempotency: maps a client-supplied Idempotency-Key to the report id
    // it originally created, so a repeated POST returns the same result
    // instead of starting a second worker job.
    Task<string?> GetReportIdForIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);
    Task LinkIdempotencyKeyAsync(string idempotencyKey, string reportId, CancellationToken ct);
}
