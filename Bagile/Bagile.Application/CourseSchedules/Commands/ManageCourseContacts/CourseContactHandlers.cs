using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.ManageCourseContacts;

public class GetCourseContactsQueryHandler
    : IRequestHandler<GetCourseContactsQuery, IEnumerable<CourseContactDto>>
{
    private readonly ICourseContactRepository _repo;

    public GetCourseContactsQueryHandler(ICourseContactRepository repo) => _repo = repo;

    public async Task<IEnumerable<CourseContactDto>> Handle(
        GetCourseContactsQuery request,
        CancellationToken ct)
    {
        var contacts = await _repo.GetByCourseScheduleAsync(request.CourseScheduleId, ct);
        return contacts.Select(ToDto);
    }

    private static CourseContactDto ToDto(CourseContact c) => new()
    {
        Id = c.Id,
        CourseScheduleId = c.CourseScheduleId,
        Role = c.Role,
        Name = c.Name,
        Email = c.Email,
        Phone = c.Phone,
        CreatedAt = c.CreatedAt,
    };
}

public class AddCourseContactCommandHandler
    : IRequestHandler<AddCourseContactCommand, CourseContactDto>
{
    private readonly ICourseContactRepository _repo;

    public AddCourseContactCommandHandler(ICourseContactRepository repo) => _repo = repo;

    public async Task<CourseContactDto> Handle(
        AddCourseContactCommand request,
        CancellationToken ct)
    {
        var contact = new CourseContact
        {
            CourseScheduleId = request.CourseScheduleId,
            Role = request.Role,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
        };

        var saved = await _repo.AddAsync(contact, ct);

        return new CourseContactDto
        {
            Id = saved.Id,
            CourseScheduleId = saved.CourseScheduleId,
            Role = saved.Role,
            Name = saved.Name,
            Email = saved.Email,
            Phone = saved.Phone,
            CreatedAt = saved.CreatedAt,
        };
    }
}

public class DeleteCourseContactCommandHandler
    : IRequestHandler<DeleteCourseContactCommand, bool>
{
    private readonly ICourseContactRepository _repo;

    public DeleteCourseContactCommandHandler(ICourseContactRepository repo) => _repo = repo;

    public async Task<bool> Handle(
        DeleteCourseContactCommand request,
        CancellationToken ct)
    {
        return await _repo.DeleteAsync(request.CourseScheduleId, request.ContactId, ct);
    }
}

public class UpdateCourseContactCommandHandler
    : IRequestHandler<UpdateCourseContactCommand, CourseContactDto?>
{
    private readonly ICourseContactRepository _repo;

    public UpdateCourseContactCommandHandler(ICourseContactRepository repo) => _repo = repo;

    public async Task<CourseContactDto?> Handle(
        UpdateCourseContactCommand request,
        CancellationToken ct)
    {
        var updated = await _repo.UpdateAsync(
            request.CourseScheduleId,
            request.ContactId,
            request.Role,
            request.Name,
            request.Email,
            request.Phone,
            ct);

        if (updated is null) return null;

        return new CourseContactDto
        {
            Id               = updated.Id,
            CourseScheduleId = updated.CourseScheduleId,
            Role             = updated.Role,
            Name             = updated.Name,
            Email            = updated.Email,
            Phone            = updated.Phone,
            CreatedAt        = updated.CreatedAt,
        };
    }
}
