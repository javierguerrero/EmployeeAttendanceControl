using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ms.employees.domain.Repositories;

namespace ms.employees.application.Commands.Handlers
{
    public class UpdateAttendanceStateCommandHandler : IRequestHandler<UpdateAttendanceStateCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public UpdateAttendanceStateCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<string> Handle(UpdateAttendanceStateCommand request, CancellationToken cancellationToken) 
        {
            var res = await _employeeRepository.UpdateAttendanceStateEmployee(request.UserName, request.Attendance, request.Notes);
            return res;
        }
    }
}
