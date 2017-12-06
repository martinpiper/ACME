using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    public class MultiMap<K, V>
    {
        Dictionary<K, List<V>> mDictionary = new Dictionary<K, List<V>>();

        public void Add(K key, V value)
        {
            List<V> list;
            if (mDictionary.TryGetValue(key, out list))
            {
                // 2A.
                list.Add(value);
            }
            else
            {
                // 2B.
                list = new List<V>();
                list.Add(value);
                mDictionary[key] = list;
            }
        }

        public int Count
        {
            get
            {
                return mDictionary.Count;
            }
        }

        public IEnumerable<K> Keys
        {
            get
            {
                return mDictionary.Keys;
            }
        }

        public List<V> this[K key]
        {
            get
            {
                List<V> list;
                if (!mDictionary.TryGetValue(key, out list))
                {
                    list = new List<V>();
                    mDictionary[key] = list;
                }
                return list;
            }
        }
    }
}
