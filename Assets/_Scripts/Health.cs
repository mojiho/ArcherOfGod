using UnityEngine;
using System;

public interface IDamageable
{
    void TakeDamage(int amount, Vector3 hitDirection = default);
}
/*
 *  캐릭터의 HP를 담당하는 스크립트 입니다.
 */

public class Health : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHp = 111;
    private int _currentHp;
    private bool _isDead;

    public event Action<int, int> OnHpChanged;      // (현재 HP, 최대 HP)
    public event Action<int, Vector3, Vector3> OnDamageTaken; // (데미지, 위치, 방향)
    public event Action OnDead;                      // 사망 시 호출

    public bool IsDead => _isDead;

    private void Awake()
    {
        _currentHp = maxHp;
    }

    public void TakeDamage(int amount, Vector3 hitDirection = default)
    {

        if (_isDead) return;

        _currentHp = Mathf.Max(0, _currentHp - amount);

        if (OnDamageTaken != null)
        {
            Vector3 popupPos = transform.position + Vector3.up * 1.5f;
            OnDamageTaken(amount, popupPos, hitDirection);
        }

        if (OnHpChanged != null)
        {
            OnHpChanged(_currentHp, maxHp);
        }

        if (_currentHp <= 0) Die();
    }

    private void Die()
    {
        _isDead = true;
        if (OnDead != null) OnDead();
    }
}