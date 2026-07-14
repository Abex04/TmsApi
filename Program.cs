using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Services;
using TmsApi.Filters;
using Asp.Versioning;
using TmsApi.Middleware;
var builder = WebApplication.CreateBuilder(args);

// Add<T>() (generic overload) lets DI construct AuditLogFilter itself,
// automatically resolving ILogger<AuditLogFilter> — no manual construction needed.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICourseEnrollmentService, CourseEnrollmentService>();
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddProblemDetails();
// Separate OpenApi documents for V1 and V2 — Scalar shows them as a dropdown
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
});

// API versioning — URL segment style (/api/v1/..., /api/v2/...)
builder.Services.AddApiVersioning(options =>
{
    // Default to V1 if no version is specified in the URL
    options.DefaultApiVersion = new ApiVersion(1, 0);
    // Accept unversioned URLs while clients are migrating
    options.AssumeDefaultVersionWhenUnspecified = true;
    // Add "api-supported-versions: 1.0, 2.0" to every response header
    options.ReportApiVersions = true;
    // Version lives in the URL path, not a header or query string
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    // Format: "v1", "v2" — used by Scalar to name the document groups
    options.GroupNameFormat = "'v'VVV";
    // Replaces {version} in route templates automatically
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
   app.MapOpenApi("/openapi/v1.json").CacheOutput();
app.MapOpenApi("/openapi/v2.json").CacheOutput();
    app.MapScalarApiReference();
}

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

app.MapControllers();
// Stamp Deprecation, Sunset, and Link headers on every V1 response.
// Must be registered before MapControllers() so it wraps controller execution.
app.UseMiddleware<V1DeprecationMiddleware>();
app.MapControllers();

// Seed deterministic demo data, but only in Development.
// Staging and production data belongs to the operations team, not this seed file.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

app.Run();