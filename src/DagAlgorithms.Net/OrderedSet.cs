using System.Collections;

namespace DagAlgorithms.Net;

/// <summary>
/// A minimal insertion-ordered set: O(1) membership testing and append, O(1) indexed
/// access, and enumeration in the exact order items were added. Used internally to give
/// graph traversal algorithms deterministic, insertion-order tie-breaking without paying
/// for a full-blown ordered dictionary implementation.
/// </summary>
internal sealed class OrderedSet<T> : IReadOnlyList<T>
    where T : notnull
{
    private readonly List<T> _items = new();
    private readonly Dictionary<T, int> _positionByItem;

    internal OrderedSet(IEqualityComparer<T> comparer)
    {
        _positionByItem = new Dictionary<T, int>(comparer);
    }

    public int Count => _items.Count;

    public T this[int index] => _items[index];

    internal bool Contains(T item) => _positionByItem.ContainsKey(item);

    internal bool Add(T item)
    {
        if (_positionByItem.ContainsKey(item))
        {
            return false;
        }

        _positionByItem.Add(item, _items.Count);
        _items.Add(item);
        return true;
    }

    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
