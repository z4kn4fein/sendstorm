namespace Sendstorm.Infrastructure
{
    public interface IMessageReceiver<in TMessage>
    {
        void Receive(TMessage message);
    }
}
