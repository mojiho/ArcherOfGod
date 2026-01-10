using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

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