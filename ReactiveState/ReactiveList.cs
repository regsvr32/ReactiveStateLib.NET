using System.Collections;

namespace ReactiveStateLib;

public class ReactiveList<T> : StateNotifier, IList<T> {
  List<T> list = new();

  public NotifierNode Notifier { get; } = new();
  public event SetListItemByIndexListener<T>? OnSetItemByIndex;
  public event AddListItemListener<T>? OnAddItem;
  public event RemoveListItemListener<T>? OnRemoveItem;
  public event RemoveListItemByPredicateListener<T>? OnRemoveItemByPredicate;

  public int Count => list.Count;
  public bool IsReadOnly => false;
  public bool Contains(T item) => list.Contains(item);
  public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
  public int IndexOf(T item) => list.IndexOf(item);
  public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

  void NoticeUpdated() => Notifier.NoticeSetProperty("Items", list.AsReadOnly(), null);

  public T this[int index] {
    get => list[index];
    set {
      var oldItem = list[index];
      list[index] = value;
      OnSetItemByIndex?.Invoke(index, value, oldItem);
      NoticeUpdated();
    }
  }

  public void Add(T item) {
    int index = list.Count;
    list.Add(item);
    OnAddItem?.Invoke(index, item);
    NoticeUpdated();
  }

  public void AddRange(IEnumerable<T> items) {
    int index = list.Count;
    var arr = items.ToArray();
    list.AddRange(arr);
    OnAddItem?.Invoke(index, arr);
    NoticeUpdated();
  }

  public void Insert(int index, T item) {
    list.Insert(index, item);
    OnAddItem?.Invoke(index, item);
    NoticeUpdated();
  }

  public void InsertRange(int index, IEnumerable<T> items) {
    var arr = items.ToArray();
    list.InsertRange(index, arr);
    OnAddItem?.Invoke(index, arr);
    NoticeUpdated();
  }

  public bool Remove(T item) {
    int index = list.IndexOf(item);
    if (index < 0) { return false; }
    RemoveAt(index);
    return true;
  }

  public void RemoveAt(int index) {
    var item = list[index];
    list.RemoveAt(index);
    OnRemoveItem?.Invoke(index, item);
    NoticeUpdated();
  }

  public void RemoveRange(int index, int count) {
    var arr = new T[count];
    list.CopyTo(index, arr, 0, count);
    list.RemoveRange(index, count);
    OnRemoveItem?.Invoke(index, arr);
    NoticeUpdated();
  }

  public void RemoveAll(Predicate<T> match) {
    int removeCount = list.RemoveAll(match);
    OnRemoveItemByPredicate?.Invoke(match, removeCount);
    NoticeUpdated();
  }

  public void Clear() {
    var items = list.ToArray();
    list.Clear();
    OnRemoveItem?.Invoke(0, items);
    NoticeUpdated();
  }
}

public delegate void SetListItemByIndexListener<T>(int index, T newItem, T oldItem);
public delegate void AddListItemListener<T>(int index, params T[] items);
public delegate void RemoveListItemListener<T>(int index, params T[] items);
public delegate void RemoveListItemByPredicateListener<T>(Predicate<T> match, int removeCount);