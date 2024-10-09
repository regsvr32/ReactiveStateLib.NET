namespace ReactiveStateLib;

public class NotifierNode {
  public event GetPropertyPathListener? OnGetPropertyPath;
  public event SetPropertyPathListener? OnSetPropertyPath;

  NotifierNode? _parent = null;
  string _name = string.Empty;

  public void AsChild(NotifierNode parent, string name) {
    _parent = parent;
    _name = name;
  }

  public void UnsetParent() {
    _parent = null;
    _name = string.Empty;
  }

  public void NoticeGetPropertyPath(IEnumerable<string> path) {
    OnGetPropertyPath?.Invoke(path);
    _parent?.NoticeGetPropertyPath(path.Prepend(_name));
  }

  public void NoticeSetPropertyPath(IEnumerable<string> path, object? newValue, object? oldValue) {
    OnSetPropertyPath?.Invoke(path, newValue, oldValue);
    _parent?.NoticeSetPropertyPath(path.Prepend(_name), newValue, oldValue);
  }

  public void NoticeGetProperty(string property) => NoticeGetPropertyPath([property]);
  public void NoticeSetProperty(string property, object? newValue, object? oldValue) => NoticeSetPropertyPath([property], newValue, oldValue);
}

public delegate void GetPropertyPathListener(IEnumerable<string> path);
public delegate void SetPropertyPathListener(IEnumerable<string> path, object? newValue, object? oldValue);