using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

// Wraps ICourseService with HybridCache to prevent cache stampedes.
// Uses GetOrCreateAsync with a state parameter to avoid closure allocations
// on a hot path — important at 1,000 req/min.
// Hit/miss observability: sets a flag inside the factory (which only runs
// on miss) and reads it after GetOrCreateAsync returns.
public class CachedCourseService(
    HybridCache cache,
    ICourseService service,
    ILogger<CachedCourseService> logger) : ICachedCourseService
{
    public async Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        // GetOrCreateAsync is atomic — if 50 requests arrive simultaneously
        // on a cold cache, only ONE fires the factory. The other 49 wait
        // for that single result. This is stampede protection.
        var list = await cache.GetOrCreateAsync(
            key,
            service,
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} — fetching from DB", key);
                var courses = await state.GetCoursesAsync(
                    new TmsApi.Application.DTOs.PagedRequest { PageSize = 50 }, token);
                return courses.Items.ToList();
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
            logger.LogInformation("Cache HIT for {Key}", key);

        return list;
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        // Removes ALL entries tagged with "courses" — both list and detail keys.
        logger.LogInformation("Invalidating cache tag {Tag}", CacheKeys.CoursesTag);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
    }
}