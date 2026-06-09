var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Validate DI lifetimes at startup — catches captive dependency bugs early
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Buggy registration — singleton holding a scoped service
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

// Register the training authentication scheme and authorization services
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        TrainingAuthHandler>("Training", null);
        // Bind PaymentOptions to the "Payments" section and validate at startup
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware pipeline — order matters!
app.UseMiddleware<RequestLoggingMiddleware>(); // Outer wrapper — logs every request
app.UseExceptionHandler("/error");             // Catch unhandled exceptions
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

app.MapControllers();

app.Run();