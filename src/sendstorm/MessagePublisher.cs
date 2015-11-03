using Ronin.Common;
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
        private readonly Ref<ImmutableTree<Type, Ref<ImmutableTree<object, StandardSubscription>>>> subscriptionRepository;
        private readonly object syncObject = new object();
        private readonly SynchronizationContext context = SynchronizationContext.Current;

        public MessagePublisher()
        {
            this.subscriptionRepository = new Ref<ImmutableTree<Type, Ref<ImmutableTree<object, StandardSubscription>>>>(ImmutableTree<Type, Ref<ImmutableTree<object, StandardSubscription>>>.Empty);
        }

        public void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread)
        {
            var messageType = typeof(TMessage);

            var immutableTree = new Ref<ImmutableTree<object, StandardSubscription>>(ImmutableTree<object, StandardSubscription>.Empty);
            var subscription = this.CreateSubscription(messageReciever, filter, executionTarget);
            var newTree = new Ref<ImmutableTree<object, StandardSubscription>>(immutableTree.Value.AddOrUpdate(messageReciever, subscription));

            lock (this.syncObject)
            {
                var newRepository = this.subscriptionRepository.Value.AddOrUpdate(messageType, newTree, (oldValue, newValue) =>
                {
                    var newSubscription = oldValue.Value.AddOrUpdate(messageReciever, subscription, (oldSubscription, newSubs) =>
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

                    if (!oldValue.TrySwapIfStillCurrent(oldValue.Value, newSubscription))
                        oldValue.Swap(_ => newSubscription);
                    return oldValue;
                });

                if (!this.subscriptionRepository.TrySwapIfStillCurrent(this.subscriptionRepository.Value, newRepository))
                    this.subscriptionRepository.Swap(_ => newRepository);
            }
        }

        public void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever)
        {
            lock (this.syncObject)
            {
                var messageType = typeof(TMessage);
                var currentRepository = this.subscriptionRepository.Value.GetValueOrDefault(messageType);

                var newSubscribers = currentRepository.Value.Update(messageReciever, null);

                if (!currentRepository.TrySwapIfStillCurrent(currentRepository.Value, newSubscribers))
                    currentRepository.Swap(_ => newSubscribers);

                var newRepository = this.subscriptionRepository.Value.Update(messageType, currentRepository);

                if (!this.subscriptionRepository.TrySwapIfStillCurrent(this.subscriptionRepository.Value, newRepository))
                    this.subscriptionRepository.Swap(_ => newRepository);
            }
        }

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
            var subscriptions = this.subscriptionRepository.Value.GetValueOrDefault(messageType);
            return subscriptions?.Value?.Enumerate().Where(sub => sub.Value != null).Select(sub => sub.Value).ToArray();
        }
    }
}