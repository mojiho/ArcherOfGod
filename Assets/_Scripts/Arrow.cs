using UnityEngine;
using System.Collections;

/* 화살의 동작을 제어하는 스크립트 입니다. */
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    public float speed = 15f;

    private Rigidbody2D _rb;
    private Collider2D _col;

    private WaitForSeconds _wsLifeTime;
    private WaitForSeconds _wsStuckTime;

    private Coroutine _returnRoutine;

    private static readonly float STOP_SPEED_SQR = 0.0001f;    // 0 근사치 판단

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        _wsLifeTime = new WaitForSeconds(3f);
        _wsStuckTime = new WaitForSeconds(2f);
    }

    private void OnEnable()
    {
        _rb.simulated = true;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _col.enabled = true;

        _rb.linearVelocity = (Vector2)transform.right * speed;

        StartReturnRoutine(CoAutoReturn());
    }

    private void OnDisable()
    {
        StopReturnRoutine();

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    private void FixedUpdate()
    {
        // 물리 프레임에서 회전 갱신
        if (_rb.bodyType != RigidbodyType2D.Dynamic) return;

        Vector2 v = _rb.linearVelocity;
        if (v.sqrMagnitude < STOP_SPEED_SQR) return;

        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        _rb.rotation = angle;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ground")) return;

        StopReturnRoutine();

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        _rb.bodyType = RigidbodyType2D.Static;
        _rb.simulated = false;

        transform.SetParent(other.transform, true);

        StartReturnRoutine(CoStuckReturn());
    }

    private void StartReturnRoutine(IEnumerator routine)
    {
        StopReturnRoutine();
        _returnRoutine = StartCoroutine(routine);
    }

    private void StopReturnRoutine()
    {
        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }
    }

    private IEnumerator CoAutoReturn()
    {
        yield return _wsLifeTime;
        ReturnToPool();
    }

    private IEnumerator CoStuckReturn()
    {
        yield return _wsStuckTime;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        StopReturnRoutine();

        if (ArrowPool.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ArrowPool.Instance.ReturnArrow(gameObject);
    }
}
