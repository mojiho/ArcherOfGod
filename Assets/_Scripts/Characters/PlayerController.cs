using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 *  플레이어 캐릭터의 움직임과 스킬 사용을 담당하는 스크립트 입니다.
 */

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, ISkillCaster
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float jumpBackDistance = 3.0f;

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
    private Rigidbody2D _rb;

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
        _rb = GetComponent<Rigidbody2D>();

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
        if (_character != null) _character.Health.OnDead += ForceGameOver;

        if (_skillSystem != null)
        {
            for (int i = 0; i < _skillSystem.myLoadout.Count; i++)
            {
                _skillSystem.TriggerCooldown(i);
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.skillDatabase != null)
            defaultSkillData = GameManager.Instance.skillDatabase.GetSkill(SkillType.Normal);
    }

    private void Update()
    {
        if (_character.Health.IsDead) return;

        ReadMoveInput();

        UpdateAnimation();

        if (!_isJumping && !_isDashing)
        {
            HandleSkillInput();
            HandleAutoFire();
        }
    }

    private void FixedUpdate()
    {
        if (_character.Health.IsDead) return;
        ApplyMovePhysics();
    }

    private void ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");

        if (h == 0 && UIManager.Instance != null)
        {
            h = UIManager.Instance.GetHorizontalInput();
        }

        if (_isAttacking && !_isAutoFiring) h = 0f;
        if (_isAutoFiring && h != 0) CancelAttackInternal();

        _moveInput = new Vector2(h, 0f);
    }

    private void ApplyMovePhysics()
    {
        if (_isDashing) return;

        if (_isJumping || _isAttacking)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            return;
        }

        if (_moveInput.x != 0)
        {
            _rb.linearVelocity = new Vector2(_moveInput.x * moveSpeed, _rb.linearVelocity.y);
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
        _isDashing = true;
        _isAttacking = true; 
        _character.Anim.SetBool(AnimationKey.IsRun, true); 

        yield return new WaitForSeconds(0.1f);

        _rb.linearVelocity = Vector2.zero;
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
        _isJumping = true;
        _isAttacking = true;
        _isAutoFiring = false;

        GameObject jumpFX = GameManager.Instance.skillDatabase.skillEffectMap["Jump"];
        if (jumpFX != null && EffectPool.Instance != null)
            EffectPool.Instance.PlayEffect(jumpFX, transform.position, Quaternion.identity);

        float gravity = Physics2D.gravity.magnitude * _rb.gravityScale;
        float jumpVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);

        float backDir = _character.Sprite.flipX ? 1f : -1f;

        float hangTime = 2f * (jumpVelocity / gravity);
        float horizontalVelocity = (jumpBackDistance / hangTime) * backDir;

        _rb.linearVelocity = new Vector2(horizontalVelocity, jumpVelocity);

        float rotateDuration = 0.5f;
        float elapsed = 0f;
        bool fired = false;
        Transform visual = _character.Sprite.transform;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            visual.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 360, t));

            if (!fired && t >= 0.5f)
            {
                fired = true;
                FireSkillArrow(info);
            }

            yield return null;
        }
        visual.localRotation = Quaternion.identity;

        while (!IsGrounded())
        {
            yield return null;
        }

        _isJumping = false;
        _isAttacking = false;
    }

    private void FireSkillArrow(SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = firePos.position;
        Transform target = GetTarget();
        float gravity = 9.81f;
        Vector3 launchVelocity;

        if (target != null)
        {

            Vector3 targetPos = target.position + Vector3.up * 0.8f;
            launchVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, targetPos, 2f, gravity);
        }
        else
        {
            Vector3 dir = transform.localScale.x > 0 ? new Vector3(1, 0.5f, 0) : new Vector3(-1, 0.5f, 0);
            launchVelocity = dir.normalized * 20f;
        }

        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null)
        {
            Arrow arrow = arrowObj.GetComponent<Arrow>();
            if (arrow != null)
            {
                arrow.SetGravity(gravity); // 중력 적용 필수
                arrow.Launch(info, info.arrowPrefab, launchVelocity, gameObject);
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

        if (!_isAttacking)
        {
            if (isRunning)
            {
                _character.Sprite.flipX = _moveInput.x < 0f;
            }
            else
            {
                Transform target = GetTarget();
                if (target != null)
                {
                    _character.Sprite.flipX = target.position.x < transform.position.x;
                }
            }
        }
    }

    private bool IsMoving() => Mathf.Abs(_moveInput.x) > 0.1f;

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

        // 1. 애니메이션 트리거 실행
        switch (info.type)
        {
            case SkillType.MultiShot:
            case SkillType.DirectShot:
                _character.Anim.SetTrigger("Skill2");
                yield return new WaitForSeconds(0.4f);
                break;
            case SkillType.ClusterShot:
                _character.Anim.SetTrigger("Skill3");
                yield return new WaitForSeconds(0.5f);
                break;
            default:
                _character.Anim.SetBool(AnimationKey.IsAttack, true);
                yield return new WaitForSeconds(0.1f);
                break;
        }

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
        _rb.simulated = false;
        _rb.linearVelocity = Vector2.zero;

        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    public Transform GetTarget()
    {
        GameObject target = GameObject.FindGameObjectWithTag("Enemy");
        return target != null ? target.transform : null;
    }

    public void ForceGameOver()
    {
        if (_character.Health.IsDead && !gameObject.activeSelf) return;

        StopAllCoroutines();

        _rb.simulated = false;
        _rb.linearVelocity = Vector2.zero;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        _character.Anim.SetBool(AnimationKey.IsRun, false);
        _character.Anim.SetBool("IsAttack", false);

        _character.Anim.Play("skill1", 0, 0f);

        StartCoroutine(DeathSequenceRoutine());
    }
}