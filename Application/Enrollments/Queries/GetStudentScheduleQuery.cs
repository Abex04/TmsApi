using MediatR;

namespace TmsApi.Application.Enrollments.Queries;

// A query represents a read request — no state change, no failure modes
// worth modelling. Queries return raw DTOs, not Result<T,E>.
public record GetStudentScheduleQuery(int StudentId) : IRequest<ScheduleDto>;

// The student's full schedule — all courses they are enrolled in.
public record ScheduleDto(int StudentId, List<ScheduleItemDto> Courses);

// One course in the student's schedule.
// Schedule field is a placeholder — add Course.MeetingSummary to your
// schema when timetable data is available.
public record ScheduleItemDto(string CourseCode, string Title, string Schedule);
