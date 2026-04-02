using Bagile.Application.CourseSchedules.DTOs;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.ManageCourseContacts;

public record AddCourseContactCommand(
    long CourseScheduleId,
    string Role,
    string Name,
    string Email,
    string? Phone
) : IRequest<CourseContactDto>;

public record DeleteCourseContactCommand(
    long CourseScheduleId,
    long ContactId
) : IRequest<bool>;

public record UpdateCourseContactCommand(
    long CourseScheduleId,
    long ContactId,
    string Role,
    string Name,
    string Email,
    string? Phone
) : IRequest<CourseContactDto?>;

public record GetCourseContactsQuery(
    long CourseScheduleId
) : IRequest<IEnumerable<CourseContactDto>>;
