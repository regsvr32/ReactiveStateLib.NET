using ReactiveStateLib;

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