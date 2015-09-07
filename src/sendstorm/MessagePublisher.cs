using Ronin.Common;
using Sendstorm.Infrastructure;
using Sendstorm.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sendstorm
{
    public class MessagePublisher : IMessagePublisher
    {
        private readonly IDictionary<Type, IDictionary<int, StandardSubscription>> subscriptionRepository;
        private readonly DisposableReaderWriterLock readerWriterLock;
        private readonly SynchronizationContext context = SynchronizationContext.Current;

        public MessagePublisher()
        {
            this.subscriptionRepository = new Dictionary<Type, IDictionary<int, StandardSubscription>>();
            this.readerWriterLock = new DisposableReaderWriterLock();
        }

        public void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread)
        {
            IDictionary<int, StandardSubscription> subscribers;
            var messageType = typeof(TMessage);

            using (this.readerWriterLock.AquireWriteLock())
                if (!this.subscriptionRepository.TryGetValue(messageType, out subscribers))
                    this.AddToDictionary(messageReciever, messageType, filter, executionTarget);
                else
                    this.CheckForSubscribers(messageReciever, subscribers, filter, executionTarget);
        }

        public void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever)
        {
            using (this.readerWriterLock.AquireWriteLock())
            {
                IDictionary<int, StandardSubscription> subscribers;
                var messageType = typeof(TMessage);

                if (!this.subscriptionRepository.TryGetValue(messageType, out subscribers)) return;
                subscribers.Remove(messageReciever.GetHashCode());
            }
        }

        private void CheckForSubscribers<TMessage>(IMessageReceiver<TMessage> messageReciever, IDictionary<int, StandardSubscription> subscribers, Func<TMessage, bool> filter, ExecutionTarget executionTarget)
        {
            StandardSubscription existingSubscriber;
            if (!subscribers.TryGetValue(messageReciever.GetHashCode(), out existingSubscriber))
            {
                var subscription = this.CreateSubscription(messageReciever, filter, executionTarget);
                subscribers.Add(messageReciever.GetHashCode(), subscription);
            }
            else
            {
                object target;
                if (!existingSubscriber.Subscriber.TryGetTarget(out target))
                {
                    existingSubscriber.Subscriber.SetTarget(messageReciever);
                }
                else
                {
                    throw new InvalidOperationException("The given type is already in the subscription list.");
                }
            }
        }

        private void AddToDictionary<TMessage>(IMessageReceiver<TMessage> messageReciever, Type messageType, Func<TMessage, bool> filter, ExecutionTarget executionTarget)
        {
            var dictionary = new Dictionary<int, StandardSubscription>();
            var subscription = this.CreateSubscription(messageReciever, filter, executionTarget);
            dictionary.Add(messageReciever.GetHashCode(), subscription);

            this.subscriptionRepository.Add(messageType, dictionary);
        }

        public void Broadcast<TMessage>(TMessage message)
        {
            var subscribers = this.GetSubscribers(message);

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
                case ExecutionTarget.BroadcastThread:
                default:
                    return new StandardSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo());
            }
        }

        private StandardSubscription[] GetSubscribers<TMessage>(TMessage message)
        {
            IDictionary<int, StandardSubscription> subscribers;
            var messageType = typeof(TMessage);

            using (this.readerWriterLock.AquireReadLock())
            {
                this.subscriptionRepository.TryGetValue(messageType, out subscribers);
                return subscribers.Values.ToArray();
            }
        }
    }
}