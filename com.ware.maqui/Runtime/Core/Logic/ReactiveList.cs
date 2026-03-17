using System;
using System.Collections;
using System.Collections.Generic;
using R3;

namespace Maqui.Core.Logic
{
    /// <summary>
    /// Reactive wrapper around List&lt;T&gt; that emits granular change notifications.
    /// Use this instead of ReactiveProperty&lt;IReadOnlyList&lt;T&gt;&gt; when the view needs
    /// incremental updates (add/remove/replace) rather than full-list rebuilds.
    ///
    /// All mutation methods emit events synchronously on the calling thread.
    /// Subscribe to ObserveAdd, ObserveRemove, ObserveReplace, ObserveReset, or
    /// ObserveCountChanged for granular reactions.
    /// </summary>
    public sealed class ReactiveList<T> : IReadOnlyList<T>, IDisposable
    {
        private readonly List<T> _list;

        private readonly Subject<ListAddEvent<T>> _addSubject = new();
        private readonly Subject<ListRemoveEvent<T>> _removeSubject = new();
        private readonly Subject<ListReplaceEvent<T>> _replaceSubject = new();
        private readonly Subject<Unit> _resetSubject = new();
        private readonly ReactiveProperty<int> _count;

        public ReactiveList()
        {
            _list = new List<T>();
            _count = new ReactiveProperty<int>(0);
        }

        public ReactiveList(IEnumerable<T> initial)
        {
            _list = new List<T>(initial);
            _count = new ReactiveProperty<int>(_list.Count);
        }

        // ── Observables ─────────────────────────────────────────────────────

        /// <summary>Emits when an item is added or inserted.</summary>
        public Observable<ListAddEvent<T>> ObserveAdd() => _addSubject;

        /// <summary>Emits when an item is removed.</summary>
        public Observable<ListRemoveEvent<T>> ObserveRemove() => _removeSubject;

        /// <summary>Emits when an item is replaced via indexer.</summary>
        public Observable<ListReplaceEvent<T>> ObserveReplace() => _replaceSubject;

        /// <summary>Emits when Clear() is called.</summary>
        public Observable<Unit> ObserveReset() => _resetSubject;

        /// <summary>Reactive count that updates after every mutation.</summary>
        public ReadOnlyReactiveProperty<int> ObserveCountChanged() => _count;

        // ── IReadOnlyList<T> ────────────────────────────────────────────────

        public int Count => _list.Count;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                var old = _list[index];
                _list[index] = value;
                _replaceSubject.OnNext(new ListReplaceEvent<T>(index, old, value));
            }
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ── Mutations ───────────────────────────────────────────────────────

        public void Add(T item)
        {
            int index = _list.Count;
            _list.Add(item);
            _count.Value = _list.Count;
            _addSubject.OnNext(new ListAddEvent<T>(index, item));
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            _count.Value = _list.Count;
            _addSubject.OnNext(new ListAddEvent<T>(index, item));
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _list[index];
            _list.RemoveAt(index);
            _count.Value = _list.Count;
            _removeSubject.OnNext(new ListRemoveEvent<T>(index, item));
        }

        public void Clear()
        {
            _list.Clear();
            _count.Value = 0;
            _resetSubject.OnNext(Unit.Default);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = _list[oldIndex];
            _list.RemoveAt(oldIndex);
            _list.Insert(newIndex, item);
            // Emit as remove + add so views can reorder
            _removeSubject.OnNext(new ListRemoveEvent<T>(oldIndex, item));
            _addSubject.OnNext(new ListAddEvent<T>(newIndex, item));
        }

        // ── Query ───────────────────────────────────────────────────────────

        public int IndexOf(T item) => _list.IndexOf(item);
        public bool Contains(T item) => _list.Contains(item);

        // ── Dispose ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            _addSubject.Dispose();
            _removeSubject.Dispose();
            _replaceSubject.Dispose();
            _resetSubject.Dispose();
            _count.Dispose();
        }
    }

    // ── Event structs ───────────────────────────────────────────────────────

    public readonly struct ListAddEvent<T>
    {
        public readonly int Index;
        public readonly T Item;
        public ListAddEvent(int index, T item) { Index = index; Item = item; }
    }

    public readonly struct ListRemoveEvent<T>
    {
        public readonly int Index;
        public readonly T Item;
        public ListRemoveEvent(int index, T item) { Index = index; Item = item; }
    }

    public readonly struct ListReplaceEvent<T>
    {
        public readonly int Index;
        public readonly T OldItem;
        public readonly T NewItem;
        public ListReplaceEvent(int index, T oldItem, T newItem) { Index = index; OldItem = oldItem; NewItem = newItem; }
    }
}
