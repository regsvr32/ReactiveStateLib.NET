# ReactiveStateLib.NET
简体中文 | [English](README.md)

提供轻量级响应式状态绑定的.NET库

## 简介
这个库意图实现下列目的：
* 响应式的状态监视，当数据有变动时自动触发对应的监听器，例如更新UI
* 比较智能地分析各监听器的依赖项，减少非必要的触发
* 以更为自动化的方式实现上述二项，而非笨拙地手动在每个setter中插入触发事件或标记为dirty的代码
* 用生成IL码而非反射的方式来实现一些有可能被高频调用的部分，以节省一些（可能微不足道的）性能，例如属性值的更新，这在游戏项目中有可能每帧都被调用
  * *但在不被认为会被/应该被高频调用的部分，例如状态对象的实例化，仍然会使用反射以避免过于冗长晦涩的代码*
* 监听器绑定和触发的代码风格会更接近一些动态语言中的方式，例如[Vue.js](https://cn.vuejs.org/)中的[watchEffect](https://cn.vuejs.org/api/reactivity-core#watcheffect)（事实上这几乎就是这个库的模仿对象），这会忽视一些关于所谓代码可读性的教条
* 让使用者可以用尽量少的代码实现上述，即使会因此产生[一些使用上的限制](#一些使用上的限制)

## 为什么不用Rx.NET
[Rx.NET](https://github.com/dotnet/reactive)是一个严谨且成熟的.NET响应式库，事实上，如果是用在较大型的商业项目中，我也会强烈建议你用Rx.NET而不是这个库。

但并非每个项目都有充足的理由让开发者为大量需绑定的状态逐一书写（恕我直言）冗长啰嗦的事件源和订阅，也只有非常少数的项目能使用到ReactiveX中那些更高级的功能，并且这些功能也并非没有替代方案。

许多时候开发者想要的只是，（例如）当状态对象中的一个属性被更新的时候，UI上依赖它的一个或多个控件也会自动更新，仅此而已。

在这种场景下，比起花费较多时间和精力去学习（有一点点晦涩的）真正而高级的响应式开发范式后、引用了一个不太轻量的库之后只使用了最基础的功能，也许一个功能简单而上手难度低的轻量库也不失为一种合理的选择。

## 示例

定义以下演示用数据模型：
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
被`[Initialized]`标记的属性会在创建状态对象时也自动创建并设置一个实例。

创建状态对象`ReactiveState`，调用`ReactiveState.Bind`绑定监听器：
```csharp
var example = new ReactiveState<Foo>();
example.Bind(true, (state) => Console.WriteLine($"Bar={state.Bar}; Baz={state.Baz}")); // output: Bar=; Baz=0
```

调用`ReactiveState.Update`更新状态，每个监听器中访问过的属性被更新都会触发响应：
```csharp
example.Update((state) => state.Bar = "hello"); // output: Bar=hello; Baz=0
example.Update((state) => state.Baz = 1); // output: Bar=hello; Baz=1
```

在一次`ReactiveState.Update`中，相同的监听器不会被重复触发：
```csharp
example.Update((state) => {
  state.Bar = "wow";
  state.Baz = 42;
}); // output: Bar=wow; Baz=42
```

`ReactiveList`提供一个可在内容有更新时触发响应的`IList`实现：
```csharp
example.Bind(true, (state) => Console.WriteLine($"Qux=[{string.Join(',', state.Qux)}]")); // output: Qux=[]
example.Update((state) => state.Qux.AddRange(["wubba", "dub"])); // output: Qux=[wubba,dub]
example.Update((state) => state.Qux.InsertRange(1, ["lubba", "dub"])); // output: Qux=[wubba,lubba,dub,dub]
example.Update((state) => state.Qux.Clear()); // output: Qux=[]
```
类似的，`ReactiveDictionary`提供一个可在内容有更新时触发响应的`IDictionary`实现。**需要注意的是**，当`ReactiveList`和`ReactiveDictionary`的类型参数也是可响应对象时，内容的子属性的更新**不会**冒泡到`ReactiveList`或`ReactiveDictionary`。出于对使用场景的怀疑和对性能开销的担忧，也没有后续更新的计划。

`ReactiveState.Bind`返回一个`BindingCanceller`，调用它可以取消监听器的绑定：
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

`ReactiveState.Bind`的第一个参数指示监听器的依赖项是否恒定。如果有些属性并非在每次触发都会被访问到（例如包在if块中），应将此参数设置为`false`：
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
一种使用场景是，上例的`Waldo`将会被很高频地更新，但某处的UI显示内容并不总是与它有关，此时即可将`Waldo`的访问置于if块中以避免频繁但无意义地触发监听器。

## 一些使用上的限制
使用`ReactiveState`或`AutoImplement`时的类型参数具有如下限制：
* 必须是接口，且接口的可访问性级别必须为`public`
* 接口成员必须全部是属性，如果需要方法，建议使用[扩展方法](https://learn.microsoft.com/zh-cn/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)
* 接口成员属性需要全部暴露`getter`和`setter`
* **不要出现循环引用**，除非使用`ShallowAttribute`将属性的子项排除出跟踪树
* `InitializedAttribute`标记的属性的类型须为接口、或者具有公开的无参数构造函数的类

使用`ReactiveState`时有如下限制或副作用：
* 调用`ReactiveState.Bind`绑定异步的监听器时，需要确保在第一个`await`之前访问过所有依赖的属性
  * *反之，如果希望读取某个属性却又想避免这个属性被记录为该绑定的依赖项，在await之后再读取该属性也是一种trick*
* **不要嵌套使用**`ReactiveState`的`Bind`和`Update`，这绝对不是个好主意，一定有更合理的做法
* 调用`ReactiveState.Bind`时会执行一次传入的监听器以分析依赖项
* 调用`ReactiveState.Bind`时，若传入的`fixedDependence`参数为`false`，则每次触发时都会重新分析依赖项，这会产生一些（微不足道的）额外性能开销
