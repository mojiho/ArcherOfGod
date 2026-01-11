using System.Collections.Generic;
using UnityEngine;

/* 화살 오브젝트 풀 스크립트 입니다. */
public class ArrowPool : MonoBehaviour
{
    public static ArrowPool Instance;

    private Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform _tr;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _tr = transform; 
    }


    public GameObject GetArrow(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        Queue<GameObject> queue = _poolDictionary[prefab];
        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
            obj.transform.SetParent(_tr);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, _tr);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.localScale = Vector3.one;
        obj.SetActive(true);

        return obj;
    }

    public void ReturnArrow(GameObject prefab, GameObject instance)
    {
        if (instance == null || prefab == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(_tr);

        if (!_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        _poolDictionary[prefab].Enqueue(instance);
    }
}