using Sendstorm.Infrastructure;
using Sendstorm.Utils;
using System;
using System.Reflection;

namespace Sendstorm.Subscription
{
    internal class StandardSubscription
    {
        public WeakReference<object> Subscriber { get; }

        private readonly MethodInfo filter;
        private readonly WeakReference<object> filterOrigin;

        public StandardSubscription(object subscriber, object filterOrigin, MethodInfo filter)
        {
            Shield.EnsureNotNull(subscriber, nameof(subscriber));

            this.Subscriber = new WeakReference<object>(subscriber);
            this.filter = filter;
            this.filterOrigin = filterOrigin == null ? null : new WeakReference<object>(filterOrigin);
        }

        public virtual void PublishMessage<TMessage>(TMessage message)
        {
            if (!this.IsSubscriptionValid(message, out var target))
                return;

            var reciever = target as IMessageReceiver<TMessage>;
            reciever?.Receive(message);
        }

        private bool IsSubscriptionValid<TMessage>(TMessage message, out object target)
        {
            if (!this.Subscriber.TryGetTarget(out target)) return false;
            return this.filter == null || !this.filterOrigin.TryGetTarget(out var origin) || (bool)this.filter.Invoke(origin, new object[] { message });
        }
    }
}
