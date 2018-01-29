using Sendstorm.Infrastructure;
using Sendstorm.Subscription;
using Sendstorm.Utils;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Sendstorm.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f54f3fc3580d2301f3aa4c3c6d28c2e419687f23392a4c0f543c17232c8c1640a12a0ebeae2ed5c59cf7443100718480a19c7fd62ab8225b40741179c6ad8c17e6dbb8d6e4d98255c6364ca6ca541148b11d7c72f74919d283f2536f52750b7e0a69d9f416e4a4eed49a38547daee8d11ca1dca646f6eb519ba5c2faeff7d7b0")]

namespace Sendstorm
{
    /// <inheritdoc />
    public class MessagePublisher : IMessagePublisher
    {
        private AvlTreeKeyValue<Type, LinkedStore<int, StandardSubscription>> subscriptionRepository;
        private readonly SynchronizationContext context = SynchronizationContext.Current;

        /// <summary>
        /// Constructs the <see cref="MessagePublisher"/>
        /// </summary>
        public MessagePublisher()
        {
            this.subscriptionRepository = AvlTreeKeyValue<Type, LinkedStore<int, StandardSubscription>>.Empty;
        }

        /// <inheritdoc />
        public void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread)
        {
            Shield.EnsureNotNull(messageReciever, nameof(messageReciever));

            var hash = messageReciever.GetHashCode();
            var messageType = typeof(TMessage);
            var subscription = this.CreateSubscription(messageReciever, filter, executionTarget);
            var newStore = new LinkedStore<int, StandardSubscription>(hash, subscription);

            Swap.SwapValue(ref this.subscriptionRepository,
                repo => repo.AddOrUpdate(messageType, newStore,
                    (oldValue, newValue) =>
                    {
                        var current = oldValue;
                        while (true)
                        {
                            if (hash == current.Key)
                                if (current.Value.Subscriber.TryGetTarget(out var target))
                                    throw new InvalidOperationException();
                                else
                                {
                                    current.Value.Subscriber.SetTarget(messageReciever);
                                    return oldValue;
                                }

                            if (current.Next == LinkedStore<int, StandardSubscription>.Empty)
                            {
                                current.Next = newStore;
                                return oldValue;
                            }

                            current = current.Next;
                        }
                    }));
        }

        /// <inheritdoc />
        public void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever)
        {
            Shield.EnsureNotNull(messageReciever, nameof(messageReciever));

            var hash = messageReciever.GetHashCode();
            var messageType = typeof(TMessage);
            var subscribers = this.subscriptionRepository.GetOrDefault(messageType);
            if (subscribers == null) return;

            if (subscribers.Key == hash)
            {
                Swap.SwapValue(ref this.subscriptionRepository,
                    repo => repo.AddOrUpdate(messageType, subscribers.Next,
                        (oldValue, newValue) => newValue));

                return;
            }

            Swap.SwapValue(ref this.subscriptionRepository,
                repo => repo.AddOrUpdate(messageType, null,
                    (oldValue, newValue) =>
                    {
                        var previous = oldValue;
                        var current = oldValue.Next;
                        while (true)
                        {
                            if (hash == current.Key)
                            {
                                previous.Next = current.Next;
                                return oldValue;
                            }

                            if (current.Next == LinkedStore<int, StandardSubscription>.Empty)
                                return oldValue;

                            previous = current;
                            current = current.Next;
                        }
                    }));
        }

        /// <inheritdoc />
        public void Broadcast<TMessage>(TMessage message)
        {
            var subscribers = this.subscriptionRepository.GetOrDefault(typeof(TMessage));
            if (subscribers == null) return;

            foreach (var subscriber in subscribers)
                subscriber.PublishMessage(message);
        }

        private StandardSubscription CreateSubscription<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter, ExecutionTarget executionTarget)
        {
            switch (executionTarget)
            {
                case ExecutionTarget.Synchronized:
                    return new SynchronizedSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo(), this.context ?? SynchronizationContext.Current);
                case ExecutionTarget.BackgroundThread:
                    return new BackgroundSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo());
                default:
                    return new StandardSubscription(messageReciever, filter?.Target, filter?.GetMethodInfo());
            }
        }
    }
}