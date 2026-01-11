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


    // 특정 프리팹에 해당하는 화살을 풀에서 가져옵니다.
    public GameObject GetArrow(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        // 해당 프리팹의 풀이 없다면 새로 생성
        if (!_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        Queue<GameObject> queue = _poolDictionary[prefab];
        GameObject obj;

        // 풀에 여유가 있으면 꺼내고, 없으면 새로 생성
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, _tr);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    // 화살을 다시 해당 프리팹 풀로 반환합니다.
    public void ReturnArrow(GameObject prefab, GameObject instance)
    {
        if (instance == null || prefab == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(_tr);

        // 해당 프리팹의 풀이 있는지 확인 후 반환
        if (!_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        _poolDictionary[prefab].Enqueue(instance);
    }
}