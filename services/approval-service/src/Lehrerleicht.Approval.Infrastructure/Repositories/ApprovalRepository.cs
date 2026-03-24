using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Enums;
using Lehrerleicht.Approval.Core.Interfaces;
using Lehrerleicht.Approval.Infrastructure.Data;

namespace Lehrerleicht.Approval.Infrastructure.Repositories;

public class ApprovalRepository : IApprovalRepository
{
    private readonly ApprovalDbContext _db;

    public ApprovalRepository(ApprovalDbContext db)
    {
        _db = db;
    }

    public async Task<ApprovalEntity?> GetByIdAsync(Guid id)
    {
        return await _db.Approvals
            .Include(a => a.Action)
            .ThenInclude(pa => pa.Options)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<ApprovalEntity?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _db.Approvals
            .Include(a => a.Action)
            .ThenInclude(pa => pa.Options)
            .Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<(List<ApprovalEntity> Items, int TotalCount)> GetByTeacherAsync(
        string teacherId, int page, int pageSize, ApprovalStatus? status, Priority? priority)
    {
        var query = _db.Approvals
            .Include(a => a.Action)
            .ThenInclude(pa => pa.Options)
            .Where(a => a.TeacherId == teacherId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(a => a.Action.Priority == priority.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<ApprovalEntity> CreateAsync(ApprovalEntity approval)
    {
        _db.Approvals.Add(approval);
        await _db.SaveChangesAsync();
        return approval;
    }

    public async Task UpdateAsync(ApprovalEntity approval)
    {
        _db.Approvals.Update(approval);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ApprovalEntity>> GetExpiredAsync()
    {
        return await _db.Approvals
            .Where(a => a.Status == ApprovalStatus.Pending && a.ExpiresAt <= DateTime.UtcNow)
            .Include(a => a.Action)
            .ToListAsync();
    }

    public async Task<int> CountByStatusAsync(string teacherId, ApprovalStatus status)
    {
        return await _db.Approvals
            .CountAsync(a => a.TeacherId == teacherId && a.Status == status);
    }

    public async Task<int> CountHighPriorityPendingAsync(string teacherId)
    {
        return await _db.Approvals
            .CountAsync(a => a.TeacherId == teacherId
                && a.Status == ApprovalStatus.Pending
                && (a.Action.Priority == Priority.High || a.Action.Priority == Priority.Urgent));
    }

    public async Task<double> GetAverageResponseTimeAsync(string teacherId)
    {
        var processedApprovals = await _db.Approvals
            .Where(a => a.TeacherId == teacherId && a.ProcessedAt != null)
            .Select(a => new { a.CreatedAt, a.ProcessedAt })
            .ToListAsync();

        if (processedApprovals.Count == 0) return 0;

        return processedApprovals
            .Average(a => (a.ProcessedAt!.Value - a.CreatedAt).TotalMinutes);
    }
}
