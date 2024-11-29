using ReactiveStateLib;

var example = new ReactiveState<Foo>();

example.Bind(true, (state) => Console.WriteLine($"Bar={state.Bar}; Baz={state.Baz}")); // output: Bar=; Baz=0

example.Update((state) => state.Bar = "hello"); // output: Bar=hello; Baz=0

example.Update((state) => state.Baz = 1); // output: Bar=hello; Baz=1

example.Update((state) => {
  state.Bar = "wow";
  state.Baz = 42;
}); // output: Bar=wow; Baz=42

example.Bind(true, (state) => Console.WriteLine($"Qux=[{string.Join(',', state.Qux)}]")); // output: Qux=[]

example.Update((state) => state.Qux.AddRange(["wubba", "dub"])); // output: Qux=[wubba,dub]

example.Update((state) => state.Qux.InsertRange(1, ["lubba", "dub"])); // output: Qux=[wubba,lubba,dub,dub]

example.Update((state) => state.Qux.Clear()); // output: Qux=[]

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

example.Bind(false, (state) => {
  Console.Write("listener invoked. ");
  var corge = state.Corge;
  Console.WriteLine(corge.Grault < corge.Graply ? $"Waldo={corge.Waldo}" : string.Empty);
}); // output: listener invoked.

example.Update((state) => state.Corge.Waldo = "wubba lubba dub-dub"); // nothing output

example.Update((state) => state.Corge.Graply = 42); // output: listener invoked. Waldo=wubba lubba dub-dub

example.Update((state) => state.Corge.Waldo = "hakuna matata!"); // output: listener invoked. Waldo=hakuna matata!

example.Update((state) => { }); // nothing output