using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;

namespace Lehrerleicht.Approval.Core.Services;

public class ActionHistoryService
{
    private readonly IActionHistoryRepository _historyRepo;

    public ActionHistoryService(IActionHistoryRepository historyRepo)
    {
        _historyRepo = historyRepo;
    }

    public async Task<PagedResult<ActionHistoryDto>> GetHistoryAsync(
        string teacherId, int page, int pageSize)
    {
        var (items, total) = await _historyRepo.GetByTeacherAsync(teacherId, page, pageSize);
        return new PagedResult<ActionHistoryDto>(
            items.Select(MapToDto).ToList(), total, page, pageSize);
    }

    public async Task<ActionHistoryDto?> GetByIdAsync(Guid id)
    {
        var history = await _historyRepo.GetByIdAsync(id);
        return history is null ? null : MapToDto(history);
    }

    public async Task<List<ActionHistoryDto>> GetByStudentIdAsync(string studentId)
    {
        var items = await _historyRepo.GetByStudentIdAsync(studentId);
        return items.Select(MapToDto).ToList();
    }

    private static ActionHistoryDto MapToDto(ActionHistory h) => new(
        h.Id, h.ApprovalId, h.TeacherId, h.ActionType,
        h.Description, h.PreviousState, h.NewState, h.Timestamp
    );
}
