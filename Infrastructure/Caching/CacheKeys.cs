namespace TmsApi.Infrastructure.Caching;

public static class CacheKeys
{
    private const string SchemaVersion = "v2";

    public static string Course(int id) => $"{SchemaVersion}:course:{id}";

    public const string CoursesAll = "v2:courses:all";

    public const string CoursesTag = "courses";
}