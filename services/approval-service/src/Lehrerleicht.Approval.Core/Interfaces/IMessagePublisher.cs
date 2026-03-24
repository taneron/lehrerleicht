namespace Lehrerleicht.Approval.Core.Interfaces;

public interface IMessagePublisher
{
    Task PublishApprovalResultAsync(string routingKey, object message);
}
