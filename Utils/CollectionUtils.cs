using System.Collections.Generic;

namespace HeadVoiceSelector.Utils;

public static class CollectionUtils
{
    public static int GetIndexOfKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        var index = 0;

        foreach (var kvp in dictionary)
        {
            if (kvp.Key.Equals(key)) return index;
            
            index++;
        }

        return -1;
    }
    
    public static int GetIndexOfVal<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue val)
    {
        var index = 0;

        foreach (var kvp in dictionary)
        {
            if (kvp.Value.Equals(val)) return index;
            
            index++;
        }

        return -1;
    }
    
    public static KeyValuePair<TKey, TValue> GetKvpFromIndex<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, int index)
    {
        var counter = 0;
        
        foreach (var kvp in dictionary)
        {
            if (counter == index) return kvp;
            
            counter++;
        }

        return default;
    }
}