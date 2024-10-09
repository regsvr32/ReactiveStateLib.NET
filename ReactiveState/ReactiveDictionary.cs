using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ReactiveStateLib;

public class ReactiveDictionary<TKey, TValue> : StateNotifier, IDictionary<TKey, TValue> where TKey : notnull {
  Dictionary<TKey, TValue> dictionary = new();

  public NotifierNode Notifier { get; } = new();
  public event SetDictionaryItemListener<TKey, TValue>? OnSetItem;
  public event RemoveDictionaryItemListener<TKey, TValue>? OnRemoveItem;

  public ICollection<TKey> Keys => dictionary.Keys;
  public ICollection<TValue> Values => dictionary.Values;
  public int Count => dictionary.Count;
  public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
  public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => dictionary.TryGetValue(key, out value);
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();

  void NoticeUpdated() => Notifier.NoticeSetProperty("Items", dictionary.AsReadOnly(), null);

  public TValue this[TKey key] { 
    get => dictionary[key];
    set {
      dictionary.TryGetValue(key, out var oldItem);
      dictionary[key] = value;
      OnSetItem?.Invoke(key, value, oldItem);
      NoticeUpdated();
    }
  }

  public void Add(TKey key, TValue value) {
    dictionary.Add(key, value);
    OnSetItem?.Invoke(key, value, default);
    NoticeUpdated();
  }

  public void Clear() {
    var items = dictionary.ToArray();
    dictionary.Clear();
    OnRemoveItem?.Invoke(items);
    NoticeUpdated();
  }

  public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value) {
    if (!dictionary.Remove(key, out value)) { return false; }
    OnRemoveItem?.Invoke(new KeyValuePair<TKey, TValue>(key, value));
    NoticeUpdated();
    return true;
  }

  public bool Remove(TKey key) => Remove(key, out _);

  bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
  bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => (dictionary as ICollection<KeyValuePair<TKey, TValue>>).Contains(item);
  void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => (dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
  void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
  bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) {
    if (!dictionary.TryGetValue(item.Key, out var value)) { return false; }
    if (!EqualityComparer<TValue>.Default.Equals(value, item.Value)) { return false; }
    return Remove(item.Key);
  }
}

public delegate void SetDictionaryItemListener<TKey, TValue>(TKey key, TValue newItem, TValue? oldItem);
public delegate void RemoveDictionaryItemListener<TKey, TValue>(params KeyValuePair<TKey, TValue>[] item);