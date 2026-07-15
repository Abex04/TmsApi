using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

// Validates EnrollStudentCommand before it reaches the handler.
// Registered automatically via AddValidatorsFromAssembly and run
// by ValidationBehavior in the MediatR pipeline.
// Failures here throw ValidationException → GlobalExceptionHandler → 400.
public class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        // StudentId must be a positive integer — 0 or negative makes no sense
        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("Student ID must be a positive number.");

        // CourseCode must be present
        RuleFor(x => x.CourseCode)
            .NotEmpty()
            .WithMessage("Course code is required.");

        // CourseCode must follow the TMS format XXX-000 (e.g. CSE-101)
        RuleFor(x => x.CourseCode)
            .Matches(@"^[A-Z]{3}-\d{3}$")
            .WithMessage("Course code must follow the format XXX-000 (e.g., CSE-101).");
    }
}
