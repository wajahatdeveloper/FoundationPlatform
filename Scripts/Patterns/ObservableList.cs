using System;
using System.Collections;
using System.Collections.Generic;

public class ObservableList<T> : IList<T>
{
    private readonly List<T> _items = new List<T>();

    public event Action<T> ItemAdded;
    public event Action<T> ItemRemoved;
    public event Action Cleared;

    public int Count => _items.Count;
    public bool IsReadOnly => false;

    public T this[int index]
    {
        get => _items[index];
        set
        {
            var old = _items[index];
            if (EqualityComparer<T>.Default.Equals(old, value)) return;
            _items[index] = value;
            ItemRemoved?.Invoke(old);
            ItemAdded?.Invoke(value);
        }
    }

    public void Add(T item)
    {
        _items.Add(item);
        ItemAdded?.Invoke(item);
    }

    public void Clear()
    {
        _items.Clear();
        Cleared?.Invoke();
    }

    public bool Contains(T item) => _items.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int IndexOf(T item) => _items.IndexOf(item);

    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        ItemAdded?.Invoke(item);
    }

    public bool Remove(T item)
    {
        var removed = _items.Remove(item);
        if (removed) ItemRemoved?.Invoke(item);
        return removed;
    }

    public void RemoveAt(int index)
    {
        var itm = _items[index];
        _items.RemoveAt(index);
        ItemRemoved?.Invoke(itm);
    }
}


