using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.ExceptionHandlers;
using TmsApi.Filters;
using TmsApi.Middleware;
using TmsApi.Infrastructure.Services;
using TmsApi.Services;
using MediatR;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Hubs;
using TmsApi.Application.Notifications;
using TmsApi.Notifications;

var builder = WebApplication.CreateBuilder(args);

// MediatR — scans the assembly containing EnrollStudentHandler for all
// IRequestHandler implementations and registers them automatically.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

// FluentValidation — scans for all AbstractValidator<T> implementations
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// CORS — allows the Angular dev server (and file:// test pages, whose
// origin is "null") to connect to the API and the SignalR hub.
// M10 Session 1: Load allowed origins from appsettings.Development.json
// instead of hardcoding them in C# source - this is what makes production
// deployment painless later (staging/prod just get a different config value,
// no code change needed).
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

// Named CORS policy "TmsClient" - replaces the M7 catch-all "Dev" policy.
// SECURITY NOTE: never combine AllowAnyOrigin() with AllowCredentials().
// ASP.NET Core throws InvalidOperationException at startup if you try -
// the browser spec forbids wildcard origins with credentialed requests,
// since that would let any malicious site make authenticated calls
// against a logged-in user's session.
builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Vital for HttpOnly auth cookies in Session 2
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Pipeline behaviors — ORDER MATTERS:
// LoggingBehavior FIRST so it wraps ValidationBehavior.
// Reverse order = validation failures appear as silent dead air in logs.
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Global exception handler — translates ValidationException → 400,
// unexpected exceptions → 500, both as RFC 7807 ProblemDetails.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICourseEnrollmentService, CourseEnrollmentService>();

// Transcript status store — singleton so the in-memory dictionary persists
// across requests for the lifetime of the app.
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();

// Bounded channel — the queue between TranscriptsController (writer) and
// TranscriptWorker (reader). Capped at 100 pending items; Wait mode means
// new writes pause rather than drop requests if the queue fills up.
builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait }));

// Background worker that processes queued transcript requests off the
// HTTP request thread — this is what lets the controller return 202 instantly.
builder.Services.AddHostedService<TranscriptWorker>();

// SignalR — real-time push. TmsHub handles connections; the
// notification service is the abstraction TranscriptWorker calls
// into, so Infrastructure never references SignalR directly.
builder.Services.AddSignalR();
builder.Services.AddSingleton<ITranscriptNotificationService, SignalRTranscriptNotificationService>();

// REGISTER HYBRID CACHE
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

// REGISTER THE CACHED COURSE SERVICE
builder.Services.AddScoped<TmsApi.Services.ICourseService, CachedCourseService>();

// Register Application-layer interfaces so CQRS handlers can resolve them via DI
builder.Services.AddScoped<TmsApi.Application.Interfaces.ICourseService, CourseService>();
builder.Services.AddScoped<TmsApi.Application.Interfaces.IEnrollmentService, CourseEnrollmentService>();

builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// M10 Session 2: Antiforgery service generates XSRF tokens for
// state-changing requests. HeaderName matches Angular's built-in
// XSRF convention (withXsrfConfiguration on the frontend expects this
// exact header name).
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

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

// Global exception handler must be registered before MapControllers
app.UseExceptionHandler();

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
// M10 Session 1 Part B: named CORS policy re-enabled. Middleware order
// matters here: UseRouting -> UseCors -> UseAuthentication -> UseAuthorization.
app.UseCors("TmsClient");
app.UseAuthentication();
app.UseAuthorization();

// M10 Session 2: issue a readable XSRF-TOKEN cookie for any request that
// carries our auth cookie. Angular JavaScript reads this cookie and echoes
// it back as the X-XSRF-TOKEN header on mutating requests - a malicious
// external site cannot read cookies across origins under SOP, so it cannot
// forge that header, which is what defeats CSRF.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || context.Request.Cookies.ContainsKey("tms_auth"))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false, // MUST be false - Angular JavaScript needs to read this one.
                Secure = !builder.Environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict
            });
    }
    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/v1.json").CacheOutput();
    app.MapOpenApi("/openapi/v2.json").CacheOutput();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        // Show both V1 and V2 as separate documents in Scalar's sidebar dropdown
        options
            .AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
    });
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

// Stamp Deprecation, Sunset, and Link headers on every V1 response.
// Must be registered before MapControllers() so it wraps controller execution.
app.UseMiddleware<V1DeprecationMiddleware>();

app.MapControllers();

// SignalR hub endpoint — clients connect here (with ?studentId=X to auto-join their group).
app.MapHub<TmsHub>("/hubs/tms");

// Seed deterministic demo data, but only in Development.
// Staging and production data belongs to the operations team, not this seed file.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

app.Run();