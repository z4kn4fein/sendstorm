﻿using Ronin.Common;
using Sendstorm.Infrastructure;
using Sendstorm.Subscription;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sendstorm
{
    public class MessagePublisher : IMessagePublisher
    {
        private ImmutableTree<Type, ImmutableTree<object, StandardSubscription>> subscriptionRepository;
        private readonly object syncObject = new object();
        private readonly SynchronizationContext context = SynchronizationContext.Current;

        public MessagePublisher()
        {
            this.subscriptionRepository = ImmutableTree<Type, ImmutableTree<object, StandardSubscription>>.Empty;
        }

        /// <summary>
        /// Subscribes to a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="messageReciever">The subscriber object.</param>
        /// <param name="filter">Subscription filter which will be evaluated before every message broadcast. With this you can make conditional subscriptions.</param>
        /// <param name="executionTarget">The target of the message delivery, available values are: BroadcastThread, BackgroundThread, UiThread.</param>
        public void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread)
        {
            var messageType = typeof(TMessage);

            var immutableTree = ImmutableTree<object, StandardSubscription>.Empty;
            var subscription = this.CreateSubscription(messageReciever, filter, executionTarget);
            var newTree = immutableTree.AddOrUpdate(messageReciever, subscription);

            lock (this.syncObject)
            {
                var newRepository = this.subscriptionRepository.AddOrUpdate(messageType, newTree, (oldValue, newValue) =>
                {
                    return oldValue.AddOrUpdate(messageReciever, subscription, (oldSubscription, newSubs) =>
                    {
                        object target;
                        if (!oldSubscription.Subscriber.TryGetTarget(out target))
                        {
                            oldSubscription.Subscriber.SetTarget(messageReciever);
                        }
                        else
                        {
                            throw new InvalidOperationException("The given object is already in the subscription list.");
                        }

                        return oldSubscription;
                    });
                });

                this.subscriptionRepository = newRepository;
            }
        }

        /// <summary>
        /// Removes a subscription from a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="messageReciever">The subscriber object.</param>
        public void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever)
        {
            lock (this.syncObject)
            {
                var messageType = typeof(TMessage);
                var currentRepository = this.subscriptionRepository.GetValueOrDefault(messageType).Update(messageReciever, null);
                this.subscriptionRepository = this.subscriptionRepository.Update(messageType, currentRepository);
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
            var subscriptions = this.subscriptionRepository.GetValueOrDefault(messageType);
            return subscriptions?.Enumerate().Where(sub => sub.Value != null).Select(sub => sub.Value).ToArray();
        }
    }
}