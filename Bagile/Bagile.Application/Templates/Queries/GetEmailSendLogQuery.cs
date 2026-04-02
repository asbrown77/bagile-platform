using Bagile.Application.Templates.DTOs;
using MediatR;

namespace Bagile.Application.Templates.Queries;

/// <summary>Get the email send history for a specific course schedule.</summary>
public record GetEmailSendLogQuery(int CourseScheduleId)
    : IRequest<IEnumerable<EmailSendLogDto>>;
