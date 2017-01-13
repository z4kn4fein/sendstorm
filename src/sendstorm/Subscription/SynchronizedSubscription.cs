using System.Reflection;
using System.Threading;
using Sendstorm.Utils;

namespace Sendstorm.Subscription
{
    internal class SynchronizedSubscription : StandardSubscription
    {
        private readonly SynchronizationContext context;

        public SynchronizedSubscription(object subscriber, object filterOrigin, MethodInfo filter, SynchronizationContext context)
            : base(subscriber, filterOrigin, filter)
        {
            Shield.EnsureNotNull(context, nameof(context), "ExecutionTarget is set to UiThread but SynchornizationContext.Current is null, subscribe to this event from the Ui thread.");

            this.context = context;
        }

        public override void PublishMessage<TMessage>(TMessage message)
        {
            this.context.Post(callback => base.PublishMessage((TMessage)callback), message);
        }
    }
}
