using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * 플레이어 움직임 및 스킬 시스템을 관리하는 메인 컨트롤러 클래스입니다.
 * SkillData 시트를 읽어 실시간 스킬 객체(SkillBase)를 생성하여 관리합니다.
 */
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
    [SerializeField] private bool autoFireWhenIdle = true;

    [Header("Skill UI")]
    [SerializeField] private List<SkillSlotUI> skillSlots;

    [Header("Skill Configuration")]
    [SerializeField] private SkillData skillDataSheet;

    // 런타임에 생성된 실제 스킬 객체들의 리스트
    private List<SkillBase> _skills;

    // Components
    private Animator _animator;
    private SpriteRenderer _sprite;

    // State
    private Vector2 _moveInput;
    private bool _isAttacking;
    private bool _attackPressed;

    // Coroutine / cached waits
    private Coroutine _attackRoutine;
    private WaitForSeconds _wsFireDelay;
    private static readonly WaitForSeconds WS_ATTACK_END = new WaitForSeconds(0.1f);

    private const float INPUT_DEADZONE = 0.1f;
    private bool IsMoving => Mathf.Abs(_moveInput.x) >= INPUT_DEADZONE;
    private bool CanStartAttack => !_isAttacking && !IsMoving;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _wsFireDelay = new WaitForSeconds(fireDelay);

        // 스킬 시트로부터 실시간 객체들 생성 및 UI 연결
        InitSkills();
    }

    private void Update()
    {
        ReadMoveInput();
        ApplyMove();
        UpdateAnimation();

        HandleAttackInput();
        HandleAutoFire();

        UpdateSkillSystem();
    }

    private void InitSkills()
    {
        if (skillDataSheet == null) return;

        _skills = new List<SkillBase>();

        for (int i = 0; i < skillDataSheet.skills.Count; i++)
        {
            SkillInfo info = skillDataSheet.skills[i];
            SkillBase skillInstance = null;

            switch (info.type)
            {
                //case SkillType.TripleShot:
                //    skillInstance = new Skill_TripleShot(this, info);
                //    break;
                    // case SkillType.PowerShot: skillInstance = new Skill_PowerShot(this, info); break;
            }

            if (skillInstance != null)
            {
                _skills.Add(skillInstance);

                if (i < skillSlots.Count)
                {
                    skillSlots[i].button.image.sprite = info.icon;
                    int index = i;
                    skillSlots[i].button.onClick.AddListener(() => TryUseSkill(index));
                }
            }
        }
    }

    private void UpdateSkillSystem()
    {
        if (_skills == null) return;

        for (int i = 0; i < _skills.Count; i++)
        {
            // 1. 쿨타임 감소
            _skills[i].UpdateCooldown(Time.deltaTime);

            // 2. UI 갱신 (SkillSlotUI 활용)
            if (i < skillSlots.Count)
            {
                float current = _skills[i].CurrentCooldown;
                float max = _skills[i].Cooldown;

                skillSlots[i].cooldownMask.fillAmount = (max > 0) ? current / max : 0;
                skillSlots[i].cooldownText.text = (current > 0) ? current.ToString("F1") : "";
            }
        }
    }

    private void TryUseSkill(int index)
    {
        if (_skills == null || index >= _skills.Count) return;

        // 쿨타임 체크 및 공격 상태 확인
        if (_skills[index].CanUse() && !_isAttacking)
        {
            StartCoroutine(CoSkillAttack(_skills[index]));
        }
    }

    private IEnumerator CoSkillAttack(SkillBase skill)
    {
        _isAttacking = true;
        _moveInput = Vector2.zero;
        _animator.SetBool(AnimationKey.IsAttack, true);

        yield return _wsFireDelay;

        // 스킬 로직 실행 및 쿨타임 시작
        skill.Use();
        skill.StartCooldown();

        yield return WS_ATTACK_END;
        FinishAttack();
    }

    // 스킬 클래스에서 호출할 공용 발사 함수
    public void FireSkillArrow(float extraAngle, float speedMultiplier = 1f)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = firePos != null ? firePos.position : transform.position;
        float baseAngle = _sprite.flipX ? 180 - 40 : 40;
        float finalAngle = _sprite.flipX ? baseAngle - extraAngle : baseAngle + extraAngle;

        Quaternion rot = Quaternion.Euler(0, 0, finalAngle);
        GameObject arrowObj = ArrowPool.Instance.GetArrow(spawnPos, rot);

        if (speedMultiplier != 1f)
        {
            Arrow arrow = arrowObj.GetComponent<Arrow>();
            if (arrow != null) arrow.speed *= speedMultiplier;
        }
    }

    /* --- 기본 공격 및 이동 로직 --- */

    private void ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (btnLeft != null && btnLeft.IsPressed) h = -1f;
        if (btnRight != null && btnRight.IsPressed) h = 1f;
        if (btnLeft != null && btnRight != null && btnLeft.IsPressed && btnRight.IsPressed) h = 0f;

        if (Mathf.Abs(h) < INPUT_DEADZONE) h = 0f;

        if (_isAttacking && h != 0f) CancelAttackInternal(true);
        _moveInput = new Vector2(h, 0f);
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

        _attackPressed = false;
        if (!CanStartAttack) return;
        StartAttack();
    }

    private void HandleAutoFire()
    {
        if (!autoFireWhenIdle || !CanStartAttack || _attackRoutine != null) return;
        StartAttack();
    }

    private void StartAttack()
    {
        StopAttackCoroutineOnly();
        _attackRoutine = StartCoroutine(CoAttack());
    }

    private IEnumerator CoAttack()
    {
        _isAttacking = true;
        _moveInput = Vector2.zero;
        _animator.SetBool(AnimationKey.IsAttack, true);

        yield return _wsFireDelay;
        if (_isAttacking) Fire();

        yield return WS_ATTACK_END;
        FinishAttack();
    }

    private void Fire()
    {
        if (ArrowPool.Instance == null) return;
        Vector3 spawnPos = firePos != null ? firePos.position : transform.position;
        Quaternion rot = _sprite.flipX ? Quaternion.Euler(0, 180, 40) : Quaternion.Euler(0, 0, 40);
        ArrowPool.Instance.GetArrow(spawnPos, rot);
    }

    private void FinishAttack()
    {
        _isAttacking = false;
        _attackRoutine = null;
        _animator.SetBool(AnimationKey.IsAttack, false);
    }

    public void CancelAttack() => CancelAttackInternal(false);
    private void CancelAttackInternal(bool clearQueuedAttack)
    {
        StopAttackCoroutineOnly();
        _isAttacking = false;
        _animator.SetBool(AnimationKey.IsAttack, false);
        if (clearQueuedAttack) _attackPressed = false;
    }

    private void StopAttackCoroutineOnly()
    {
        if (_attackRoutine != null) { StopCoroutine(_attackRoutine); _attackRoutine = null; }
    }

    private void UpdateAnimation()
    {
        _animator.SetBool(AnimationKey.IsRun, IsMoving);
        if (_isAttacking) return;
        if (_moveInput.x != 0f) _sprite.flipX = _moveInput.x < 0f;
    }

    public void OnAttackButtonDown() => _attackPressed = true;
}