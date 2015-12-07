namespace Sendstorm.Infrastructure
{
    /// <summary>
    /// Interface used for subscribing messages in the <see cref="IMessagePublisher"/>
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribing.</typeparam>
    public interface IMessageReceiver<in TMessage>
    {
        /// <summary>
        /// Receives a message from the <see cref="IMessagePublisher"/>
        /// </summary>
        /// <param name="message">The message object.</param>
        void Receive(TMessage message);
    }
}
