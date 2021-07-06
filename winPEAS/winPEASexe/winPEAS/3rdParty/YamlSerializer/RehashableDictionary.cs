using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;

namespace System.Yaml
{
    interface IRehashableKey
    {
        event EventHandler Changed;
    }

    /// <summary>
    /// <para>Dictionary that automatically rehash when the content of a key is changed.
    /// Keys of this dictionary must implement <see cref="IRehashableKey"/>.</para>
    /// <para>It also call back item addition and removal by <see cref="Added"/> and
    /// <see cref="Removed"/> events.</para>
    /// </summary>
    /// <typeparam name="K">Type of key. Must implements <see cref="IRehashableKey"/>.</typeparam>
    /// <typeparam name="V">Type of value.</typeparam>
    class RehashableDictionary<K, V>: IDisposable, IDictionary<K, V>
        where K: class, IRehashableKey
    {
        class KeyValue
        {
            public K key;
            public V value;
            public KeyValue(K key, V value)
            {
                this.key = key;
                this.value = value;
            }
        }
        /// <summary>
        /// <para>A dictionary that returns <see cref="KeyValue"/> or <see cref="List&lt;KeyValue&gt;"/> 
        /// from hash code. This is the main repository that stores the <see cref="KeyValue"/> pairs.</para>
        /// <para>If there are several entries that have same hash code for thir keys, 
        /// a <see cref="List&lt;KeyValue&gt;"/> is stored to hold all those entries.
        /// Otherwise, a <see cref="KeyValue"/> is stored.</para>
        /// </summary>
        SortedDictionary<int, object> items = new SortedDictionary<int, object>();
        /// <summary>
        /// <para>A dictionary that returns hash code from the key reference.
        /// The key must be the instance that <see cref="object.ReferenceEquals"/>
        /// to one exsisting in the dictionary.</para>
        /// </summary>
        /// <remarks>
        /// <para>We store the hashes correspoinding to each key. So that when rehash,
        /// we can find old hash code to quickly find the entry for the key.</para>
        /// <para>It is also used to remember the number of keys exists in the dictionary.</para>
        /// </remarks>
        Dictionary<K, int> hashes = new Dictionary<K, int>(
            TypeUtils.EqualityComparerByRef<K>.Default);

        /// <summary>
        /// Recalc hash key of the <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to be rehash. The key must be the instance that 
        /// <see cref="object.ReferenceEquals"/> to one exsisting in the dictionary.</param>
        void Rehash(K key)
        {
            // Note that key is compared by reference in this function!

            // update hash
            var oldHash = hashes[key];
            var newHash = key.GetHashCode();
            hashes[key] = newHash;

            // remove old entry
            var item = items[oldHash];
            KeyValue kv = null;
            if ( item is KeyValue ) {
                // only one item was found whose hash code equals to oldHash.
                kv = (KeyValue)item;
                // must be found
                Debug.Assert(kv.key == key);
                items.Remove(oldHash);
            } else {
                // several items were found whose hash codes equal to oldHash.
                var list = (List<KeyValue>)item;
                for ( int i = 0; i < list.Count; i++ ) {
                    kv = list[i];
                    if ( kv.key == key ) {
                        list.RemoveAt(i);
                        break;
                    }
                    // must be found
                    Debug.Assert(i + 1 < list.Count);
                }
                // only one item is left, whose hash code equals to oldHash.
                if ( list.Count == 1 )
                    items[oldHash] = list.First();
            }

            // add new entry
            if ( items.TryGetValue(newHash, out item) ) {
                if ( item is KeyValue ) {
                    // must not exist already
                    Debug.Assert(!( (KeyValue)item ).key.Equals(key));
                    var list = new List<KeyValue>();
                    list.Add((KeyValue)item);
                    list.Add(kv);
                    items[newHash] = list;
                } else {
                    // must not exist already
                    Debug.Assert(!( item as List<KeyValue> ).Any(li => li.key.Equals(key)));
                    ( item as List<KeyValue> ).Add(kv);
                }
            } else {
                items[newHash] = kv;
            }
        }

        public class DictionaryEventArgs: EventArgs
        {
            public K Key;
            public V Value;
            public DictionaryEventArgs(K key, V value)
            {
                Key = key;
                Value = value;
            }
        }

        protected virtual void OnAdded(K key, V value)
        {
            // set observer
            key.Changed += new EventHandler(KeyChanged);
            if ( Added != null )
                Added(this, new DictionaryEventArgs(key, value));
        }
        public event EventHandler<DictionaryEventArgs> Added;

        void KeyChanged(object sender, EventArgs e)
        {
            Rehash((K)sender);
        }

        protected virtual void OnRemoved(K key, V value)
        {
            // remove observer
            key.Changed -= new EventHandler(KeyChanged);
            if ( Removed != null )
                Removed(this, new DictionaryEventArgs(key, value));
        }
        public event EventHandler<DictionaryEventArgs> Removed;

        public void Dispose()
        {
            // remove observers
            Clear();
        }

        void AddCore(K key, V value, bool exclusive)
        {
            var newkv = new KeyValue(key, value);
            FindItem(key, false, default(V),
                (hash) => {                             // not found hash
                    items.Add(hash, newkv);
                    hashes.Add(key, hash);
                },
                (hash, oldkv) => {                      // hash hit one entry but key not found
                    var list = new List<KeyValue>();
                    list.Add(oldkv);
                    items[hash] = list;
                    list.Add(newkv);
                    hashes.Add(key, hash);
                },
                (hash, oldkv) => {                      // hash hit one entry and key found
                    ReplaceKeyValue(oldkv, newkv, exclusive);
                },
                (hash, list) => {                       // hash hit several entries but key not found
                    list.Add(newkv);
                    hashes.Add(key, hash);
                },
                (hash, oldkv, list, i) => {             // hash hit several entries and key found
                    ReplaceKeyValue(oldkv, newkv, exclusive);
                }
                );
            OnAdded(key, value);
        }

        void ReplaceKeyValue(KeyValue oldkv, KeyValue newkv, bool exclusive)
        {
            if ( exclusive )
                throw new InvalidOperationException("Same key already exists.");
            var oldkv_saved = new KeyValue(oldkv.key, oldkv.value);
            oldkv.key = newkv.key;
            oldkv.value = newkv.value;
            hashes.Remove(oldkv_saved.key);
            OnRemoved(oldkv_saved.key, oldkv_saved.value);
        }

        bool RemoveCore(K key, bool compareValue, V value)
        {
            bool result = true;
            FindItem(key, compareValue, value,
                (hash) => { result = false; },      // key not found
                (hash, kv) => {                     // hash hit one entry and key found
                    items.Remove(hash);
                    hashes.Remove(kv.key);
                    OnRemoved(kv.key, kv.value);
                },
                (hash, kv, list, i) => {            // hash hit several entries and key found
                    list.RemoveAt(i);
                    // only one entry left
                    if ( list.Count == 1 )
                        items[hash] = list.First();
                    hashes.Remove(kv.key);
                    OnRemoved(kv.key, kv.value);
                }
                );
            return result;
        }

        bool TryGetValueCore(K key, out V value)
        {
            bool result = true;
            V v = default(V);
            FindItem(key, false, default(V),
                (hash) => { result = false; },              // key not found
                (hash, kv) => { v = kv.value; },            // hash hit one entry and key found
                (hash, kv, list, i) => { v = kv.value; }    // hash hit several entries and key found
                );
            value = v;
            return result;
        }

        public ICollection<KeyValuePair<K, V>> ItemsFromHash(int key_hash)
        {
            object entry;
            if ( items.TryGetValue(key_hash, out entry) ) {
                if ( entry is KeyValue ) {
                    return new ItemsCollection(this, (KeyValue)entry);
                } else {
                    return new ItemsCollection(this, (List<KeyValue>)entry);
                }
            } else {
                return new ItemsCollection(this);
            }
        }

        class ItemsCollection: KeysValuesBase<KeyValuePair<K, V>>
        {
            List<KeyValue> list;
            public ItemsCollection(RehashableDictionary<K, V> dictionary, List<KeyValue> list)
                : base(dictionary)
            {
                this.list = list;
            }
            public ItemsCollection(RehashableDictionary<K, V> dictionary, KeyValue entry)
                : base(dictionary)
            {
                this.list = new List<KeyValue>();
                list.Add(entry);
            }
            public ItemsCollection(RehashableDictionary<K, V> dictionary)
                : base(dictionary)
            {
                this.list = new List<KeyValue>();
            }

            public override int Count
            {
                get { return list.Count; }
            }

            public override bool Contains(KeyValuePair<K, V> item)
            {
                return list.Any(entry => entry.key.Equals(item.Key) && entry.value.Equals(item.Value));
            }

            public override void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                foreach ( var entry in list )
                    array[arrayIndex++] = new KeyValuePair<K, V>(entry.key, entry.value);
            }

            public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                foreach ( var item in list )
                    yield return new KeyValuePair<K, V>(item.key, item.value);
            }
        }

        /// <summary>
        /// Try to find entry for key (and value).
        /// </summary>
        /// <param name="key">key to find</param>
        /// <param name="compareValue">if true, value matters</param>
        /// <param name="value">value to find</param>
        /// <param name="NotFound">key not found</param>
        /// <param name="FoundOne">hash hit one entry and key found</param>
        /// <param name="FoundList">hash hit several entries and key found</param>
        void FindItem(K key, bool compareValue, V value,
            Action<int> NotFound,                                   // key not found
            Action<int, KeyValue> FoundOne,                         // hash hit one entry and key found
            Action<int, KeyValue, List<KeyValue>, int> FoundList)   // hash hit several entries and key found
        {
            FindItem(key, compareValue, value,
                (hash) => NotFound(hash),
                (hash, kv) => NotFound(hash),
                (hash, kv) => FoundOne(hash, kv),
                (hash, list) => NotFound(hash),
                (hash, kv, list, i) => FoundList(hash, kv, list, i)
                );
        }

        /// <summary>
        /// Try to find entry for key (and value).
        /// </summary>
        /// <param name="key">key to find</param>
        /// <param name="compareValue">if true, value matters</param>
        /// <param name="value">value to find</param>
        /// <param name="NotFoundHash">hash not found</param>
        /// <param name="NotFoundKeyOne">hash hit one entry but key not found</param>
        /// <param name="FoundOne">hash hit one entry and key found</param>
        /// <param name="NotFoundKeyList">hash hit several entries but key not found</param>
        /// <param name="FoundList">hash hit several entries and key found</param>
        void FindItem(K key, bool compareValue, V value,
            Action<int> NotFoundHash,                               // hash not found
            Action<int, KeyValue> NotFoundKeyOne,                   // hash hit one entry but key not found
            Action<int, KeyValue> FoundOne,                         // hash hit one entry and key found
            Action<int, List<KeyValue>> NotFoundKeyList,            // hash hit several entries but key not found
            Action<int, KeyValue, List<KeyValue>, int> FoundList)   // hash hit several entries and key found
        {
            var hash = key.GetHashCode();
            object item;
            if ( !items.TryGetValue(hash, out item) ) {
                NotFoundHash(hash); // hash not found
            } else {
                KeyValue kv;
                if ( item is KeyValue ) {
                    kv = (KeyValue)item;
                    if ( !kv.key.Equals(key) || ( compareValue && !kv.value.Equals(value) ) ) {
                        NotFoundKeyOne(hash, kv); // hash hit one entry but key not found
                    } else {
                        FoundOne(hash, kv); // hash hit one entry and key found
                    }
                } else {
                    var list = (List<KeyValue>)item;
                    var i = list.FindIndex(i2 => i2.key.Equals(key));
                    if ( i < 0 ) {
                        NotFoundKeyList(hash, list); // hash hit several entries but key not found
                    } else {
                        kv = list[i];
                        if ( compareValue && !kv.value.Equals(value) ) {
                            NotFoundKeyList(hash, list); // hash hit several entries but key not found
                        } else {
                            FoundList(hash, kv, list, i); // hash hit several entries and key found
                        }
                    }
                }
            }
        }

        IEnumerator<KeyValue> GetEnumeratorCore(IDictionary<int, object> items)
        {
            foreach ( var item in items )
                if ( item.Value is KeyValue ) {
                    var kv = (KeyValue)item.Value;
                    yield return kv;
                } else {
                    var list = (List<KeyValue>)item.Value;
                    foreach ( var kv in list )
                        yield return kv;
                }
        }

        #region IDictionary<K,V> メンバ

        public void Add(K key, V value)
        {
            AddCore(key, value, true);
        }

        public bool ContainsKey(K key)
        {
            V value;
            return TryGetValueCore(key, out value);
        }

        public ICollection<K> Keys
        {
            get { return new KeyCollection(this); }
        }

        /// <summary>
        /// Collection that is readonly and invalidated when an item is 
        /// added to or removed from the dictionary.
        /// </summary>
        abstract class KeysValuesBase<T>: ICollection<T>, IDisposable
        {
            protected bool Invalid = false;
            protected RehashableDictionary<K, V> Dictionary;
            public KeysValuesBase(RehashableDictionary<K, V> dictionary)
            {
                Dictionary = dictionary;
                Dictionary.Added += DictionaryChanged;
                Dictionary.Removed += DictionaryChanged;
            }
            public void Dispose()
            {
                Dictionary.Added -= DictionaryChanged;
                Dictionary.Removed -= DictionaryChanged;
            }
            void DictionaryChanged(object sender, RehashableDictionary<K, V>.DictionaryEventArgs e)
            {
                Invalid = true;
            }

            protected void CheckValid()
            {
                if ( Invalid )
                    throw new InvalidOperationException(
                        "Dictionary was modified after this collection was created.");
            }

            static void ThrowReadOnlyError()
            {
                throw new InvalidOperationException("Collection is readonly.");
            }

            #region ICollection<K> メンバ

            public void Add(T item)
            { ThrowReadOnlyError(); }

            public void Clear()
            { ThrowReadOnlyError(); }

            public abstract bool Contains(T item);

            public abstract void CopyTo(T[] array, int arrayIndex);

            public virtual int Count
            { get { return Dictionary.Count; } }

            public bool IsReadOnly
            { get { return true; } }

            public bool Remove(T item)
            { ThrowReadOnlyError(); return false; }

            #endregion

            #region IEnumerable<T> メンバ

            public abstract IEnumerator<T> GetEnumerator();

            #endregion

            #region IEnumerable メンバ

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        class KeyCollection: KeysValuesBase<K>
        {
            public KeyCollection(RehashableDictionary<K, V> dictionary)
                : base(dictionary)
            { }

            public override bool Contains(K item)
            {
                return Dictionary.ContainsKey(item);
            }

            public override void CopyTo(K[] array, int arrayIndex)
            {
                foreach ( var item in Dictionary ) {
                    CheckValid();
                    array[arrayIndex++] = item.Key;
                }
            }

            public override IEnumerator<K> GetEnumerator()
            {
                foreach ( var item in Dictionary ) {
                    CheckValid();
                    yield return item.Key;
                }
            }
        }

        public bool Remove(K key)
        {
            return RemoveCore(key, false, default(V));
        }

        public bool TryGetValue(K key, out V value)
        {
            return TryGetValueCore(key, out value);
        }

        public ICollection<V> Values
        {
            get { return new ValueCollection(this); }
        }

        class ValueCollection: KeysValuesBase<V>
        {
            public ValueCollection(RehashableDictionary<K, V> dictionary)
                : base(dictionary)
            { }

            public override bool Contains(V item)
            {
                return Dictionary.Any(entry=>entry.Value.Equals(item));
            }

            public override void CopyTo(V[] array, int arrayIndex)
            {
                foreach ( var item in Dictionary ) {
                    CheckValid();
                    array[arrayIndex++] = item.Value;
                }
            }

            public override IEnumerator<V> GetEnumerator()
            {
                foreach ( var item in Dictionary ) {
                    CheckValid();
                    yield return item.Value;
                }
            }
        }

        public V this[K key]
        {
            get
            {
                V value;
                if ( TryGetValueCore(key, out value) )
                    return value;
                throw new ArgumentException("Key not exist.");
            }
            set
            {
                AddCore(key, value, false);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<K,V>> メンバ

        public void Add(KeyValuePair<K, V> item)
        {
            AddCore(item.Key, item.Value, true);
        }

        public void Clear()
        {
            var oldItems = items;
            items = new SortedDictionary<int, object>();
            hashes.Clear();
            var iter= GetEnumeratorCore(oldItems);
            while(iter.MoveNext())
                OnRemoved(iter.Current.key, iter.Current.value);
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            V value;
            if ( !TryGetValueCore(item.Key, out value) )
                return false;
            return value.Equals(item.Value);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            foreach ( var item in this )
                array[arrayIndex++] = item;
        }

        public int Count
        {
            get { return hashes.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return RemoveCore(item.Key, true, item.Value);
        }

        #endregion

        #region IEnumerable<KeyValuePair<K,V>> メンバ

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            var iter = GetEnumeratorCore(items);
            while ( iter.MoveNext() )
                yield return new KeyValuePair<K, V>(iter.Current.key, iter.Current.value);
        }

        #endregion

        #region IEnumerable メンバ

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
