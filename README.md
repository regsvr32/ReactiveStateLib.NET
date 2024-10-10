# ReactiveStateLib.NET
 English | [简体中文](README.zh-CN.md)

A lightweight .NET library for reactive state binding

## Introduction
This library aims to achieve the following objectives:
* Reactive state monitoring that automatically triggers corresponding listeners when data changes, such as updating the UI.
* Intelligent dependency analysis for listeners to reduce unnecessary triggers.
* Implement the above two points in a more automated way, rather than manually inserting trigger events or marking dirty states in each setter.
* Use IL generation instead of reflection for potentially frequently called sections to save some (perhaps insignificant) performance, such as property value updates, which might be called every frame in game projects.
  * *However, for parts that are not expected or should not be called frequently, such as state object instantiation, reflection will still be used to avoid overly verbose and obscure code.*
* Listener binding and triggering code style will be closer to that of dynamic languages, such as [Vue.js](https://vuejs.org/) with its [watchEffect](https://vuejs.org/api/reactivity-core#watcheffect) (in fact, this library is almost modeled after it). This approach disregards some dogmas about so-called code readability.
* Enable users to implement the above with as little code as possible, even at the cost of [some limitations](#Limitations).

## Why not use Rx.NET
[Rx.NET](https://github.com/dotnet/reactive) is a rigorous and mature reactive .NET library. In fact, if you're working on a large commercial project, I would strongly recommend Rx.NET over this library.

However, not every project has a sufficient reason to have developers write (forgive me) verbose event sources and subscriptions for every state that needs binding. Only very few projects can take advantage of the more advanced features in ReactiveX, and these features do have alternatives.

In many cases, what developers want is simply this: when a property in a state object is updated, one or more controls in the UI that depend on it will automatically update—nothing more.

In such scenarios, instead of spending a lot of time and effort learning the somewhat obscure reactive development paradigm, introducing a non-lightweight library, and then only using the most basic functions, perhaps a simple, lightweight library that’s easier to get started with might be a reasonable choice.

## Example

Define the following demo data model:
```csharp
public interface Foo {
  string Bar { get; set; }
  int Baz { get; set; }
  [Initialized] ReactiveList<string> Qux { get; set; }
  [Initialized] ReactiveDictionary<string, int> Quux { get; set; }
  [Initialized] Corge Corge { get; set; }
}

public interface Corge {
  int Grault { get; set; }
  int Graply { get; set; }
  string Waldo { get; set; }
}
```
Properties marked with `[Initialized]` will automatically create and set an instance when the state object is created.

Create a state object `ReactiveState` and call `ReactiveState.Bind` to bind a listener:
```csharp
var example = new ReactiveState<Foo>();
example.Bind(true, (state) => Console.WriteLine($"Bar={state.Bar}; Baz={state.Baz}")); // output: Bar=; Baz=0
```

Use `ReactiveState.Update` to update the state, and each time a property accessed in a listener is updated, it triggers the listener:
```csharp
example.Update((state) => state.Bar = "hello"); // output: Bar=hello; Baz=0
example.Update((state) => state.Baz = 1); // output: Bar=hello; Baz=1
```

In one `ReactiveState.Update`, the same listener will not be triggered multiple times:
```csharp
example.Update((state) => {
  state.Bar = "wow";
  state.Baz = 42;
}); // output: Bar=wow; Baz=42
```

`ReactiveList` provides an `IList` implementation that triggers responses when its content is updated:
```csharp
example.Bind(true, (state) => Console.WriteLine($"Qux=[{string.Join(',', state.Qux)}]")); // output: Qux=[]
example.Update((state) => state.Qux.AddRange(["wubba", "dub"])); // output: Qux=[wubba,dub]
example.Update((state) => state.Qux.InsertRange(1, ["lubba", "dub"])); // output: Qux=[wubba,lubba,dub,dub]
example.Update((state) => state.Qux.Clear()); // output: Qux=[]
```
Similarly, `ReactiveDictionary` provides an `IDictionary` implementation that triggers responses when its content is updated. **Note:** When the type parameters of `ReactiveList` and `ReactiveDictionary` are also reactive objects, updates to sub-properties of the content **will not** bubble up to the `ReactiveList` or `ReactiveDictionary`. Due to skepticism about usage scenarios and concerns about performance overhead, there are no plans for future updates.

`ReactiveState.Bind` returns a `BindingCanceller`, which can be called to cancel the listener binding:
```csharp
example.Update((state) => {
  state.Quux["dub"] = 2;
  state.Quux["wubba"] = 0;
}); // nothing output

var cancel = example.Bind(true, (state) => {
  var keys = state.Quux.OrderBy(item => item.Value).Select(item => item.Key);
  Console.WriteLine($"Quux(ordered keys)=[{string.Join(',', keys)}]");
}); // output: Quux(ordered keys)=[wubba,dub]

example.Update((state) => {
  state.Quux["_dub"] = 3;
  state.Quux["lubba"] = 1;
}); // output: Quux(ordered keys)=[wubba,lubba,dub,_dub]

cancel();
example.Update((state) => state.Quux.Clear()); // nothing output
```

The first parameter of `ReactiveState.Bind` indicates whether the listener’s dependencies are fixed. If some properties are not accessed every time the listener is triggered (e.g., wrapped in an if block), set this parameter to `false`:
```csharp
example.Bind(false, (state) => {
  Console.Write("listener invoked. ");
  var corge = state.Corge;
  Console.WriteLine(corge.Grault < corge.Graply ? $"Waldo={corge.Waldo}" : string.Empty);
}); // output: listener invoked.

example.Update((state) => state.Corge.Waldo = "wubba lubba dub-dub"); // nothing output
example.Update((state) => state.Corge.Graply = 42); // output: listener invoked. Waldo=wubba lubba dub-dub
example.Update((state) => state.Corge.Waldo = "hakuna matata!"); // output: listener invoked. Waldo=hakuna matata!
```
A typical use case is that `Waldo` in the above example will be updated frequently, but the displayed UI content is not always related to it. In this case, you can place the access to `Waldo` in an if block to avoid frequently but meaninglessly triggering the listener.

## Limitations
When using `ReactiveState` or `AutoImplement`, the type parameters have the following restrictions:
* Must be an interface, and the interface's accessibility level must be `public`.
* All interface members must be properties. If methods are needed, it is recommended to use [extension methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods).
* Interface member properties must expose both a getter and setter.
* **No circular references**, unless the property is excluded from the tracking tree using the `ShallowAttribute`.
* Properties marked with `InitializedAttribute` must be either interfaces or classes with a public parameterless constructor.

When using `ReactiveState`, the following limitations or side effects apply:
* When binding asynchronous listeners with `ReactiveState.Bind`, make sure to access all dependent properties before the first `await`.
  * *Conversely, if you want to read a property without having it recorded as a dependency of the binding, reading it after the await is a trick.*
* **Do not nest** `ReactiveState`'s `Bind` and `Update`—this is definitely not a good idea, and there must be a better approach.
* Calling `ReactiveState.Bind` executes the passed listener once to analyze dependencies.
* When `ReactiveState.Bind` is called with the `fixedDependence` parameter set to `false`, dependencies are re-analyzed on each trigger, which incurs some (insignificant) additional performance overhead.
