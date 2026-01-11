using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

/* 
 *  데미지 팝업 매니저 클래스 입니다.
 *  데미지 팝업 오브젝트를 풀링하여 관리합니다.
 */

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private GameObject popupPrefab;
    private Queue<DamagePopup> _pool = new Queue<DamagePopup>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShowPopup(int damage, Vector3 position, Vector3 direction)
    {
        DamagePopup popup = (_pool.Count > 0) ? _pool.Dequeue() : Instantiate(popupPrefab).GetComponent<DamagePopup>();

        popup.gameObject.SetActive(true);
        popup.transform.position = position;
        popup.Setup(damage, direction);
    }

    public void ReturnPopup(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        _pool.Enqueue(popup);
    }
}