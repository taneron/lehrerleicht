using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Core.Interfaces;

public interface ISchoolRepository
{
    Task<School?> GetByIdAsync(Guid id);
    Task<List<School>> GetAllAsync();
    Task<School> CreateAsync(School school);
    Task UpdateAsync(School school);
}
