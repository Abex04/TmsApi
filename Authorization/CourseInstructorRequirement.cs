using Microsoft.AspNetCore.Authorization;

namespace TmsApi.Authorization;

// Marker requirement - carries no data itself. All the actual logic
// lives in CourseInstructorHandler, which receives the Course resource
// alongside this requirement.
public class CourseInstructorRequirement : IAuthorizationRequirement { }
