using Ronin.Common;
using Sendstorm.Infrastructure;
using Sendstorm.Subscription;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sendstorm
{
    /// <summary>
    /// Represents an observer pattern implementation.
    /// </summary>
    public class MessagePublisher : IMessagePublisher
    {
        private readonly ConcurrentKeyValueStore<Type, ConcurrentKeyValueStore<int, StandardSubscription>> subscriptionRepository;
        private readonly SynchronizationContext context = SynchronizationContext.Current;

        /// <summary>
        /// Constructs the <see cref="MessagePublisher"/>
        /// </summary>
        public MessagePublisher()
        {
            this.subscriptionRepository = new ConcurrentKeyValueStore<Type, ConcurrentKeyValueStore<int, StandardSubscription>>();
        }

        /// <summary>
        /// Subscribes to a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="messageReciever">The subscriber object.</param>
        /// <param name="filter">Subscription filter which will be evaluated before every broadcast. With this you can make conditional subscriptions.</param>
        /// <param name="executionTarget">The target of the message delivery, available values are: BroadcastThread, BackgroundThread, UiThread.</param>
        public void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread)
        {
            Shield.EnsureNotNull(messageReciever, nameof(messageReciever));

            var messageType = typeof(TMessage);
            var subscription = this.CreateSubscription(messageReciever, filter, executionTarget);
            var subscriptions = this.subscriptionRepository.GetOrAdd(messageType,
                () => new ConcurrentKeyValueStore<int, StandardSubscription>());

            subscriptions.AddOrUpdate(messageReciever.GetHashCode(), () => subscription, (oldValue, newValue) =>
            {
                object target;
                if (oldValue.Subscriber.TryGetTarget(out target))
                    throw new InvalidOperationException("The given object is already subscribed.");
                oldValue.Subscriber.SetTarget(messageReciever);
                return oldValue;
            });


        }

        /// <summary>
        /// Removes a subscription from a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="messageReciever">The subscriber object.</param>
        public void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever)
        {
            Shield.EnsureNotNull(messageReciever, nameof(messageReciever));

            var messageType = typeof(TMessage);
            ConcurrentKeyValueStore<int, StandardSubscription> subscriptions;
            if (this.subscriptionRepository.TryGet(messageType, out subscriptions))
            {
                subscriptions.TryRemove(messageReciever.GetHashCode());
            }
        }

        /// <summary>
        /// Broadcasts a message to the related subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message object.</param>
        public void Broadcast<TMessage>(TMessage message)
        {
            var subscribers = this.GetSubscribers(message);

            if (subscribers == null) return;

            foreach (var subscriber in subscribers)
            {
                subscriber.PublishMessage(message);
            }
        }

        private StandardSubscription CreateSubscription<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter, ExecutionTarget executionTarget)
        {
            switch (executionTarget)
            {
                case ExecutionTarget.UiThread:
                    return new SynchronizedSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo(), this.context ?? SynchronizationContext.Current);
                case ExecutionTarget.BackgroundThread:
                    return new BackgroundSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo());
                default:
                    return new StandardSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo());
            }
        }

        private StandardSubscription[] GetSubscribers<TMessage>(TMessage message)
        {
            var messageType = typeof(TMessage);
            ConcurrentKeyValueStore<int, StandardSubscription> subscriptions;
            return this.subscriptionRepository.TryGet(messageType, out subscriptions) ? subscriptions.GetAll().ToArray() : null;
        }
    }
}