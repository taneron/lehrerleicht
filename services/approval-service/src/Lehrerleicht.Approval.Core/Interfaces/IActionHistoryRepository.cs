using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Core.Interfaces;

public interface IActionHistoryRepository
{
    Task<ActionHistory?> GetByIdAsync(Guid id);
    Task<(List<ActionHistory> Items, int TotalCount)> GetByTeacherAsync(
        string teacherId, int page, int pageSize);
    Task<List<ActionHistory>> GetByApprovalIdAsync(Guid approvalId);
    Task<List<ActionHistory>> GetByStudentIdAsync(string studentId);
    Task<ActionHistory> CreateAsync(ActionHistory history);
}
