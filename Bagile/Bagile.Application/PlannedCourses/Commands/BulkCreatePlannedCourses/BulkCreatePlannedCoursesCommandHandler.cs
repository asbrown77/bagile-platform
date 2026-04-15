using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.BulkCreatePlannedCourses;

public class BulkCreatePlannedCoursesCommandHandler
    : IRequestHandler<BulkCreatePlannedCoursesCommand, BulkCreatePlannedCoursesResult>
{
    private readonly IPlannedCourseRepository _repo;

    public BulkCreatePlannedCoursesCommandHandler(IPlannedCourseRepository repo)
    {
        _repo = repo;
    }

    public async Task<BulkCreatePlannedCoursesResult> Handle(
        BulkCreatePlannedCoursesCommand request,
        CancellationToken ct)
    {
        var results = new List<BulkRowResult>();

        for (var i = 0; i < request.Courses.Count; i++)
        {
            var row = request.Courses[i];
            var validationError = Validate(row, i);
            if (validationError != null)
            {
                results.Add(new BulkRowResult { Index = i, Success = false, Error = validationError });
                continue;
            }

            try
            {
                var id = await _repo.InsertAsync(ToEntity(row));
                results.Add(new BulkRowResult { Index = i, Success = true, Id = id });
            }
            catch (Exception ex)
            {
                results.Add(new BulkRowResult { Index = i, Success = false, Error = ex.Message });
            }
        }

        return new BulkCreatePlannedCoursesResult { Results = results };
    }

    private static string? Validate(BulkCourseRow row, int index)
    {
        if (string.IsNullOrWhiteSpace(row.CourseType))
            return $"Row {index}: courseType is required";
        if (row.TrainerId <= 0)
            return $"Row {index}: trainerId is required";
        if (row.StartDate == default)
            return $"Row {index}: startDate is required";
        if (row.EndDate == default)
            return $"Row {index}: endDate is required";
        if (row.EndDate < row.StartDate)
            return $"Row {index}: endDate must be on or after startDate";
        return null;
    }

    private static PlannedCourse ToEntity(BulkCourseRow row) => new()
    {
        CourseType = row.CourseType.ToUpperInvariant(),
        TrainerId = row.TrainerId,
        StartDate = row.StartDate,
        EndDate = row.EndDate,
        IsVirtual = row.IsVirtual,
        Venue = row.Venue,
        Notes = row.Notes,
        DecisionDeadline = row.DecisionDeadline,
        IsPrivate = row.IsPrivate,
        Status = "planned",
    };
}
