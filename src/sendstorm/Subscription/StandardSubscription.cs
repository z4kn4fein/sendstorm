using Ronin.Common;
using Sendstorm.Infrastructure;
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
            Shield.EnsureNotNull(subscriber);

            this.Subscriber = new WeakReference<object>(subscriber);
            this.filter = filter;
            this.filterOrigin = filterOrigin == null ? null : new WeakReference<object>(filterOrigin);
        }

        public virtual void PublishMessage<TMessage>(TMessage message)
        {
            object target;
            if (!this.IsSubscriptionValid(message, out target))
                return;

            var reciever = target as IMessageReceiver<TMessage>;
            if (reciever == null) return;

            reciever.Receive(message);
        }

        private bool IsSubscriptionValid<TMessage>(TMessage message, out object target)
        {
            object filterOrigin;
            if (!this.Subscriber.TryGetTarget(out target)) return false;

            if (this.filter != null && this.filterOrigin.TryGetTarget(out filterOrigin) &&
            !(bool)this.filter.Invoke(filterOrigin, new object[] { message }))
                return false;

            return true;
        }
    }
}
