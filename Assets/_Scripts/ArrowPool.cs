using System.Collections.Generic;
using UnityEngine;

/* 화살 오브젝트 풀링을 관리하는 스크립트 */
public class ArrowPool : MonoBehaviour
{
    public static ArrowPool Instance;

    [Header("Settings")]
    public GameObject arrowPrefab;
    public int poolSize = 20;

    private Queue<GameObject> _poolQueue;
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
        _poolQueue = new Queue<GameObject>(poolSize);

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
            CreateNewArrow();
    }

    private GameObject CreateNewArrow()
    {
        var newArrow = Instantiate(arrowPrefab, _tr);
        newArrow.SetActive(false);
        _poolQueue.Enqueue(newArrow);
        return newArrow;
    }

    public GameObject GetArrow(Vector3 position, Quaternion rotation)
    {
        var obj = (_poolQueue.Count > 0) ? _poolQueue.Dequeue() : CreateNewArrow();

        var t = obj.transform;
        t.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    public void ReturnArrow(GameObject arrow)
    {
        if (arrow == null) return;

        // 풀 상태 정리
        arrow.SetActive(false);
        arrow.transform.SetParent(_tr, false);
        _poolQueue.Enqueue(arrow);
    }
}
