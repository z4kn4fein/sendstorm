using System.Reflection;
using System.Threading.Tasks;

namespace Sendstorm.Subscription
{
    internal class BackgroundSubscription : StandardSubscription
    {
        public BackgroundSubscription(object subscriber, object filterOrigin, MethodInfo filter)
            : base(subscriber, filterOrigin, filter)
        {
        }

        public override void PublishMessage<TMessage>(TMessage message)
        {
            Task.Factory.StartNew(() => base.PublishMessage(message));
        }
    }
}
