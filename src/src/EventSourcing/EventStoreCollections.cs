using System.Collections;

namespace Purview.EventSourcing;

/// <summary>
/// Event-sourced list collection.
/// Exposes read-only semantics to consumers while providing IList access for EF materialization.
/// </summary>
/// <typeparam name="T">The type of the elements in the list.</typeparam>
/// <remarks>
/// <para>
/// Aggregate collection state that participates in generated events should use this type (or
/// <see cref="EventStoreSet{T}"/>) rather than an ordinary mutable collection, because the generated
/// collection-event plumbing is designed around these wrappers.
/// </para>
/// <para>
/// Consumers see read-only semantics through <see cref="IReadOnlyList{T}"/>; the mutating <see cref="IList{T}"/>
/// surface exists so persistence frameworks (for example EF Core) can materialize the collection during reads.
/// </para>
/// </remarks>
public sealed class EventStoreList<T> : IList<T>, IReadOnlyList<T>, IList
{
	readonly List<T> _items;

	/// <summary>
	/// Initializes an empty list.
	/// </summary>
	public EventStoreList()
	{
		_items = [];
	}

	/// <summary>
	/// Initializes a list with the specified initial capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity of the list.</param>
	public EventStoreList(int capacity) => _items = [with(capacity)];

	/// <summary>
	/// Initializes a list populated with the supplied items.
	/// </summary>
	/// <param name="items">The items to add to the list.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
	public EventStoreList(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		_items = [.. items];
	}

	/// <summary>
	/// Gets the number of elements in the list.
	/// </summary>
	public int Count => _items.Count;

	/// <summary>
	/// Gets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>The element at the specified index.</returns>
	public T this[int index] => _items[index];

	bool ICollection<T>.IsReadOnly => false;

	T IList<T>.this[int index]
	{
		get => _items[index];
		set => _items[index] = value;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the list.
	/// </summary>
	/// <returns>An enumerator for the list.</returns>
	public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	void ICollection<T>.Add(T item) => _items.Add(item);

	void ICollection<T>.Clear() => _items.Clear();

	bool ICollection<T>.Contains(T item) => _items.Contains(item);

	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

	bool ICollection<T>.Remove(T item) => _items.Remove(item);

	int IList<T>.IndexOf(T item) => _items.IndexOf(item);

	void IList<T>.Insert(int index, T item) => _items.Insert(index, item);

	void IList<T>.RemoveAt(int index) => _items.RemoveAt(index);

	bool IList.IsFixedSize => false;

	bool IList.IsReadOnly => false;

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => false;

	object? IList.this[int index]
	{
		get => _items[index];
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			_items[index] = (T)value;
		}
	}

	int IList.Add(object? value)
	{
		ArgumentNullException.ThrowIfNull(value);
		_items.Add((T)value);
		return _items.Count - 1;
	}

	bool IList.Contains(object? value) => value is T typed && _items.Contains(typed);

	int IList.IndexOf(object? value) => value is T typed ? _items.IndexOf(typed) : -1;

	void IList.Insert(int index, object? value)
	{
		ArgumentNullException.ThrowIfNull(value);
		_items.Insert(index, (T)value);
	}

	void IList.Remove(object? value)
	{
		if (value is T typed)
			_items.Remove(typed);
	}

	void IList.Clear() => _items.Clear();

	void IList.RemoveAt(int index) => _items.RemoveAt(index);

	void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
}

/// <summary>
/// Event-sourced set collection.
/// Enforces uniqueness while providing IList access for EF materialization.
/// </summary>
/// <typeparam name="T">The type of the elements in the set.</typeparam>
/// <remarks>
/// <para>
/// Aggregate collection state that participates in generated events should use this type (or
/// <see cref="EventStoreList{T}"/>) rather than an ordinary mutable collection, because the generated
/// collection-event plumbing is designed around these wrappers.
/// </para>
/// <para>
/// Consumers see read-only semantics through <see cref="IReadOnlySet{T}"/>; the mutating <see cref="IList{T}"/>
/// surface exists so persistence frameworks (for example EF Core) can materialize the collection during reads.
/// Uniqueness is enforced for every mutation path.
/// </para>
/// </remarks>
public sealed class EventStoreSet<T> : IList<T>, IReadOnlySet<T>, IReadOnlyList<T>, IList
{
	readonly List<T> _items;
	readonly HashSet<T> _set;

	/// <summary>
	/// Initializes an empty set.
	/// </summary>
	public EventStoreSet()
	{
		_items = [];
		_set = [];
	}

	/// <summary>
	/// Initializes a set with the specified initial capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity of the set.</param>
	public EventStoreSet(int capacity)
	{
		_items = [with(capacity)];
		_set = [];
	}

	/// <summary>
	/// Initializes a set populated with the supplied items using the default equality comparer.
	/// </summary>
	/// <param name="items">The items to add to the set.</param>
	public EventStoreSet(IEnumerable<T> items)
		: this(items, comparer: null) { }

	/// <summary>
	/// Initializes a set populated with the supplied items using the specified equality comparer.
	/// </summary>
	/// <param name="items">The items to add to the set.</param>
	/// <param name="comparer">The comparer used to enforce uniqueness, or null for the default comparer.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
	public EventStoreSet(IEnumerable<T> items, IEqualityComparer<T>? comparer)
	{
		ArgumentNullException.ThrowIfNull(items);
		_items = [];
		_set = [with(comparer)];

		foreach (var item in items)
			AddUnique(item);
	}

	/// <summary>
	/// Gets the number of elements in the set.
	/// </summary>
	public int Count => _items.Count;

	/// <summary>
	/// Gets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>The element at the specified index.</returns>
	public T this[int index] => _items[index];

	bool ICollection<T>.IsReadOnly => false;

	T IList<T>.this[int index]
	{
		get => _items[index];
		set
		{
			var existing = _items[index];
			if (EqualityComparer<T>.Default.Equals(existing, value))
				return;

			if (_set.Contains(value))
				throw new InvalidOperationException("EventStoreSet does not allow duplicate values.");

			_set.Remove(existing);
			_items[index] = value;
			_set.Add(value);
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through the set.
	/// </summary>
	/// <returns>An enumerator for the set.</returns>
	public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	void ICollection<T>.Add(T item) => AddUnique(item);

	void ICollection<T>.Clear()
	{
		_items.Clear();
		_set.Clear();
	}

	/// <summary>
	/// Determines whether the set contains the specified item.
	/// </summary>
	/// <param name="item">The item to locate.</param>
	/// <returns>True when the item is present, otherwise false.</returns>
	public bool Contains(T item) => _set.Contains(item);

	bool ICollection<T>.Contains(T item) => _set.Contains(item);

	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

	bool ICollection<T>.Remove(T item)
	{
		if (!_set.Remove(item))
			return false;

		_items.Remove(item);
		return true;
	}

	int IList<T>.IndexOf(T item) => _items.IndexOf(item);

	void IList<T>.Insert(int index, T item)
	{
		if (_set.Contains(item))
			return;

		_items.Insert(index, item);
		_set.Add(item);
	}

	void IList<T>.RemoveAt(int index)
	{
		var item = _items[index];
		_items.RemoveAt(index);
		_set.Remove(item);
	}

	/// <summary>
	/// Determines whether the set is a proper subset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns>True when the set is a proper subset of <paramref name="other"/>.</returns>
	public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

	/// <summary>
	/// Determines whether the set is a proper superset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns>True when the set is a proper superset of <paramref name="other"/>.</returns>
	public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

	/// <summary>
	/// Determines whether the set is a subset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns>True when the set is a subset of <paramref name="other"/>.</returns>
	public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

	/// <summary>
	/// Determines whether the set is a superset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns>True when the set is a superset of <paramref name="other"/>.</returns>
	public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

	/// <summary>
	/// Determines whether the set shares any elements with the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns>True when at least one element is shared.</returns>
	public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

	/// <summary>
	/// Determines whether the set and the specified collection contain the same elements.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns>True when the collections are equal.</returns>
	public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

	bool IList.IsFixedSize => false;

	bool IList.IsReadOnly => false;

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => false;

	object? IList.this[int index]
	{
		get => _items[index];
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			((IList<T>)this)[index] = (T)value;
		}
	}

	int IList.Add(object? value)
	{
		ArgumentNullException.ThrowIfNull(value);
		var typed = (T)value;
		if (_set.Contains(typed))
			return _items.IndexOf(typed);

		_items.Add(typed);
		_set.Add(typed);
		return _items.Count - 1;
	}

	bool IList.Contains(object? value) => value is T typed && _set.Contains(typed);

	int IList.IndexOf(object? value) => value is T typed ? _items.IndexOf(typed) : -1;

	void IList.Insert(int index, object? value)
	{
		ArgumentNullException.ThrowIfNull(value);
		((IList<T>)this).Insert(index, (T)value);
	}

	void IList.Remove(object? value)
	{
		if (value is T typed)
			((ICollection<T>)this).Remove(typed);
	}

	void IList.Clear() => ((ICollection<T>)this).Clear();

	void IList.RemoveAt(int index) => ((IList<T>)this).RemoveAt(index);

	void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

	void AddUnique(T item)
	{
		if (!_set.Add(item))
			return;

		_items.Add(item);
	}
}
