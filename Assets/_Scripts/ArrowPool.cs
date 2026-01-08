using System.Collections.Generic;
using UnityEngine;

/* 화살 오브젝트 풀링을 관리하는 스크립트 */

public class ArrowPool : MonoBehaviour
{
    public static ArrowPool Instance;   // 싱글톤

    [Header("Settings")]
    public GameObject arrowPrefab; // 화살 프리팹
    public int poolSize = 20;      // 처음에 미리 만들어둘 개수

    // 화살 보관 큐
    private Queue<GameObject> _poolQueue = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewArrow();
        }
    }

    private GameObject CreateNewArrow()
    {
        GameObject newArrow = Instantiate(arrowPrefab, transform);
        newArrow.SetActive(false);
        _poolQueue.Enqueue(newArrow);
        return newArrow;
    }

    public GameObject GetArrow(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        // 창고에 남은 게 있으면 꺼내 쓰고, 없으면 새로 만듦
        if (_poolQueue.Count > 0)
            obj = _poolQueue.Dequeue();
        else
            obj = CreateNewArrow();

        // 위치/회전 설정 후 켜기
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    // 반납하기
    public void ReturnArrow(GameObject arrow)
    {
        arrow.SetActive(false);
        arrow.transform.SetParent(transform);
        _poolQueue.Enqueue(arrow);
    }
}