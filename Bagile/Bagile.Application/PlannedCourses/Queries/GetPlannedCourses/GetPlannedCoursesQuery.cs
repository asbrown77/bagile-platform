using MediatR;
using Bagile.Application.PlannedCourses.DTOs;

namespace Bagile.Application.PlannedCourses.Queries.GetPlannedCourses;

public record GetPlannedCoursesQuery
    : IRequest<IEnumerable<PlannedCourseDto>>;
