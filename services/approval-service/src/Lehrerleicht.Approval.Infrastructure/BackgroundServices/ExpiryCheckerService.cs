using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Enums;
using Lehrerleicht.Approval.Core.Interfaces;

namespace Lehrerleicht.Approval.Infrastructure.BackgroundServices;

public class ExpiryCheckerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiryCheckerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public ExpiryCheckerService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiryCheckerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredApprovalsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for expired approvals");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckExpiredApprovalsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var approvalRepo = scope.ServiceProvider.GetRequiredService<IApprovalRepository>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IActionHistoryRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var expired = await approvalRepo.GetExpiredAsync();

        foreach (var approval in expired)
        {
            approval.Status = ApprovalStatus.Expired;
            approval.ProcessedAt = DateTime.UtcNow;
            await approvalRepo.UpdateAsync(approval);

            await historyRepo.CreateAsync(new ActionHistory
            {
                ApprovalId = approval.Id,
                ActionType = ActionHistoryType.Expired,
                Description = "Approval expired",
                PreviousState = ApprovalStatus.Pending.ToString(),
                NewState = ApprovalStatus.Expired.ToString()
            });

            await publisher.PublishApprovalResultAsync("approval.expired", new
            {
                correlationId = approval.CorrelationId,
                approvalId = approval.Id,
                teacherId = approval.TeacherId,
                status = "expired",
                processedAt = approval.ProcessedAt
            });

            _logger.LogInformation("Expired approval {ApprovalId}", approval.Id);
        }

        if (expired.Count > 0)
        {
            _logger.LogInformation("Expired {Count} approvals", expired.Count);
        }
    }
}
