using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, ISkillCaster
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Attack Settings")]
    [SerializeField] private Transform firePos;
    [SerializeField] private float fireDelay = 0.7f;
    [SerializeField] private SkillInfo defaultSkillData; // 기본 공격 데이터

    [Header("Behavior")]
    [SerializeField] private bool autoFireWhenIdle = true;

    // 내부 컴포넌트
    private Character _character;
    private SkillSystem _skillSystem;

    // [핵심] 스킬 전략들을 담아두는 가방 (Strategy Dictionary)
    private Dictionary<SkillType, SkillStrategy> _skillStrategies;

    // 키 매핑
    private readonly KeyCode[] _skillKeys = { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B };

    // 상태 변수
    private Vector2 _moveInput;
    private bool _isAttacking;
    private bool _isAutoFiring; // 현재 공격이 자동 사격인지 체크

    private Coroutine _attackRoutine;
    private WaitForSeconds _wsFireDelay;

    // 상태 확인 프로퍼티 (ISkillCaster 구현용)
    public bool CanAction => !(_character != null && _character.Health.IsDead);

    // ====================================================
    // [ISkillCaster 구현] 전략 클래스들이 나를 참조할 때 사용
    // ====================================================
    public Transform GetTransform() => transform;
    public Transform GetFirePos() => firePos != null ? firePos : transform;
    public GameObject GetGameObject() => gameObject;

    private void Awake()
    {
        _character = GetComponent<Character>();
        _skillSystem = GetComponent<SkillSystem>();

        // [핵심] 전략 등록 (새로운 스킬이 생기면 여기에 추가만 하면 됨)
        _skillStrategies = new Dictionary<SkillType, SkillStrategy>
        {
            { SkillType.Normal, new NormalSkill() },
            { SkillType.MultiShot, new MultiShotSkill() }
            // 예: { SkillType.Dash, new DashSkill() }
        };
    }

    private void Start()
    {
        _wsFireDelay = new WaitForSeconds(fireDelay);

        if (_character != null)
        {
            _character.Health.OnDead += HandleDeath;
        }

        // 기본 공격 데이터 안전장치
        if (GameManager.Instance != null && GameManager.Instance.skillDatabase != null)
        {
            defaultSkillData = GameManager.Instance.skillDatabase.GetSkill(SkillType.Normal);
        }
    }

    private void Update()
    {
        if (_character.Health.IsDead) return;

        ReadMoveInput();
        ApplyMove();
        UpdateAnimation();

        HandleSkillInput(); // 1순위: 스킬 입력
        HandleAutoFire();   // 2순위: 자동 사격
    }

    // ========================================================================
    // 1. 입력 및 스킬 처리 섹션
    // ========================================================================

    private void HandleSkillInput()
    {
        if (!CanAction) return;

        for (int i = 0; i < _skillKeys.Length; i++)
        {
            if (Input.GetKeyDown(_skillKeys[i]))
            {
                TryUseSkill(i);
            }
        }
    }

    // 외부(UI)에서도 호출 가능하도록 public
    public void TryUseSkill(int slotIndex)
    {
        if (_skillSystem == null) return;
        if (slotIndex >= _skillSystem.myLoadout.Count) return;

        // A. 쿨타임 및 사용 가능 여부 체크
        SkillType type = _skillSystem.myLoadout[slotIndex];
        if (!_skillSystem.IsSkillReady(type)) return;

        // B. 인터럽트 로직: 자동 사격 중이면 끊고 스킬 발동
        if (_isAttacking)
        {
            if (_isAutoFiring)
            {
                CancelAttackInternal(); // "비켜! 스킬 나간다"
            }
            else
            {
                return; // "이미 다른 스킬 쓰는 중이야"
            }
        }

        // C. 데이터 가져오기
        SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(type);
        if (info == null) return;

        // D. 스킬 실행 (코루틴)
        StartCoroutine(PlaySkillAction(info));

        // E. 쿨타임 시스템 가동 (UI 갱신)
        _skillSystem.TriggerCooldown(type, slotIndex);
    }

    // ISkillCaster 인터페이스 구현
    public IEnumerator PlaySkillAction(SkillInfo info)
    {
        // 안전장치
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);

        _isAttacking = true;
        _isAutoFiring = false; // "이건 스킬이다" (중요)
        _attackRoutine = null;

        Transform target = GetTarget(); 
        if (target != null)
        {
            _character.Sprite.flipX = target.position.x < transform.position.x;
        }

        _character.Anim.SetBool(AnimationKey.IsAttack, true);

        yield return new WaitForSeconds(0.1f); // 선딜레이

        // [변경] 직접 쏘지 않고 전략에게 위임
        UseSkillLogic(info);

        yield return new WaitForSeconds(0.2f); // 후딜레이

        _isAttacking = false;
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
    }

    // ========================================================================
    // 2. 자동 사격 섹션
    // ========================================================================

    private void HandleAutoFire()
    {
        // 공격 중 아님 + 이동 안 함 + 설정 켜짐
        if (autoFireWhenIdle && !_isAttacking && !IsMoving())
        {
            StartAutoAttack();
        }
    }

    private void StartAutoAttack()
    {
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = StartCoroutine(CoAutoAttack());
    }

    private IEnumerator CoAutoAttack()
    {
        _isAttacking = true;
        _isAutoFiring = true; // "이건 자동 사격이다" (취소 가능)

        _character.Anim.SetBool(AnimationKey.IsAttack, true);

        yield return _wsFireDelay; // 공격 딜레이

        // 대기 중에 스킬로 인해 취소됐는지 체크
        if (_isAttacking && defaultSkillData != null)
        {
            // [변경] Fire() 대신 UseSkillLogic() 사용 (전략 패턴 통일)
            UseSkillLogic(defaultSkillData);
        }

        yield return new WaitForSeconds(0.1f);

        _isAttacking = false;
        _isAutoFiring = false;
        _attackRoutine = null;
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
    }

    // ========================================================================
    // 3. 스킬 전략 실행 (핵심 로직)
    // ========================================================================

    private void UseSkillLogic(SkillInfo info)
    {
        // 딕셔너리에 등록된 전략이 있는지 확인
        if (_skillStrategies.TryGetValue(info.type, out SkillStrategy strategy))
        {
            // "전략아, 내 정보(this) 줄 테니까 대신 쏴줘!" (위임)
            strategy.Use(this, info);
        }
        else
        {
            Debug.LogError($"[Player] {info.type} 타입에 해당하는 스킬 클래스가 Dictionary에 없습니다!");
        }
    }

    // ※ 기존의 Fire(), FireMultiShot() 함수는 모두 삭제되었습니다.
    // (SkillStrategy 클래스로 이동했기 때문입니다)

    // ========================================================================
    // 4. 유틸리티 (이동, 애니메이션, 사망)
    // ========================================================================

    private void CancelAttackInternal()
    {
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _isAttacking = false;
        _isAutoFiring = false;
        _attackRoutine = null;
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
    }

    private void ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");

        // 스킬 사용 중이면 이동 불가 (자동 사격 중엔 이동하면 캔슬)
        if (_isAttacking && !_isAutoFiring) h = 0f;
        if (_isAutoFiring && h != 0) CancelAttackInternal();

        _moveInput = new Vector2(h, 0f);
    }

    private void ApplyMove()
    {
        if (_moveInput.x == 0f) return;
        transform.Translate(_moveInput * moveSpeed * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        bool isRunning = Mathf.Abs(_moveInput.x) > 0.1f;
        _character.Anim.SetBool(AnimationKey.IsRun, isRunning);
        if (!_isAttacking && _moveInput.x != 0f) _character.Sprite.flipX = _moveInput.x < 0f;
    }

    private bool IsMoving() => Mathf.Abs(_moveInput.x) > 0.1f;

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