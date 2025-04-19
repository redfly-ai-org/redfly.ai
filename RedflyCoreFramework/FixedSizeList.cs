namespace RedflyCoreFramework;

public class FixedSizeList<T>
{
    private readonly List<T> _list = new();
    private readonly int _maxSize;

    public FixedSizeList(int maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be greater than zero.");
        _maxSize = maxSize;
    }

    public void Add(T item)
    {
        if (_list.Count == _maxSize)
        {
            _list.RemoveAt(0); // Remove the oldest item (first in the list)
        }
        _list.Add(item);
    }

    public T this[int index] => _list[index];

    public int Count => _list.Count;

    public IReadOnlyList<T> Items => _list.AsReadOnly();

    public bool All(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return _list.All(predicate);
    }
}
