namespace ReactiveStateLib;

public partial class ReactiveState<T> {
  class ListenerBinding(bool fixedDependence, Action<T> listener, ReactiveState<T> hub) {
    PropertyTree<object>? dependence = null;

    public void Invoke() {
      if (fixedDependence && dependence != null) {
        listener(hub.state);
        return;
      }
      var dependencesOld = dependence ?? new();
      hub.invoking.Push(this);
      dependence = new();
      listener(hub.state);
      dependence.Diff(dependencesOld, out var add, out var remove);
      foreach (var path in add) { hub.bindings.Touch(path).Add(this); }
      foreach (var path in remove) { hub.bindings.Touch(path).Remove(this); }
      hub.invoking.Pop();
    }

    public void Cancel() {
      if (dependence == null) { return; }
      var dependencesOld = dependence;
      dependence = new();
      dependence.Diff(dependencesOld, out _, out var remove);
      foreach (var path in remove) { hub.bindings.Touch(path).Remove(this); }
    }

    public void Dependence(IEnumerable<string> path) => dependence?.Touch(path);
  }
}
