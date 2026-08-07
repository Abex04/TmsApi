using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Dtos;
using TmsApi.Infrastructure.Caching;
using TmsApi.Services;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    CourseService service,
    ILogger<CachedCourseService> logger) : TmsApi.Services.ICourseService
{
    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var key = CacheKeys.Course(id);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            id,
            async (courseId, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);
                return await service.GetByIdAsync(courseId, token);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
        }

        return dto;
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return await service.CodeExistsAsync(code, ct);
    }

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var result = await cache.GetOrCreateAsync(
            key,
            async token =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);
                return await service.GetCoursesAsync(request, token);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
        }

        return result;
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
        logger.LogInformation("Invalidated course cache after creating {CourseCode}", request.Code);
        return result;
    }
}