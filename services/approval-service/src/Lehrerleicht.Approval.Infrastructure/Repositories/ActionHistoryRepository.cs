using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;
using Lehrerleicht.Approval.Infrastructure.Data;

namespace Lehrerleicht.Approval.Infrastructure.Repositories;

public class ActionHistoryRepository : IActionHistoryRepository
{
    private readonly ApprovalDbContext _db;

    public ActionHistoryRepository(ApprovalDbContext db)
    {
        _db = db;
    }

    public async Task<ActionHistory?> GetByIdAsync(Guid id)
    {
        return await _db.ActionHistories.FindAsync(id);
    }

    public async Task<(List<ActionHistory> Items, int TotalCount)> GetByTeacherAsync(
        string teacherId, int page, int pageSize)
    {
        var query = _db.ActionHistories
            .Where(h => h.TeacherId == teacherId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(h => h.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<ActionHistory>> GetByApprovalIdAsync(Guid approvalId)
    {
        return await _db.ActionHistories
            .Where(h => h.ApprovalId == approvalId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ActionHistory>> GetByStudentIdAsync(string studentId)
    {
        return await _db.ActionHistories
            .Include(h => h.Approval)
            .ThenInclude(a => a.Action)
            .Where(h => h.Approval.Action.StudentId == studentId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync();
    }

    public async Task<ActionHistory> CreateAsync(ActionHistory history)
    {
        _db.ActionHistories.Add(history);
        await _db.SaveChangesAsync();
        return history;
    }
}
