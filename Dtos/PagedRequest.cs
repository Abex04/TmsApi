namespace TmsApi.Dtos;

// The input contract for any paginated GET request.
// Bound from query-string parameters via [FromQuery] in the controller.
public record PagedRequest
{
    // Single source of truth for the page-size cap — never inline a magic number elsewhere.
    private const int MaxPageSize = 50;
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    // Clamped on both ends: a hostile client sending pageSize=10000 lands on 50;
    // a confused client sending pageSize=0 (or negative) lands on the default 20.
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 20 : value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Search { get; init; }

    // Plain string for now — the SERVICE layer decides which values are safe
    // to translate into an actual LINQ OrderBy, via a whitelist. Never trust
    // this string directly against a dynamic query.
    public string OrderBy { get; init; } = "Title";

    public bool Descending { get; init; }
}