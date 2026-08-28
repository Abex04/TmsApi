using System.Net;
using System.Net.Http.Json;

namespace TmsApi.Tests;

public class CoursesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CoursesApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCourses_ReturnsOkAndPagedJson()
    {
        // Act - pin the V2 URL, since V1 and V2 return different envelope
        // shapes (see the versioning note in the PDF).
        var response = await _client.GetAsync("/api/v2.0/courses?page=1&pageSize=10");

        // Assert - check HTTP status 200 OK
        response.EnsureSuccessStatusCode();

        // V2's envelope wraps rows in "data", not "items" - matches
        // Controllers/V2/CoursesController.cs's actual shape.
        var page = await response.Content.ReadFromJsonAsync<V2PagedCoursesJson>();
        Assert.NotNull(page?.Data);
    }

    [Fact]
    public async Task CreateCourse_InvalidCode_ReturnsValidationError()
    {
        // Act - POST to the unversioned /api/courses controller, which is
        // where course creation actually lives in this codebase (V1/V2
        // CoursesController only expose GET and PUT).
        var response = await _client.PostAsJsonAsync("/api/courses", new
        {
            code = "",
            title = "Intro to TMS Security",
            maxCapacity = 30
        });

        // Assert - validation failure returns 400 Bad Request
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity);
    }

    private sealed class V2PagedCoursesJson
    {
        public List<CourseRowJson> Data { get; set; } = default!;
    }

    private sealed class CourseRowJson
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public int MaxCapacity { get; set; }
        public int EnrollmentCount { get; set; }
    }
}
