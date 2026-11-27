namespace TrueMediator.Contracts;

public interface IPublisher
{
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
