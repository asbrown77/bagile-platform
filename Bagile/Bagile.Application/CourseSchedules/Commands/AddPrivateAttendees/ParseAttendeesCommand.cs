using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.AddPrivateAttendees;

public record ParseAttendeesCommand(string RawText) : IRequest<List<AttendeeInput>>;

public class ParseAttendeesCommandHandler
    : IRequestHandler<ParseAttendeesCommand, List<AttendeeInput>>
{
    public Task<List<AttendeeInput>> Handle(
        ParseAttendeesCommand request,
        CancellationToken ct)
    {
        var result = new List<AttendeeInput>();

        if (string.IsNullOrWhiteSpace(request.RawText))
            return Task.FromResult(result);

        var lines = request.RawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l));

        foreach (var line in lines)
        {
            var parts = line.Contains('\t')
                ? line.Split('\t')
                : line.Split(',');

            parts = parts.Select(p => p.Trim()).ToArray();

            if (parts.Length >= 3)
            {
                // FirstName, LastName, Email [, Company] [, Country]
                result.Add(new AttendeeInput
                {
                    FirstName = parts[0],
                    LastName = parts[1],
                    Email = parts[2],
                    Company = parts.Length > 3 ? parts[3] : null,
                    Country = parts.Length > 4 ? parts[4] : null
                });
            }
            else if (parts.Length == 2)
            {
                // "Full Name, email" or "Full Name\temail"
                var nameParts = parts[0].Split(' ', 2);
                result.Add(new AttendeeInput
                {
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : "",
                    Email = parts[1]
                });
            }
        }

        return Task.FromResult(result);
    }
}
