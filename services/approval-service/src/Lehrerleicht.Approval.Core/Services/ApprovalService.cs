using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Enums;
using Lehrerleicht.Approval.Core.Interfaces;

namespace Lehrerleicht.Approval.Core.Services;

public class ApprovalService
{
    private readonly IApprovalRepository _approvalRepo;
    private readonly IActionHistoryRepository _historyRepo;
    private readonly IMessagePublisher _publisher;

    public ApprovalService(
        IApprovalRepository approvalRepo,
        IActionHistoryRepository historyRepo,
        IMessagePublisher publisher)
    {
        _approvalRepo = approvalRepo;
        _historyRepo = historyRepo;
        _publisher = publisher;
    }

    public async Task<PagedResult<ApprovalDto>> GetApprovalsAsync(
        string teacherId, int page, int pageSize, string? status, string? priority)
    {
        ApprovalStatus? statusFilter = status is not null
            ? Enum.Parse<ApprovalStatus>(status, ignoreCase: true)
            : null;
        Priority? priorityFilter = priority is not null
            ? Enum.Parse<Priority>(priority, ignoreCase: true)
            : null;

        var (items, total) = await _approvalRepo.GetByTeacherAsync(
            teacherId, page, pageSize, statusFilter, priorityFilter);

        return new PagedResult<ApprovalDto>(
            items.Select(MapToDto).ToList(), total, page, pageSize);
    }

    public async Task<ApprovalDetailDto?> GetApprovalByIdAsync(Guid id)
    {
        var approval = await _approvalRepo.GetByIdWithDetailsAsync(id);
        if (approval is null) return null;

        var history = await _historyRepo.GetByApprovalIdAsync(id);

        return new ApprovalDetailDto(
            approval.Id,
            approval.CorrelationId,
            approval.TeacherId,
            approval.Status,
            MapActionToDto(approval.Action),
            approval.CreatedAt,
            approval.ExpiresAt,
            approval.ProcessedAt,
            approval.ProcessedByDeviceType,
            approval.RejectionReason,
            approval.IsRead,
            approval.ReadAt,
            history.Select(MapHistoryToDto).ToList()
        );
    }

    public async Task<ApprovalResultDto> ApproveAsync(
        Guid id, string teacherId, ApprovalDecisionDto decision)
    {
        var approval = await _approvalRepo.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Approval {id} not found");

        if (approval.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Approval {id} is not pending (current: {approval.Status})");

        if (approval.TeacherId != teacherId)
            throw new UnauthorizedAccessException("Not authorized to process this approval");

        // Apply selected options
        if (decision.SelectedOptions is { Count: > 0 })
        {
            foreach (var optionResponse in decision.SelectedOptions)
            {
                var option = approval.Action.Options.FirstOrDefault(o => o.Id == optionResponse.OptionId)
                    ?? throw new ArgumentException($"Option {optionResponse.OptionId} not found");
                option.SelectedValueJson = optionResponse.SelectedValueJson;
            }
        }

        // Validate required options are answered
        var unanswered = approval.Action.Options
            .Where(o => o.IsRequired && string.IsNullOrEmpty(o.SelectedValueJson))
            .ToList();
        if (unanswered.Count > 0)
            throw new ArgumentException(
                $"Required options not answered: {string.Join(", ", unanswered.Select(o => o.Label))}");

        approval.Status = ApprovalStatus.Approved;
        approval.ProcessedAt = DateTime.UtcNow;
        approval.ProcessedByDeviceId = decision.DeviceId;
        approval.ProcessedByDeviceType = decision.DeviceType;

        await _approvalRepo.UpdateAsync(approval);

        await _historyRepo.CreateAsync(new ActionHistory
        {
            ApprovalId = id,
            TeacherId = teacherId,
            ActionType = ActionHistoryType.Approved,
            Description = "Approval approved by teacher",
            PreviousState = ApprovalStatus.Pending.ToString(),
            NewState = ApprovalStatus.Approved.ToString(),
            DeviceId = decision.DeviceId,
            DeviceType = decision.DeviceType
        });

        await _publisher.PublishApprovalResultAsync("approval.approved", new
        {
            correlationId = approval.CorrelationId,
            approvalId = approval.Id,
            teacherId = approval.TeacherId,
            status = "approved",
            processedAt = approval.ProcessedAt,
            action = new
            {
                type = approval.Action.Type.ToString(),
                source = approval.Action.Source.ToString(),
                payload = approval.Action.PayloadJson,
                targetSystem = approval.Action.TargetSystem
            },
            selectedOptions = approval.Action.Options
                .Where(o => o.SelectedValueJson is not null)
                .Select(o => new
                {
                    label = o.Label,
                    type = o.Type.ToString(),
                    selectedValue = o.SelectedValueJson
                })
        });

        return new ApprovalResultDto(approval.Id, approval.Status, approval.ProcessedAt.Value);
    }

    public async Task<ApprovalResultDto> RejectAsync(
        Guid id, string teacherId, RejectionDto rejection)
    {
        var approval = await _approvalRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Approval {id} not found");

        if (approval.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Approval {id} is not pending (current: {approval.Status})");

        if (approval.TeacherId != teacherId)
            throw new UnauthorizedAccessException("Not authorized to process this approval");

        approval.Status = ApprovalStatus.Rejected;
        approval.ProcessedAt = DateTime.UtcNow;
        approval.RejectionReason = rejection.Reason;
        approval.ProcessedByDeviceId = rejection.DeviceId;
        approval.ProcessedByDeviceType = rejection.DeviceType;

        await _approvalRepo.UpdateAsync(approval);

        await _historyRepo.CreateAsync(new ActionHistory
        {
            ApprovalId = id,
            TeacherId = teacherId,
            ActionType = ActionHistoryType.Rejected,
            Description = $"Approval rejected: {rejection.Reason}",
            PreviousState = ApprovalStatus.Pending.ToString(),
            NewState = ApprovalStatus.Rejected.ToString(),
            DeviceId = rejection.DeviceId,
            DeviceType = rejection.DeviceType
        });

        await _publisher.PublishApprovalResultAsync("approval.rejected", new
        {
            correlationId = approval.CorrelationId,
            approvalId = approval.Id,
            teacherId = approval.TeacherId,
            status = "rejected",
            processedAt = approval.ProcessedAt,
            rejectionReason = rejection.Reason
        });

        return new ApprovalResultDto(approval.Id, approval.Status, approval.ProcessedAt.Value);
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var approval = await _approvalRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Approval {id} not found");

        if (approval.IsRead) return;

        approval.IsRead = true;
        approval.ReadAt = DateTime.UtcNow;
        await _approvalRepo.UpdateAsync(approval);
    }

    public async Task<ApprovalStatsDto> GetStatsAsync(string teacherId)
    {
        var pending = await _approvalRepo.CountByStatusAsync(teacherId, ApprovalStatus.Pending);
        var approved = await _approvalRepo.CountByStatusAsync(teacherId, ApprovalStatus.Approved);
        var rejected = await _approvalRepo.CountByStatusAsync(teacherId, ApprovalStatus.Rejected);
        var expired = await _approvalRepo.CountByStatusAsync(teacherId, ApprovalStatus.Expired);
        var highPriority = await _approvalRepo.CountHighPriorityPendingAsync(teacherId);
        var avgResponse = await _approvalRepo.GetAverageResponseTimeAsync(teacherId);

        return new ApprovalStatsDto(pending, approved, rejected, expired, highPriority, avgResponse);
    }

    private static ApprovalDto MapToDto(ApprovalEntity a) => new(
        a.Id, a.CorrelationId, a.TeacherId, a.Status,
        MapActionToDto(a.Action),
        a.CreatedAt, a.ExpiresAt, a.ProcessedAt,
        a.IsRead, a.ReadAt
    );

    private static PendingActionDto MapActionToDto(PendingAction a) => new(
        a.Id, a.Type, a.Source, a.Priority,
        a.Title, a.Description,
        a.StudentName, a.ClassName, a.ParentName,
        a.TargetSystem,
        a.OriginalMessagePreview, a.OriginalMessageTimestamp,
        a.ConfidenceScore, a.AiReasoning,
        a.Options.OrderBy(o => o.SortOrder).Select(MapOptionToDto).ToList()
    );

    private static ActionOptionDto MapOptionToDto(ActionOption o) => new(
        o.Id, o.Type, o.Label, o.HelpText, o.IsRequired,
        o.SortOrder, o.ChoicesJson, o.SelectedValueJson
    );

    private static ActionHistoryDto MapHistoryToDto(ActionHistory h) => new(
        h.Id, h.ApprovalId, h.TeacherId, h.ActionType,
        h.Description, h.PreviousState, h.NewState, h.Timestamp
    );
}
