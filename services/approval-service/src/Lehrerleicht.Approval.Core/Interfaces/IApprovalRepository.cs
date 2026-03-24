using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Interfaces;

public interface IApprovalRepository
{
    Task<ApprovalEntity?> GetByIdAsync(Guid id);
    Task<ApprovalEntity?> GetByIdWithDetailsAsync(Guid id);
    Task<(List<ApprovalEntity> Items, int TotalCount)> GetByTeacherAsync(
        string teacherId, int page, int pageSize, ApprovalStatus? status, Priority? priority);
    Task<ApprovalEntity> CreateAsync(ApprovalEntity approval);
    Task UpdateAsync(ApprovalEntity approval);
    Task<List<ApprovalEntity>> GetExpiredAsync();
    Task<int> CountByStatusAsync(string teacherId, ApprovalStatus status);
    Task<int> CountHighPriorityPendingAsync(string teacherId);
    Task<double> GetAverageResponseTimeAsync(string teacherId);
}
