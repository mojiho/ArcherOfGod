using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MultiShotSkill;

[RequireComponent(typeof(Rigidbody2D))] // 리지드바디 필수
public class PlayerController : MonoBehaviour, ISkillCaster
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpShotPower = 15f; // 점프샷 높이
    private bool _isDashing = false; 
    private bool _isJumping = false; 

    [Header("Attack Settings")]
    [SerializeField] private Transform firePos;
    [SerializeField] private float fireDelay = 0.7f;
    [SerializeField] private SkillInfo defaultSkillData;

    [Header("Behavior")]
    [SerializeField] private bool autoFireWhenIdle = true;

    private Character _character;
    private SkillSystem _skillSystem;
    private Rigidbody2D _rb; // [추가] 물리 제어용

    private Dictionary<SkillType, SkillStrategy> _skillStrategies;
    private readonly KeyCode[] _skillKeys = { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B };

    private Vector2 _moveInput;
    private bool _isAttacking;
    private bool _isAutoFiring;

    private Coroutine _attackRoutine;
    private WaitForSeconds _wsFireDelay;

    public bool CanAction => !(_character != null && _character.Health.IsDead);

    public Transform GetTransform() => transform;
    public Transform GetFirePos() => firePos != null ? firePos : transform;
    public GameObject GetGameObject() => gameObject;

    private void Awake()
    {
        _character = GetComponent<Character>();
        _skillSystem = GetComponent<SkillSystem>();
        _rb = GetComponent<Rigidbody2D>(); // [중요] 컴포넌트 가져오기

        _skillStrategies = new Dictionary<SkillType, SkillStrategy>
        {
            { SkillType.Normal, new NormalSkill() },
            { SkillType.MultiShot, new MultiShotSkill() },
            { SkillType.DirectShot, new DirectShotSkill() },
            { SkillType.Dash, new DashSkill() },
            { SkillType.JumpShot, new JumpShotSkill() },
            { SkillType.ClusterShot, new ClusterShotSkill() }
        };
    }

    private void Start()
    {
        _wsFireDelay = new WaitForSeconds(fireDelay);
        if (_character != null) _character.Health.OnDead += HandleDeath;
        if (GameManager.Instance != null && GameManager.Instance.skillDatabase != null)
            defaultSkillData = GameManager.Instance.skillDatabase.GetSkill(SkillType.Normal);
    }

    private void Update()
    {
        if (_character.Health.IsDead) return;

        // 1. 입력 감지
        ReadMoveInput();

        // 2. 애니메이션 & 방향 보기 (여기서 적 바라보기 처리)
        UpdateAnimation();

        // 점프 중이거나 대쉬 중일 때는 스킬/공격 입력 불가
        if (!_isJumping && !_isDashing)
        {
            HandleSkillInput();
            HandleAutoFire();
        }
    }

    // [핵심 변경] 물리 이동은 FixedUpdate에서 처리해야 부드럽습니다.
    private void FixedUpdate()
    {
        if (_character.Health.IsDead) return;
        ApplyMovePhysics();
    }

    // ========================================================================
    // 이동 관련 (물리 적용)
    // ========================================================================

    private void ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (_isAttacking && !_isAutoFiring) h = 0f;
        if (_isAutoFiring && h != 0) CancelAttackInternal();
        _moveInput = new Vector2(h, 0f);
    }

    private void ApplyMovePhysics()
    {
        // [핵심] 대쉬 중일 때는 플레이어의 키 입력(속도 0 만들기)을 무시해야 쭉 미끄러짐
        if (_isDashing) return;

        // 점프 중에는 공중 이동 제약 (원한다면 0.5f 곱해서 느리게 이동 가능)
        float currentSpeed = _isJumping ? moveSpeed * 0.3f : moveSpeed;

        if (_moveInput.x != 0)
        {
            _rb.linearVelocity = new Vector2(_moveInput.x * currentSpeed, _rb.linearVelocity.y);
        }
        else
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
        }
    }
    public void StartDashPhysics()
    {
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true; // 이동 입력 무시 시작
        _isAttacking = true; // 다른 스킬 사용 불가
        _character.Anim.SetBool(AnimationKey.IsRun, true); // 달리기 모션 or 대쉬 모션

        yield return new WaitForSeconds(0.4f); // 0.4초 동안 대쉬 관성 유지

        _rb.linearVelocity = Vector2.zero; // 대쉬 끝, 정지
        _isDashing = false;
        _isAttacking = false;
        _character.Anim.SetBool(AnimationKey.IsRun, false);
    }
    public void StartJumpShotSequence(SkillInfo info)
    {
        StartCoroutine(JumpShotRoutine(info));
    }

    private IEnumerator JumpShotRoutine(SkillInfo info)
    {
        // 1. 상태 잠금 (이동, 공격, 스킬 불가)
        _isJumping = true;
        _isAttacking = true;
        _isAutoFiring = false;

        // 2. 슈퍼 점프 (다이렉트샷 회피)
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0); // Y속도 리셋
        _rb.AddForce(Vector2.up * jumpShotPower, ForceMode2D.Impulse);

        // 3. 공중 회전 (360도) - DOTween이 없으므로 코루틴으로 회전
        float rotateDuration = 0.4f;
        float elapsed = 0f;
        float startZ = 0f; // 2D 스프라이트 회전은 Z축

        // 회전하는 동안 화살 발사
        bool fired = false;

        // 스프라이트 렌더러가 아니라 부모나 별도 회전체를 돌려야 하는데,
        // 여기서는 간단히 SpriteRenderer가 아닌 GameObject 자체를 돌리거나
        // 연출용으로 Anim을 쓰는게 좋지만, 요청대로 코드 회전 구현:
        Transform visual = _character.Sprite.transform;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            // Z축 회전 (0 -> 360)
            float z = Mathf.Lerp(0, 360, t);
            visual.localRotation = Quaternion.Euler(0, 0, z);

            // 중간(50%) 쯤에 화살 발사
            if (!fired && t >= 0.5f)
            {
                fired = true;
                FireSkillArrow(info); // 내부 발사 로직
            }

            yield return null;
        }
        visual.localRotation = Quaternion.identity; // 회전 원복

        // 4. 착지 대기 (땅에 닿을 때까지)
        while (!IsGrounded())
        {
            yield return null;
        }

        // 5. 상태 해제
        _isJumping = false;
        _isAttacking = false;
    }


    private void FireSkillArrow(SkillInfo info)
    {
        // 점프샷 전용 발사 (전략 클래스 안 쓰고 직접 발사하거나, Normal 전략 재사용)
        // 여기선 간단히 직선/약간 아래로 발사
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = firePos.position;
        Transform target = GetTarget();
        Vector3 dir;

        if (target != null)
            dir = (target.position - spawnPos).normalized;
        else
            dir = transform.localScale.x > 0 ? new Vector3(1, -0.5f, 0).normalized : new Vector3(-1, -0.5f, 0).normalized;

        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null)
        {
            Arrow arrow = arrowObj.GetComponent<Arrow>();
            if (arrow != null)
            {
                arrow.SetGravity(9.81f);
                arrow.Launch(info, info.arrowPrefab, dir * 20f, gameObject);
            }
        }
    }

    private bool IsGrounded()
    {
        return Mathf.Abs(_rb.linearVelocity.y) < 0.01f;
    }

    private void UpdateAnimation()
    {
        bool isRunning = Mathf.Abs(_moveInput.x) > 0.1f;
        _character.Anim.SetBool(AnimationKey.IsRun, isRunning);

        // 공격 중이 아닐 때만 방향 전환 허용
        if (!_isAttacking)
        {
            if (isRunning)
            {
                // [이동 중] 입력 방향 바라보기
                _character.Sprite.flipX = _moveInput.x < 0f;
            }
            else
            {
                // [멈춤] 적 바라보기 (요청하신 기능)
                Transform target = GetTarget();
                if (target != null)
                {
                    // 적이 왼쪽에 있으면 flipX = true (왼쪽 봄)
                    _character.Sprite.flipX = target.position.x < transform.position.x;
                }
            }
        }
    }

    private bool IsMoving() => Mathf.Abs(_moveInput.x) > 0.1f;

    // ... (아래 HandleSkillInput, PlaySkillAction 등 나머지 코드는 기존과 동일 유지) ...

    private void HandleSkillInput()
    {
        if (!CanAction) return;
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            if (Input.GetKeyDown(_skillKeys[i])) TryUseSkill(i);
        }
    }

    public void TryUseSkill(int slotIndex)
    {
        if (_skillSystem == null) return;
        if (slotIndex >= _skillSystem.myLoadout.Count) return;
        if (!_skillSystem.IsSkillReady(slotIndex)) return;

        if (_isAttacking)
        {
            if (_isAutoFiring) CancelAttackInternal();
            else return;
        }

        SkillType type = _skillSystem.myLoadout[slotIndex];
        SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(type);
        if (info == null) return;

        StartCoroutine(PlaySkillAction(info));
        _skillSystem.TriggerCooldown(slotIndex);
    }

    public IEnumerator PlaySkillAction(SkillInfo info)
    {
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _isAttacking = true;
        _isAutoFiring = false;
        _attackRoutine = null;

        Transform target = GetTarget();
        if (target != null) _character.Sprite.flipX = target.position.x < transform.position.x;

        _character.Anim.SetBool(AnimationKey.IsAttack, true);
        yield return new WaitForSeconds(0.1f);
        UseSkillLogic(info);
        yield return new WaitForSeconds(0.2f);

        _isAttacking = false;
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
    }

    private void HandleAutoFire()
    {
        if (autoFireWhenIdle && !_isAttacking && !IsMoving()) StartAutoAttack();
    }

    private void StartAutoAttack()
    {
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = StartCoroutine(CoAutoAttack());
    }

    private IEnumerator CoAutoAttack()
    {
        _isAttacking = true;
        _isAutoFiring = true;
        _character.Anim.SetBool(AnimationKey.IsAttack, true);
        yield return _wsFireDelay;

        if (_isAttacking && defaultSkillData != null) UseSkillLogic(defaultSkillData);
        yield return new WaitForSeconds(0.1f);

        _isAttacking = false;
        _isAutoFiring = false;
        _attackRoutine = null;
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
    }

    private void UseSkillLogic(SkillInfo info)
    {
        if (_skillStrategies.TryGetValue(info.type, out SkillStrategy strategy))
            strategy.Use(this, info);
        else
            Debug.LogError($"[Player] {info.type} 전략 없음");
    }

    private void CancelAttackInternal()
    {
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _isAttacking = false;
        _isAutoFiring = false;
        _attackRoutine = null;
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
    }

    private void HandleDeath()
    {
        StopAllCoroutines();
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        _character.Anim.SetTrigger(AnimationKey.Die);
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(2.0f);
        gameObject.SetActive(false);
    }

    public Transform GetTarget()
    {
        GameObject target = GameObject.FindGameObjectWithTag("Enemy");
        return target != null ? target.transform : null;
    }
}