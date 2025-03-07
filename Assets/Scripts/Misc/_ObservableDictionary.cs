using System;
using System.Collections.Generic;

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    public event Action<TKey, TValue> OnValueChanged;
    public event Action<TKey> OnValueRemoved;
    public event Action<TKey, TValue> OnValueAdded;

    public new TValue this[TKey key]
    {
        get => base[key];
        set
        {
            bool keyExists = base.ContainsKey(key);
            base[key] = value;

            if (keyExists)
                OnValueChanged?.Invoke(key, value);
            else
                OnValueAdded?.Invoke(key, value);
        }
    }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        OnValueAdded?.Invoke(key, value);
    }

    public new bool Remove(TKey key)
    {
        if (base.TryGetValue(key, out TValue value))
        {
            bool removed = base.Remove(key);
            if (removed)
                OnValueRemoved?.Invoke(key);
            return removed;
        }
        return false;
    }
}