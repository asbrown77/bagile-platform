using Bagile.Application.Analytics.DTOs;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetCourseDemand;

public record GetCourseDemandQuery(int Months = 12) : IRequest<CourseDemandResultDto>;
