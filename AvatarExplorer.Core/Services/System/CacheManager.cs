namespace AvatarExplorer.Core.Services.System;

/// <summary>キーと値のペアをメモリ上にキャッシュする汎用のキャッシュマネージャー。キーが存在しない場合は既定値を返します。</summary>
/// <typeparam name="T">キーの型。null 非許容である必要があります。</typeparam>
/// <typeparam name="TValue">値の型。</typeparam>
public class CacheManager<T, TValue>(TValue? defaultValue = default) where T : notnull
{
    private readonly TValue? defaultValue = defaultValue;
    private readonly Dictionary<T, TValue> _cache = [];

    /// <summary>指定したキーと値をキャッシュに追加（上書き）します。</summary>
    /// <param name="key">キャッシュのキー。</param>
    /// <param name="value">紐付ける値。</param>
    public virtual void Add(T key, TValue value)
    {
        _cache[key] = value;
    }

    /// <summary>指定したキーの値を取得します。存在しない場合は既定値を out 引数に設定し、<see langword="false"/> を返します。</summary>
    /// <param name="key">取得するキー。</param>
    /// <param name="value">取得した値、またはキーが存在しない場合は既定値。</param>
    /// <returns>キーが存在すれば <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
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

    /// <summary>指定したキーの値を取得します。存在しない場合は既定値を返します。</summary>
    /// <param name="key">取得するキー。</param>
    /// <returns>キャッシュされた値、または既定値。</returns>
    public virtual TValue? Get(T key)
    {
        if (TryGetValue(key, out TValue? value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>指定したキーがキャッシュに存在するかどうかを判定します。</summary>
    /// <param name="key">確認するキー。</param>
    /// <returns>存在すれば <see langword="true"/>。</returns>
    public virtual bool ContainsKey(T key) => _cache.ContainsKey(key);
    /// <summary>指定したキーとそれに関連付けられた値をキャッシュから削除します。</summary>
    /// <param name="key">削除するキー。</param>
    /// <returns>削除に成功した場合は <see langword="true"/>。</returns>
    public virtual bool Remove(T key) => _cache.Remove(key);
    /// <summary>キャッシュのすべてのエントリを削除します。</summary>
    public virtual void Clear() => _cache.Clear();
}
