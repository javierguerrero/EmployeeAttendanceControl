using MediatR;

namespace ms.employees.application.Commands
{
    public record UpdateAttendanceStateCommand(string UserName, bool Attendance, string Notes) : IRequest<string>;
}
