namespace TmsApi.Application.DTOs;

// The output contract for any paginated collection response.
// Generic over T so this same wrapper can page through any resource type,
// not just courses.
public record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }

    // Computed, not stored — always derived fresh from TotalCount and PageSize.
    // Casting to (double) before dividing avoids integer-division truncation:
    // 25 / 10 would be 2 as integers, but we need 2.5 → ceiling → 3 pages.
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}