using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 *  이펙트 풀 클래스 입니다.
 */
public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance;

    private Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform _tr;

    private void Awake()
    {
        Instance = this;
        _tr = transform;
    }

    public GameObject PlayEffect(GameObject prefab, Vector3 position, Quaternion rotation, float duration = 2f)
    {
        if (prefab == null) return null;

        if (!_poolDictionary.ContainsKey(prefab))
            _poolDictionary.Add(prefab, new Queue<GameObject>());

        GameObject obj;
        if (_poolDictionary[prefab].Count > 0)
        {
            obj = _poolDictionary[prefab].Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, _tr);
        }

        obj.transform.position = new Vector3(position.x, position.y, -1f);

        obj.transform.rotation = rotation;

        obj.SetActive(true);

        StartCoroutine(ReturnRoutine(prefab, obj, duration));
        return obj;
    }

    private IEnumerator ReturnRoutine(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (instance == null) yield break;

        instance.SetActive(false);
        if (_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary[prefab].Enqueue(instance);
        }
    }
}