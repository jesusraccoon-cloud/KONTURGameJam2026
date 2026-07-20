using System;

namespace Core
{
    [Serializable]
    public class KeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public KeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
