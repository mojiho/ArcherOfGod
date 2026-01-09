using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 플레이어 움직임 컨트롤러 클래스 입니다. */
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Mobile Inputs")]
    [SerializeField] private DirectionButton btnLeft;
    [SerializeField] private DirectionButton btnRight;

    [Header("Attack Settings")]
    [SerializeField] private Transform firePos;
    [SerializeField] private float fireDelay = 0.7f;

    [Header("Behavior")]
    [SerializeField] private bool autoFireWhenIdle = true; // 서있으면 자동 연사

    [Header("Skill UI")]
    [SerializeField] private List<SkillSlotUI> skillSlots;

    private List<SkillBase> _skills;

    // Input flags (UI)
    private bool _attackPressed;

    // Components
    private Animator _animator;
    private SpriteRenderer _sprite;

    // State
    private Vector2 _moveInput;
    private bool _isAttacking;

    // Coroutine / cached waits
    private Coroutine _attackRoutine;
    private WaitForSeconds _wsFireDelay;
    private static readonly WaitForSeconds WS_ATTACK_END = new WaitForSeconds(0.1f);

    // Tunables
    private const float INPUT_DEADZONE = 0.1f;

    private bool IsMoving => Mathf.Abs(_moveInput.x) >= INPUT_DEADZONE;

    // "정지 상태에서만 공격 시작" 규칙
    private bool CanStartAttack => !_isAttacking && !IsMoving;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _wsFireDelay = new WaitForSeconds(fireDelay);
    }

    private void Update()
    {
        ReadMoveInput();
        ApplyMove();

        UpdateAnimation();

        HandleAttackInput();
        HandleAutoFire();
    }

    private void ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");

        if (btnLeft != null && btnLeft.IsPressed) h = -1f;
        if (btnRight != null && btnRight.IsPressed) h = 1f;
        if (btnLeft != null && btnRight != null && btnLeft.IsPressed && btnRight.IsPressed)
            h = 0f;

        if (Mathf.Abs(h) < INPUT_DEADZONE) h = 0f;

        if (_isAttacking && h != 0f)
        {
            CancelAttackInternal(clearQueuedAttack: true);
        }

        _moveInput = new Vector2(h, 0f);
    }

    public void OnAttackButtonDown()
    {
        _attackPressed = true;
    }

    private void ApplyMove()
    {
        if (_moveInput.x == 0f) return;
        transform.Translate(_moveInput * moveSpeed * Time.deltaTime);
    }

    private void HandleAttackInput()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Z) || _attackPressed;
        if (!pressed) return;

        // 이동 중 눌러도 멈추자마자 나가는 "지연 발사" 방지
        _attackPressed = false;

        if (!CanStartAttack) return;

        StartAttack();
    }

    private void HandleAutoFire()
    {
        if (!autoFireWhenIdle) return;
        if (!CanStartAttack) return;
        if (_attackRoutine != null) return;   // 중복 시작 방지

        StartAttack();
    }

    private void StartAttack()
    {
        // 안전: 혹시 남아있으면 정리
        StopAttackCoroutineOnly();

        _attackRoutine = StartCoroutine(CoAttack());
    }

    private IEnumerator CoAttack()
    {
        _isAttacking = true;
        _moveInput = Vector2.zero;

        _animator.SetBool(AnimationKey.IsAttack, true);

        yield return _wsFireDelay;

        // 공격 도중 취소되었으면 발사하지 않음
        if (_isAttacking)
            Fire();

        yield return WS_ATTACK_END;

        FinishAttack();
    }

    private void FinishAttack()
    {
        _isAttacking = false;
        _attackRoutine = null;

        _animator.SetBool(AnimationKey.IsAttack, false);
    }

    // 외부에서 호출할 수 있는 취소 함수
    public void CancelAttack()
    {
        CancelAttackInternal(clearQueuedAttack: false);
    }

    private void CancelAttackInternal(bool clearQueuedAttack)
    {
        StopAttackCoroutineOnly();
        _isAttacking = false;

        _animator.SetBool(AnimationKey.IsAttack, false);

        if (clearQueuedAttack)
            _attackPressed = false;
    }

    private void StopAttackCoroutineOnly()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
    }

    public void SetAttackSpeed(float newDelay)
    {
        fireDelay = newDelay;
        _wsFireDelay = new WaitForSeconds(fireDelay);
    }

    private void Fire()
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = firePos != null ? firePos.position : transform.position;

        // 시선 방향(FlipX)에 따라 좌/우 + 곡사 40도
        Quaternion rot = _sprite.flipX
            ? Quaternion.Euler(0, 180, 40)
            : Quaternion.Euler(0, 0, 40);

        ArrowPool.Instance.GetArrow(spawnPos, rot);
    }


    private void UpdateAnimation()
    {
        _animator.SetBool(AnimationKey.IsRun, IsMoving);

        if (_isAttacking) return;

        if (_moveInput.x != 0f)
            _sprite.flipX = _moveInput.x < 0f;
    }
}
