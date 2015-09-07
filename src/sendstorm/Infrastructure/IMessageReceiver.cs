namespace Sendstorm.Infrastructure
{
    public interface IMessageReceiver<TMessage>
    {
        void Receive(TMessage message);
    }
}
