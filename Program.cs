var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Register the training authentication scheme and authorization services
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        TrainingAuthHandler>("Training", null);

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