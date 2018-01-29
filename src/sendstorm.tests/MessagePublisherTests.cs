using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sendstorm.Infrastructure;
using Sendstorm.Subscription;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sendstorm.Tests
{
    [TestClass]
    public class MessagePublisherTests
    {
        private IMessagePublisher publisher;

        [TestInitialize]
        public void Init()
        {
            this.publisher = new MessagePublisher();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void MessagePublisherTests_SubscribeDuplicatesTest()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            this.publisher.Subscribe(reciever.Object);
            this.publisher.Subscribe(reciever.Object);
        }

        [TestMethod]
        public void MessagePublisherTests_BroadcastWithoutSubscribers()
        {
            this.publisher.Broadcast(5);
        }

        [TestMethod]
        public void MessagePublisherTests_SubscribePublishTest()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            this.publisher.Subscribe(reciever.Object);
            this.publisher.Broadcast(5);
            reciever.Verify(rec => rec.Receive(5), Times.Once);

        }

        [TestMethod]
        public void MessagePublisherTests_UnSubscriberPublishTest()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            var reciever2 = new Mock<IMessageReceiver<int>>();
            this.publisher.Subscribe(reciever.Object);
            this.publisher.Subscribe(reciever2.Object);
            this.publisher.UnSubscribe(reciever.Object);
            this.publisher.Broadcast(5);

            reciever.Verify(rec => rec.Receive(It.IsAny<int>()), Times.Never);
            reciever2.Verify(rec => rec.Receive(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void MessagePublisherTests_UnSubscriberPublishTest2()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            var reciever2 = new Mock<IMessageReceiver<int>>();
            var reciever3 = new Mock<IMessageReceiver<int>>();
            var reciever4 = new Mock<IMessageReceiver<int>>();
            var reciever5 = new Mock<IMessageReceiver<int>>();

            this.publisher.Subscribe(reciever.Object);
            this.publisher.Subscribe(reciever2.Object);
            this.publisher.Subscribe(reciever3.Object);
            this.publisher.Subscribe(reciever4.Object);
            this.publisher.Subscribe(reciever5.Object);

            this.publisher.Broadcast(5);

            reciever.Verify(rec => rec.Receive(5), Times.Once);
            reciever2.Verify(rec => rec.Receive(5), Times.Once);
            reciever3.Verify(rec => rec.Receive(5), Times.Once);
            reciever4.Verify(rec => rec.Receive(5), Times.Once);
            reciever5.Verify(rec => rec.Receive(5), Times.Once);

            this.publisher.UnSubscribe(reciever.Object);

            this.publisher.Broadcast(6);

            reciever.Verify(rec => rec.Receive(6), Times.Never);
            reciever2.Verify(rec => rec.Receive(6), Times.Once);
            reciever3.Verify(rec => rec.Receive(6), Times.Once);
            reciever4.Verify(rec => rec.Receive(6), Times.Once);
            reciever5.Verify(rec => rec.Receive(6), Times.Once);

            this.publisher.UnSubscribe(reciever5.Object);

            this.publisher.Broadcast(7);

            reciever.Verify(rec => rec.Receive(7), Times.Never);
            reciever2.Verify(rec => rec.Receive(7), Times.Once);
            reciever3.Verify(rec => rec.Receive(7), Times.Once);
            reciever4.Verify(rec => rec.Receive(7), Times.Once);
            reciever5.Verify(rec => rec.Receive(7), Times.Never);

            this.publisher.UnSubscribe(reciever3.Object);

            this.publisher.Broadcast(8);

            reciever.Verify(rec => rec.Receive(8), Times.Never);
            reciever2.Verify(rec => rec.Receive(8), Times.Once);
            reciever3.Verify(rec => rec.Receive(8), Times.Never);
            reciever4.Verify(rec => rec.Receive(8), Times.Once);
            reciever5.Verify(rec => rec.Receive(8), Times.Never);

            this.publisher.UnSubscribe(reciever4.Object);

            this.publisher.Broadcast(9);

            reciever.Verify(rec => rec.Receive(9), Times.Never);
            reciever2.Verify(rec => rec.Receive(9), Times.Once);
            reciever3.Verify(rec => rec.Receive(9), Times.Never);
            reciever4.Verify(rec => rec.Receive(9), Times.Never);
            reciever5.Verify(rec => rec.Receive(9), Times.Never);

            this.publisher.UnSubscribe(reciever2.Object);

            this.publisher.Broadcast(10);

            reciever.Verify(rec => rec.Receive(10), Times.Never);
            reciever2.Verify(rec => rec.Receive(10), Times.Never);
            reciever3.Verify(rec => rec.Receive(10), Times.Never);
            reciever4.Verify(rec => rec.Receive(10), Times.Never);
            reciever5.Verify(rec => rec.Receive(10), Times.Never);
        }

        [TestMethod]
        public void MessagePublisherTests_UnSubscriberPublishTest3()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            var reciever2 = new Mock<IMessageReceiver<int>>();
            var reciever3 = new Mock<IMessageReceiver<int>>();
            var reciever4 = new Mock<IMessageReceiver<int>>();
            var reciever5 = new Mock<IMessageReceiver<int>>();

            this.publisher.Subscribe(reciever.Object);
            this.publisher.Subscribe(reciever2.Object);
            this.publisher.Subscribe(reciever3.Object);
            this.publisher.Subscribe(reciever4.Object);
            this.publisher.Subscribe(reciever5.Object);

            this.publisher.UnSubscribe(reciever3.Object);

            this.publisher.Broadcast(5);

            reciever.Verify(rec => rec.Receive(5), Times.Once);
            reciever2.Verify(rec => rec.Receive(5), Times.Once);
            reciever3.Verify(rec => rec.Receive(5), Times.Never);
            reciever4.Verify(rec => rec.Receive(5), Times.Once);
            reciever5.Verify(rec => rec.Receive(5), Times.Once);
        }

        [TestMethod]
        public void MessagePublisherTests_SubscribeConditional()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            this.publisher.Subscribe(reciever.Object, result => result == 5);
            this.publisher.Broadcast(0);

            reciever.Verify(rec => rec.Receive(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void MessagePublisherTests_PublishOnBackgroundThread()
        {
            var completionSource = new TaskCompletionSource<int>();
            var reciever = new Mock<IMessageReceiver<int>>();
            reciever.Setup(rec => rec.Receive(It.IsAny<int>()))
                .Callback((int message) => completionSource.SetResult(message))
                .Verifiable();
            this.publisher.Subscribe(reciever.Object, executionTarget: ExecutionTarget.BackgroundThread);
            this.publisher.Broadcast(5);
            var result = completionSource.Task.Result;
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void MessagePublisherTests_WeakReference()
        {
            var reciever = new WeakReferenceMessageReceiver();

            this.publisher.Subscribe(reciever);
            reciever = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

            //Ensures broadcast to a collected object doesn't kill the observer
            this.publisher.Broadcast(5);

            var newreciever = new WeakReferenceMessageReceiver();
            this.publisher.Subscribe(newreciever);

            this.publisher.Broadcast(5);
            Assert.AreEqual(5, newreciever.Message);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MessagePublisherTests_PublishOnUiThread_FromNonUiThread()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            this.publisher.Subscribe(reciever.Object, executionTarget: ExecutionTarget.Synchronized);
        }

        [TestMethod]
        public void MessagePublisherTests_SynchronizedSubscriptionTest()
        {
            var syncContext = new Mock<SynchronizationContext>();
            var sub = new SynchronizedSubscription(new Mock<IMessageReceiver<int>>().Object, null, null, syncContext.Object);

            sub.PublishMessage(5);

            syncContext.Verify(context => context.Post(It.IsAny<SendOrPostCallback>(), 5), Times.Once);
        }
    }

    class WeakReferenceMessageReceiver : IMessageReceiver<int>
    {
        public int Message { get; set; }
        public void Receive(int message)
        {
            this.Message = message;
        }
    }
}