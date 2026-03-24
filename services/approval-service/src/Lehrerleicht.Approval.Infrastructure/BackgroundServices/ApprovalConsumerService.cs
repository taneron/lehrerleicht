using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Enums;
using Lehrerleicht.Approval.Core.Interfaces;
using Lehrerleicht.Approval.Infrastructure.Messaging;
using Lehrerleicht.Approval.Infrastructure.Messaging.Messages;

namespace Lehrerleicht.Approval.Infrastructure.BackgroundServices;

public class ApprovalConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConsumer _consumer;
    private readonly ILogger<ApprovalConsumerService> _logger;

    public ApprovalConsumerService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConsumer consumer,
        ILogger<ApprovalConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.OnMessageReceived += HandleMessageAsync;

        try
        {
            await _consumer.StartAsync(stoppingToken);

            // Keep alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Approval consumer service stopping");
        }
    }

    private async Task HandleMessageAsync(PendingApprovalMessage message)
    {
        using var scope = _scopeFactory.CreateScope();
        var approvalRepo = scope.ServiceProvider.GetRequiredService<IApprovalRepository>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IActionHistoryRepository>();

        try
        {
            var approval = new ApprovalEntity
            {
                CorrelationId = Guid.Parse(message.CorrelationId),
                TeacherId = message.TeacherId,
                Status = ApprovalStatus.Pending,
                ExpiresAt = DateTime.Parse(message.ExpiresAt).ToUniversalTime(),
                Action = new PendingAction
                {
                    Type = Enum.Parse<ActionType>(message.Action.Type, ignoreCase: true),
                    Source = Enum.Parse<ActionSource>(message.Action.Source, ignoreCase: true),
                    Priority = Enum.Parse<Priority>(message.Action.Priority, ignoreCase: true),
                    Title = message.Action.Title,
                    Description = message.Action.Description,
                    StudentId = message.Action.StudentId,
                    StudentName = message.Action.StudentName,
                    ClassName = message.Action.ClassName,
                    ParentName = message.Action.ParentName,
                    PayloadJson = message.Action.Payload,
                    TargetSystem = message.Action.TargetSystem,
                    OriginalMessageId = message.Action.OriginalMessageId,
                    OriginalMessagePreview = message.Action.OriginalMessagePreview,
                    OriginalMessageTimestamp = message.Action.OriginalMessageTimestamp is not null
                        ? DateTime.Parse(message.Action.OriginalMessageTimestamp).ToUniversalTime()
                        : null,
                    ConfidenceScore = message.Action.ConfidenceScore,
                    AiReasoning = message.Action.AiReasoning,
                    Options = message.Action.Options?.Select((o, i) => new ActionOption
                    {
                        Type = Enum.Parse<ActionOptionType>(o.Type, ignoreCase: true),
                        Label = o.Label,
                        HelpText = o.HelpText,
                        IsRequired = o.IsRequired,
                        SortOrder = o.SortOrder,
                        ChoicesJson = o.Choices is not null
                            ? JsonSerializer.Serialize(o.Choices)
                            : null
                    }).ToList() ?? new List<ActionOption>()
                }
            };

            await approvalRepo.CreateAsync(approval);

            await historyRepo.CreateAsync(new ActionHistory
            {
                ApprovalId = approval.Id,
                ActionType = ActionHistoryType.Created,
                Description = $"Approval created for: {message.Action.Title}",
                NewState = ApprovalStatus.Pending.ToString()
            });

            _logger.LogInformation(
                "Created approval {ApprovalId} for teacher {TeacherId}",
                approval.Id, message.TeacherId);

            // TODO: Send SignalR notification via hub context
            // TODO: Send push notification
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process pending approval for correlation {CorrelationId}",
                message.CorrelationId);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
