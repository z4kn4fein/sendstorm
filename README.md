# sendstorm [![Build status](https://ci.appveyor.com/api/projects/status/8xtxxogo6gwbjnyw/branch/master?svg=true)](https://ci.appveyor.com/project/pcsajtai/sendstorm/branch/master) [![Join the chat at https://gitter.im/z4kn4fein/sendstorm](https://img.shields.io/badge/gitter-join%20chat-green.svg)](https://gitter.im/z4kn4fein/sendstorm?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![NuGet Version](http://img.shields.io/nuget/v/Sendstorm.svg?style=flat)](https://www.nuget.org/packages/Sendstorm/) [![NuGet Downloads](http://img.shields.io/nuget/dt/Sendstorm.svg?style=flat)](https://www.nuget.org/packages/Sendstorm/)
This library contains a portable observer pattern implementation written in c#.

Supported platforms:

 - .NET 4.5 and above
 - Windows 8/8.1/10
 - Windows Phone Silverlight 8/8.1
 - Windows Phone 8.1
 - Xamarin (Android/iOS/iOS Classic)

##Usage
####Subscribe/Broadcast
When you want to subscribe to a specified event you have to implement the **IMessageReciever** interface.
```c#
class Foo : IMessageReciever<FooMessage>
{
	public void Recieve(FooMessage message)
	{
		//do something with the message
	}
}
```
Then you can subscribe your class as a listener to that specific event.
```c#
var messagePublisher = new MessagePublisher();
messagePublisher.Subscribe<FooMessage>(new Foo());
```
Now you can broadcast messages to your subscribers.
```c#
messagePublisher.Broadcast<FooMessage>(new FooMessage());
```
####Filters
If you want to recieve the messages conditionally you can specify a filter for your subscription which's parameter will be the message object.
```c#
messagePublisher.Subscribe<FooMessage>(new Foo(), fooMessage => false); 
```
> This sample above will completely prevent the **Foo** object from recieving any **FooMessage**, in a real scenario here you can check against some props of the message object, or if you subscribe inside your subscriber object you can check the state of it.

####Execution target
You can also specify where you want the **MessagePublisher** delegate your message recieve invokations.  
```c#
messagePublisher.Subscribe<FooMessage>(new Foo(), fooMessage => false,
	ExecutionTarget.BackgroundThread); 
```
Available options are:

 - BroadcastThread (it'll delegate the recieve call to the thread from where the broadcast was called)
 - BackgroundThread (it'll create a task and will let the ThreadPool schedule the execution of the recieve call)
 - UiThread (it'll delegate the recieve call to the UI thread through its SynchronizationContext)

> The UI thread option only works when the **MessagePublisher** is able to collect a valid SynchronizationContext object for delegating calls to the UI thread. To achieve this you have to instantiate it on the UI thread.