using UnityEngine;
using System.Collections;

/* 화살의 동작을 제어하는 스크립트 */
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    public float speed = 15f;

    private Rigidbody2D _rb;
    private Collider2D _col;

    // [최적화] 대기 시간 객체 캐싱
    private WaitForSeconds _wsLifeTime;  // 날아가는 최대 시간
    private WaitForSeconds _wsStuckTime; // 박혀있는 시간

    // 현재 실행 중인 반납 코루틴을 저장 (멈추기 위해 필요)
    private Coroutine _returnRoutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // 게임 시작 시 한 번만 생성 (최적화)
        _wsLifeTime = new WaitForSeconds(3f);
        _wsStuckTime = new WaitForSeconds(2f);
    }

    private void OnEnable()
    {
        // 초기화
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _col.enabled = true;

        _rb.linearVelocity = transform.right * speed;

        // 반납 코루틴 시작
        _returnRoutine = StartCoroutine(CoAutoReturn());
    }

    private void OnDisable()
    {
        _returnRoutine = null;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    private void Update()
    {
        // 날아가는 중(Dynamic)일 때만 회전
        if (_rb.bodyType == RigidbodyType2D.Dynamic)
        {
            Vector2 v = _rb.linearVelocity;
            if (v != Vector2.zero)
            {
                float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            // 실행 중인 코루틴 중지
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);

            _rb.simulated = false;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;

            // 바닥의 자식으로 등록 (바닥이 움직여도 화살이 고정됨)
            transform.SetParent(other.transform);

            // 박혀있는 상태용 코루틴 시작 (2초 뒤 반납)
            _returnRoutine = StartCoroutine(CoStuckReturn());
        }
    }

    // 발사 후 자동 반납 코루틴
    private IEnumerator CoAutoReturn()
    {
        yield return _wsLifeTime;
        ReturnToPool();
    }

    // 바닥에 박혔을 때
    private IEnumerator CoStuckReturn()
    {
        yield return _wsStuckTime;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 혹시 실행 중인 코루틴이 있다면 정리
        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }
        transform.SetParent(ArrowPool.Instance.transform);
        ArrowPool.Instance.ReturnArrow(this.gameObject);
    }
}