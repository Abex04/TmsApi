using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Entities;

namespace TmsApi.Authorization;

// Resource-based authorization: role membership alone ("Instructor")
// isn't enough here - we need to check that THIS specific instructor
// owns THIS specific course before allowing an edit.
public class CourseInstructorHandler : AuthorizationHandler<CourseInstructorRequirement, Course>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseInstructorRequirement requirement,
        Course resource)
    {
        // "sub" matches the claim name TokenService actually issues
        // (see M11 Session 2 - we cleared the default claim type map,
        // so this is the literal claim name, not a long schema URI).
        var userId = context.User.FindFirstValue("sub");
        var isInstructor = context.User.IsInRole("Instructor");
        var isAdmin = context.User.IsInRole("Admin");

        // Admins can manage any course, regardless of ownership.
        if (isAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Instructors can only manage courses where InstructorId matches
        // their own user id - this is what stops Instructor A from
        // editing Instructor B's course even though both hold the same role.
        if (isInstructor && resource.InstructorId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
