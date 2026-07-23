using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

// Query handler — reads enrollments for a student and projects them
// into a ScheduleDto. No business rules, no Result<T,E> — just a read.
public class GetStudentScheduleHandler(IEnrollmentService repo)
    : IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    public async Task<ScheduleDto> Handle(
        GetStudentScheduleQuery query, CancellationToken ct)
    {
        // Fetch all enrollments for this student, including Course details
        // so we can project the course code and title into the DTO.
        var enrollments = await repo.GetByStudentIdAsync(query.StudentId, ct);

        var items = enrollments.Select(e => new ScheduleItemDto(
            e.Course.Code,
            e.Course.Title,
            "TBD")).ToList();

        return new ScheduleDto(query.StudentId, items);
    }
}