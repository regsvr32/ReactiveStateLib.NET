namespace ReactiveStateLib;

class PropertyTree<T> where T : new() {
  Dictionary<string, PropertyTree<T>> children = new();
  T? value;

  T TouchLeaf(IEnumerator<string> path) {
    if (!path.MoveNext()) {
      value ??= new T();
      return value;
    }
    if (!children.TryGetValue(path.Current, out var child)) {
      child = new();
      children.Add(path.Current, child);
    }
    return child.TouchLeaf(path);
  }
  public T Touch(IEnumerable<string> path) => TouchLeaf(path.GetEnumerator());

  IEnumerable<string[]> GetLeaves(IEnumerable<string> path) {
    if (children.Count == 0) {
      yield return path.ToArray();
    }
    else {
      foreach (var (name, child) in children) {
        foreach (var leaf in child.GetLeaves(path.Append(name))) { yield return leaf; }
      }
    }
  }

  public void Diff(PropertyTree<T> other, out List<string[]> add, out List<string[]> remove, IEnumerable<string>? prePath = null) {
    add = new List<string[]>();
    remove = new List<string[]>();
    prePath ??= Enumerable.Empty<string>();

    foreach (var (name, child) in children) {
      var path = prePath.Append(name);
      if (other.children.ContainsKey(name)) {
        child.Diff(other.children[name], out var subAdd, out var subRemove, path);
        add.AddRange(subAdd);
        remove.AddRange(subRemove);
      }
      else {
        add.AddRange(child.GetLeaves(path));
      }
    }

    foreach (var (name, child) in other.children) {
      var path = prePath.Append(name);
      if (!children.ContainsKey(name)) {
        remove.AddRange(child.GetLeaves(path));
      }
    }
  }

  public IEnumerable<TValue> Search<TValue>(PropertyTree<TValue> valueTree) where TValue : new() {
    if (children.Count == 0) {
      foreach (var childValue in valueTree.GetAllValues()) { yield return childValue; }
      yield break;
    }
    if (valueTree.value != null) { yield return valueTree.value; }
    foreach (var (name, child) in children) {
      if (valueTree.children.TryGetValue(name, out var childValueTree)) {
        foreach (var childValue in child.Search(childValueTree)) { yield return childValue; }
      }
    }
  }

  IEnumerable<T> GetAllValues() {
    var result = children.Values.SelectMany(child => child.GetAllValues());
    if (value != null) { result = result.Prepend(value); }
    return result;
  }
}