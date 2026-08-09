namespace System.Collections.Concurrent;

internal static class ConcurrentDictionaryCompatibilityExtensions
{
    public static bool TryRemove<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        KeyValuePair<TKey, TValue> item)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
    }
}
