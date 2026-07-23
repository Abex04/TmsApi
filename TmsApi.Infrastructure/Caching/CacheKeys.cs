namespace TmsApi.Infrastructure.Caching;

// Central cache key definitions — single source of truth for all cache keys.
// Embedding SchemaVersion means bumping it to "v3" instantly makes all
// "v2" cache entries unreachable — no coordinated flush needed on deploy.
public static class CacheKeys
{
    private const string SchemaVersion = "v2";

    // Key for a single course by code
    public static string Course(string code) => $"{SchemaVersion}:course:{code}";

    // Key for the full courses list
    public static string CoursesAll => $"{SchemaVersion}:courses:all";

    // Tag used to invalidate ALL course cache entries at once
    public const string CoursesTag = "courses";
}
