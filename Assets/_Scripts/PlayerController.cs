using UnityEngine;
using System.Collections;

/* 플레이어 캐릭터를 제어하는 스크립트 */

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Mobile Inputs")]
    [SerializeField] private DirectionButton btnLeft;  // 왼쪽 버튼 연결
    [SerializeField] private DirectionButton btnRight; // 오른쪽 버튼 연결

    [Header("Attack Settings")]
    [SerializeField] private Transform firePos;         // 화살 발사 위치 오브젝트
    [SerializeField] private float fireDelay = 0.7f;    // 버튼 누르고 화살이 나갈 때까지 걸리는 시간

    // 공격용 (버튼 클릭 방식)
    private bool isAttackButtonPressed = false;

    // 내부 컴포넌트
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _inputVec;

    // [최적화] 코루틴 관련 변수
    private WaitForSeconds _wsFireDelay; // 대기 시간 객체 재활용
    private Coroutine _attackRoutine;    // 현재 실행 중인 공격 코루틴 저장
    private bool _isAttacking = false;   // 공격 상태 플래그

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 게임 시작 시 대기 시간 객체를 미리 만들어둠 (최적화)
        _wsFireDelay = new WaitForSeconds(fireDelay);
    }

    // 공격속도 변경 시 호출
    public void SetAttackSpeed(float newDelay)
    {
        fireDelay = newDelay;
        _wsFireDelay = new WaitForSeconds(fireDelay);
    }

    private void Update()
    {
        HandleInput();
        Move();
        //HandleAttack();
        if (_inputVec.sqrMagnitude < 0.01f && !_isAttacking)
        {
            _attackRoutine = StartCoroutine(CoAttack());
        }
        UpdateAnimation();
    }

    private void HandleInput()
    {
        // 공격 중 이면 이동 불가
        if (_isAttacking)
        {
            _inputVec = Vector2.zero;
            return;
        }

        // 키보드 입력 (PC 테스트용)
        float h = Input.GetAxisRaw("Horizontal");

        // 버튼 입력
        if (btnLeft != null && btnLeft.IsPressed) h = -1f;
        if (btnRight != null && btnRight.IsPressed) h = 1f;
        if (btnLeft != null && btnRight != null && btnLeft.IsPressed && btnRight.IsPressed) h = 0f;

        _inputVec = new Vector2(h, 0);
    }

    private void Move()
    {
        transform.Translate(_inputVec * moveSpeed * Time.deltaTime);
    }

    private void HandleAttack()
    {
        // 이미 공격 중이면 중복 실행 방지
        if (_isAttacking) return;

        // Z키 또는 공격 버튼 플래그 체크
        if (Input.GetKeyDown(KeyCode.Z) || isAttackButtonPressed)
        {
            isAttackButtonPressed = false;

            // 혹시 실행 중인 코루틴이 있다면 정지 (안전장치)
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);

            // 공격 코루틴 시작
            _attackRoutine = StartCoroutine(CoAttack());
        }
    }

    // 공격 흐름을 제어하는 코루틴
    private IEnumerator CoAttack()
    {
        _isAttacking = true;
        _inputVec = Vector2.zero; // 즉시 정지
        _animator.SetTrigger(AnimationKey.Attack);

        yield return _wsFireDelay;

        Fire();

        yield return new WaitForSeconds(0.1f);

        _isAttacking = false;
        _attackRoutine = null;
    }

    // 피격 등으로 공격을 강제 취소해야 할 때 호출
    public void CancelAttack()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
            _isAttacking = false;
        }
    }

    private void UpdateAnimation()
    {
        // 공격 중일 때는 달리기 중지
        if (_isAttacking) return;

        bool isMoving = _inputVec.magnitude > 0;
        _animator.SetBool(AnimationKey.IsRun, isMoving);

        if (_inputVec.x != 0)
        {
            _spriteRenderer.flipX = _inputVec.x < 0;
        }
    }

    public void OnAttackButtonDown()
    {
        isAttackButtonPressed = true;
    }

    public void Fire()
    {
        Vector3 spawnPos = firePos != null ? firePos.position : transform.position;

        Quaternion rotation;

        // 곡사 발사를 위해 위로 30도 회전 좌 우 고려
        if (_spriteRenderer.flipX)
        {
            // 좌
            rotation = Quaternion.Euler(0, 180, 30);
        }
        else
        {
            // 우
            rotation = Quaternion.Euler(0, 0, 30);
        }

        ArrowPool.Instance.GetArrow(spawnPos, rotation);
    }
}