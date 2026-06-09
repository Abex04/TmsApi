var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Validate DI lifetimes at startup
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Service registrations
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

// Register the training authentication scheme
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// Bind PaymentOptions and validate at startup
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register ProblemDetails service
builder.Services.AddProblemDetails();

var app = builder.Build();

// Middleware pipeline — order matters!
app.UseMiddleware<RequestLoggingMiddleware>(); // Outer wrapper — logs every request
app.UseExceptionHandler();                     // Catch unhandled exceptions
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();                       // Step 1: Who are you?
app.UseAuthorization();                        // Step 2: Are you allowed?

// Protected route — anonymous callers get 401
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

// Test route — intentionally throws to verify ProblemDetails shape
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

app.MapControllers();

app.Run();