using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;
using Lehrerleicht.Approval.Infrastructure.Data;

namespace Lehrerleicht.Approval.Infrastructure.Repositories;

public class SchoolRepository : ISchoolRepository
{
    private readonly ApprovalDbContext _db;

    public SchoolRepository(ApprovalDbContext db)
    {
        _db = db;
    }

    public async Task<School?> GetByIdAsync(Guid id)
    {
        return await _db.Schools
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<School>> GetAllAsync()
    {
        return await _db.Schools
            .Include(s => s.Teachers)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<School> CreateAsync(School school)
    {
        _db.Schools.Add(school);
        await _db.SaveChangesAsync();
        return school;
    }

    public async Task UpdateAsync(School school)
    {
        _db.Schools.Update(school);
        await _db.SaveChangesAsync();
    }
}
