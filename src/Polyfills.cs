#if NET40

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Collections.ObjectModel
{
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private const string NOTSUPPORTED_EXCEPTION_MESSAGE = "This dictionary is read-only";

        private readonly IDictionary<TKey, TValue> _dictionary;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public void Add(TKey key, TValue value)
        {
            throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys => _dictionary.Keys;

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public int Count => _dictionary.Count;

        public bool IsReadOnly => true;

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (_dictionary as IEnumerable).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
        }

        public void Clear()
        {
            throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
        }

        public ICollection<TValue> Values => throw new NotSupportedException(NOTSUPPORTED_EXCEPTION_MESSAGE);
    }
}

#endif