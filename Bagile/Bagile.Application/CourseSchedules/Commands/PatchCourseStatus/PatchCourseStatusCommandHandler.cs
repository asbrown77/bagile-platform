using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.PatchCourseStatus;

public class PatchCourseStatusCommandHandler : IRequestHandler<PatchCourseStatusCommand, bool>
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        "enquiry", "quoted", "confirmed", "completed", "cancelled",
        "planned", "publish", "draft", "sold_out", "partial_live", "live"
    ];

    private readonly ICourseScheduleRepository _courseRepo;

    public PatchCourseStatusCommandHandler(ICourseScheduleRepository courseRepo)
    {
        _courseRepo = courseRepo;
    }

    public async Task<bool> Handle(PatchCourseStatusCommand request, CancellationToken ct)
    {
        if (!AllowedStatuses.Contains(request.Status))
            throw new ArgumentException($"Invalid status '{request.Status}'.");

        await _courseRepo.UpdateStatusAsync(request.Id, request.Status);
        return true;
    }
}
