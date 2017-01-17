# sendstorm [![Build status](https://ci.appveyor.com/api/projects/status/8xtxxogo6gwbjnyw/branch/master?svg=true)](https://ci.appveyor.com/project/pcsajtai/sendstorm/branch/master) [![Coverage Status](https://coveralls.io/repos/z4kn4fein/sendstorm/badge.svg?branch=master&service=github)](https://coveralls.io/github/z4kn4fein/sendstorm?branch=master) [![Join the chat at https://gitter.im/z4kn4fein/sendstorm](https://img.shields.io/badge/gitter-join%20chat-1dce73.svg)](https://gitter.im/z4kn4fein/sendstorm?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![NuGet Version](https://buildstats.info/nuget/Sendstorm)](https://www.nuget.org/packages/Sendstorm/)
Sendstorm is a portable pub/sub framework for .NET based solutions.

##Features

 - Subscribe / Broadcast
 - Generic message types
 - Weakly referenced subscribers
 - Subscription filters
 - Execution targets

##Supported platforms

 - .NET 4.5 and above
 - Windows 8/8.1/10
 - Windows Phone Silverlight 8/8.1
 - Windows Phone 8.1
 - Xamarin (Android/iOS/iOS Classic)
 - .NET core [![Build status](https://ci.appveyor.com/api/projects/status/jn49cmxvuesxqb28/branch/master?svg=true)](https://ci.appveyor.com/project/pcsajtai/sendstorm-core/branch/master) [![NuGet Version](https://buildstats.info/nuget/Sendstorm.Core)](https://www.nuget.org/packages/Sendstorm.Core/)

##Subscribe / Broadcast
When you want to subscribe to an event, you have to implement the `IMessageReceiver` interface in your subscriber class.
```c#
class Foo : IMessageReceiver<FooMessage>
{
	public void Receive(FooMessage message)
	{
		//do something with the message
	}
}
```
Then you can subscribe your class as a listener to that specific event.
```c#
var messagePublisher = new MessagePublisher();
var foo = new Foo();
messagePublisher.Subscribe<FooMessage>(foo);
```
Now you can broadcast messages to your subscribers.
```c#
messagePublisher.Broadcast<FooMessage>(new FooMessage());
```
You are also able to unsubscribe if you don't want to receive messages anymore.
```c#
messagePublisher.UnSubscribe<FooMessage>(foo);
```
##Filters
If you want to receive the messages conditionally you can specify a filter for your subscription which's parameter will be the message object.
```c#
messagePublisher.Subscribe<FooMessage>(new Foo(), fooMessage => false); 
```
> This sample above will completely prevent the `Foo` object from recieving any `FooMessage`, in a real scenario here you can check against some properties of the message object, or the state of your subscriber class.

##Execution target
You can also specify where you want to delegate your messages.  
```c#
messagePublisher.Subscribe<FooMessage>(new Foo(), fooMessage => false,
	ExecutionTarget.BackgroundThread); 
```
Available options are:

 - *BroadcastThread* (it'll delegate the receive call to the thread from where the broadcast was called)
 - *BackgroundThread* (it'll create a task and will let the ThreadPool schedule the execution of the receive call)
 - *UiThread* (it'll delegate the receive call to the UI thread through its `SynchronizationContext`)

> The UI thread option only works when the `MessagePublisher` is able to collect a valid `SynchronizationContext` object for delegating calls to the UI thread. To achieve this you have to instantiate it on the UI thread.
