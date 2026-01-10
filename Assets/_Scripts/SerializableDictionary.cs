using System.Collections.Generic;
using UnityEngine;

// 이 클래스를 상속받아 구체적인 타입을 만듭니다.
[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count)
        {
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null && !this.ContainsKey(keys[i]))
            {
                this.Add(keys[i], values[i]);
            }
        }
    }

    public void OnBeforeSerialize()
    {
    }
}