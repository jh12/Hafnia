namespace Hafnia;

public interface IActorBridge
{
    void Tell(object message);
    Task<T> Ask<T>(object message, CancellationToken cancellationToken = default);
}
