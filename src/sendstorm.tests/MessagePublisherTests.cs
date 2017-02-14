﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            publisher.Subscribe(reciever.Object);
            publisher.Subscribe(reciever.Object);
        }

        [TestMethod]
        public void MessagePublisherTests_BroadcastWithoutSubscribers()
        {
            publisher.Broadcast(5);
        }

        [TestMethod]
        public void MessagePublisherTests_SubscribePublishTest()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            publisher.Subscribe(reciever.Object);
            publisher.Broadcast(5);
            reciever.Verify(rec => rec.Receive(5), Times.Once);

        }

        [TestMethod]
        public void MessagePublisherTests_UnSubscriberPublishTest()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            publisher.Subscribe(reciever.Object);
            publisher.UnSubscribe(reciever.Object);
            publisher.Broadcast(5);

            reciever.Verify(rec => rec.Receive(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void MessagePublisherTests_SubscribeConditional()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            publisher.Subscribe(reciever.Object, result => result == 5);
            publisher.Broadcast(0);

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
            publisher.Subscribe(reciever.Object, executionTarget: ExecutionTarget.BackgroundThread);
            publisher.Broadcast(5);
            var result = completionSource.Task.Result;
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void MessagePublisherTests_WeakReference()
        {
            var reciever = new WeakReferenceMessageReceiver();

            publisher.Subscribe(reciever);
            reciever = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

            //Ensures broadcast to a collected object doesn't kill the observer
            publisher.Broadcast(5);

            var newreciever = new WeakReferenceMessageReceiver();
            publisher.Subscribe(newreciever);

            publisher.Broadcast(5);
            Assert.AreEqual(5, newreciever.Message);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MessagePublisherTests_PublishOnUiThread_FromNonUiThread()
        {
            var reciever = new Mock<IMessageReceiver<int>>();
            publisher.Subscribe(reciever.Object, executionTarget: ExecutionTarget.Synchronized);
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

        public override int GetHashCode()
        {
            return 10;
        }
    }
}