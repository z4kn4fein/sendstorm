using System;

namespace Sendstorm.Infrastructure
{
    public interface IMessagePublisher
    {
        void Subscribe<TMessage>(IMessageReceiver<TMessage> messageReciever, Func<TMessage, bool> filter = null, ExecutionTarget executionTarget = ExecutionTarget.BroadcastThread);
        void UnSubscribe<TMessage>(IMessageReceiver<TMessage> messageReciever);
        void Broadcast<TMessage>(TMessage message);
    }
}
