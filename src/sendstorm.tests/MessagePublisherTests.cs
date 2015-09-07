using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sendstorm;
using Sendstorm.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Sandstorm.Tests.MessagePublisherTests
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
            var reciever = new FakeIntMessageReciever();
            publisher.Subscribe(reciever);
            publisher.Subscribe(reciever);
        }

        [TestMethod]
        public void MessagePublisherTests_SubscribePublishTest()
        {
            var reciever = new FakeIntMessageReciever();

            publisher.Subscribe(reciever);

            var expectedResult = 5;

            publisher.Broadcast(expectedResult);
            Assert.AreEqual(expectedResult, reciever.Message);
        }

        [TestMethod]
        public void MessagePublisherTests_MixedSubscriberPublishTest()
        {
            var reciever1 = new FakeIntMessageReciever();
            var reciever2 = new FakeMixedMessageReciever();

            publisher.Subscribe(reciever1);
            publisher.Subscribe<string>(reciever2);
            publisher.Subscribe<int>(reciever2);

            var expectedResult = 5;
            var expectedStringResult = "FakeMessage";

            publisher.Broadcast(expectedResult);
            publisher.Broadcast(expectedStringResult);

            Assert.AreEqual(expectedResult, reciever1.Message);
            Assert.AreEqual(expectedResult, reciever2.Message);
            Assert.AreEqual(expectedStringResult, reciever2.StringMessage);
        }

        [TestMethod]
        public void MessagePublisherTests_UnSubscriberPublishTest()
        {
            var reciever1 = new FakeIntMessageReciever();
            var reciever2 = new FakeMixedMessageReciever();

            publisher.Subscribe(reciever1);
            publisher.Subscribe<string>(reciever2);
            publisher.Subscribe<int>(reciever2);

            publisher.UnSubscribe(reciever1);
            publisher.UnSubscribe<string>(reciever2);

            var expectedResult = 5;
            var expectedStringResult = "FakeMessage";

            publisher.Broadcast(expectedResult);
            publisher.Broadcast(expectedStringResult);

            Assert.AreEqual(0, reciever1.Message);
            Assert.AreEqual(expectedResult, reciever2.Message);
            Assert.AreEqual(null, reciever2.StringMessage);
        }

        [TestMethod]
        public void MessagePublisherTests_SubscribeConditional()
        {
            var reciever = new FakeConditionalMessageReciever();

            publisher.Subscribe(reciever, result => result == 5);

            var expectedResult = 0;

            publisher.Broadcast(expectedResult);
            Assert.AreEqual(expectedResult, reciever.Message);
        }

        [TestMethod]
        public void MessagePublisherTests_PublishOnBackgroundThread()
        {
            var completionSource = new TaskCompletionSource<int>();
            var reciever = new BackgroundMessageReciever(completionSource);

            publisher.Subscribe(reciever, executionTarget: ExecutionTarget.BackgroundThread);

            var expectedResult = 5;

            publisher.Broadcast(expectedResult);
            var result = completionSource.Task.Result;
            Assert.AreEqual(expectedResult, result);
        }

        public class FakeIntMessageReciever : IMessageReceiver<int>
        {
            public int Message { get; set; }

            public void Receive(int message)
            {
                this.Message = message;
            }
        }

        public class FakeMixedMessageReciever : IMessageReceiver<int>, IMessageReceiver<string>
        {
            public int Message { get; set; }
            public string StringMessage { get; set; }

            public void Receive(int message)
            {
                this.Message = message;
            }

            public void Receive(string message)
            {
                this.StringMessage = message;
            }
        }

        public class FakeConditionalMessageReciever : IMessageReceiver<int>
        {
            public int Message { get; set; }

            public void Receive(int message)
            {
                this.Message = message;
            }
        }

        public class BackgroundMessageReciever : IMessageReceiver<int>
        {
            private readonly TaskCompletionSource<int> completionSource;

            public BackgroundMessageReciever(TaskCompletionSource<int> completionSource)
            {
                this.completionSource = completionSource;
            }

            public void Receive(int message)
            {
                this.completionSource.SetResult(message);
            }
        }
    }
}