namespace AvatarExplorer.Core.Services.System;

public class CacheManager<T, TValue>(TValue? defaultValue = default) where T : notnull
{
    private readonly TValue? defaultValue = defaultValue;
    private readonly Dictionary<T, TValue> _cache = [];

    public virtual void Add(T key, TValue value)
    {
        _cache[key] = value;
    }

    public virtual bool TryGetValue(T key, out TValue? value)
    {
        if (_cache.TryGetValue(key, out TValue? cachedValue))
        {
            value = cachedValue;
            return true;
        }

        value = defaultValue;
        return false;
    }

    public virtual TValue? Get(T key)
    {
        if (TryGetValue(key, out TValue? value))
        {
            return value;
        }

        return defaultValue;
    }

    public virtual bool ContainsKey(T key) => _cache.ContainsKey(key);
    public virtual bool Remove(T key) => _cache.Remove(key);
    public virtual void Clear() => _cache.Clear();
}
