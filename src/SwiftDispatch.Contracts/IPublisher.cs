namespace SwiftDispatch.Contracts;

public interface IPublisher
{
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
