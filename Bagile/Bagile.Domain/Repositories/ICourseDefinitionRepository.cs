using Bagile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Repositories
{
    public interface ICourseDefinitionRepository
    {
        Task<IEnumerable<CourseDefinition>> GetAllAsync();
        Task<CourseDefinition?> GetByCodeAsync(string code);
        Task UpdateBadgeUrlAsync(string code, string? badgeUrl);
        Task UpdateDurationAsync(string code, int durationDays);
        Task UpdateNameAsync(string code, string name);
        Task<CourseDefinition> CreateAsync(string code, string name, int durationDays);
        Task<IEnumerable<string>> GetAliasesAsync(string code);
        Task AddAliasAsync(string code, string alias);
        Task<bool> RemoveAliasAsync(string code, string alias);
        Task<bool> AliasExistsAsync(string alias);
    }

}
