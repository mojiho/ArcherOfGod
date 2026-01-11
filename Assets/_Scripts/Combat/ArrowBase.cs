using System.Collections;
using UnityEngine;

/*
 *  화살의 기본 동작을 담당하는 추상 클래스 입니다.
 */

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class ArrowBase : MonoBehaviour
{
    protected SkillInfo info;
    protected GameObject originPrefab;
    protected Vector3 initialVelocity;
    protected bool isLaunched = false;

    // Components & Caching
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected WaitForSeconds wsLifeTime;
    protected WaitForSeconds wsStuckTime;
    protected Coroutine flightCoroutine;

    protected GameObject owner;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        wsLifeTime = new WaitForSeconds(3f);
        wsStuckTime = new WaitForSeconds(2f);
    }

    public virtual void Launch(SkillInfo skillInfo, GameObject prefab, Vector3 velocity, GameObject owner)
    {
        info = skillInfo;
        originPrefab = prefab;
        initialVelocity = velocity;
        this.owner = owner;
        isLaunched = true;

        transform.SetParent(null);

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        col.enabled = true;

        StopFlight();
        flightCoroutine = StartCoroutine(FlightSequence());
    }

    protected abstract IEnumerator FlightSequence();

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLaunched) return;

        if (other.gameObject == owner) return;

        if (owner != null && other.CompareTag(owner.tag)) return;

        if (other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            OnHitEntity(other);
            ReturnToPool();
        }
        else if (other.CompareTag("Ground"))
        {
            StartCoroutine(StuckRoutine(other.transform));
        }
    }

    protected virtual void OnHitEntity(Collider2D entity)
    {
        IDamageable target = entity.GetComponent<IDamageable>();
        if (target == null) target = entity.GetComponentInParent<IDamageable>();

        if (target != null)
        {
            int damage = (info != null && info.damage > 0) ? info.damage : 10;

            Vector3 hitDir = rb.linearVelocity.normalized;

            if (hitDir == Vector3.zero) hitDir = transform.right;

            target.TakeDamage(damage, hitDir);
        }
    }

    protected IEnumerator StuckRoutine(Transform ground)
    {
        isLaunched = false;
        StopFlight();

        // 물리 연산 중단 및 고정
        rb.simulated = false;
        transform.SetParent(ground, true);

        yield return wsStuckTime;
        ReturnToPool();
    }

    protected void StopFlight()
    {
        if (flightCoroutine != null)
        {
            StopCoroutine(flightCoroutine);
            flightCoroutine = null;
        }
    }

    protected void ReturnToPool()
    {
        isLaunched = false;
        StopFlight();
        ArrowPool.Instance.ReturnArrow(originPrefab, gameObject);
    }
}