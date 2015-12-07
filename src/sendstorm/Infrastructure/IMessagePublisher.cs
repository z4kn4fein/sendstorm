using System;

namespace Sendstorm.Infrastructure
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Subscribes to a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="messageReciever">The subscriber object.</param>
        /// <param name="filter">Subscription filter which will be evaluated before every message broadcast. With this you can make conditional subscriptions.</param>
        /// <param name="executionTarget">The target of the message delivery, available values are: BroadcastThread, BackgroundThread, UiThread.</param>
        void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread);

        /// <summary>
        /// Removes a subscription from a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="messageReciever">The subscriber object.</param>
        void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever);

        /// <summary>
        /// Broadcasts a message to the related subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message object.</param>
        void Broadcast<TMessage>(TMessage message);
    }
}
