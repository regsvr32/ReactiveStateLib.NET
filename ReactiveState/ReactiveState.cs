namespace ReactiveStateLib;

public partial class ReactiveState<T> {
  PropertyTree<HashSet<ListenerBinding>> bindings = new();
  PropertyTree<object> dirty = new();
  Stack<ListenerBinding> invoking = new();
  T state;

  public ReactiveState() {
    state = AutoImplement.CreateReactive<T>(out var notifier);
    notifier.OnGetPropertyPath += (path) => invoking.FirstOrDefault()?.Dependence(path);
    notifier.OnSetPropertyPath += (path, _, _) => dirty.Touch(path);
  }

  public BindingCanceller Bind(bool fixedDependence, Action<T> listener) {
    var binding = new ListenerBinding(fixedDependence, listener, this);
    binding.Invoke();
    return binding.Cancel;
  }

  public void Update(Action<T> update) {
    update(state);
    var triggered = new HashSet<ListenerBinding>();
    foreach (var bindingSet in dirty.Search(bindings)) {
      foreach (var binding in bindingSet) { triggered.Add(binding); }
    }
    dirty = new();
    foreach (var binding in triggered) { binding.Invoke(); }
  }

  public T GetState() => state;
  public void ClearBindings() => bindings = new();
}

public delegate void BindingCanceller();