using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * 적 캐릭터의 행동을 제어하는 클래스입니다.
 */


[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour, ISkillCaster
{
    [SerializeField] private Transform firePos;
    [SerializeField] private float moveSpeed = 3f;

    private SkillSystem _skillSystem;
    private bool _isActing;
    private Character _character;
    private Rigidbody2D _rb;
    [Header("JumpShot Settings")]
    [SerializeField] private float jumpHeight = 2.5f; 
    [SerializeField] private float jumpBackDistance = 3.0f;

    private Dictionary<SkillType, SkillStrategy> _skillStrategies;

    public bool CanAction => !_isActing && !(_character != null && _character.Health.IsDead);

    public Transform GetTransform() => transform;
    public Transform GetFirePos() => firePos != null ? firePos : transform;
    public GameObject GetGameObject() => gameObject;

    public Transform GetTarget()
    {
        if (GameManager.Instance.player != null)
            return GameManager.Instance.player.transform;
        return null;
    }

    private void Awake()
    {
        _character = GetComponent<Character>();
        _rb = GetComponent<Rigidbody2D>();
        _skillSystem = GetComponent<SkillSystem>();

        // 플레이어와 동일한 전략 사전 구성
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
        if (_character != null) _character.Health.OnDead += ForceGameOver;

        StartCoroutine(SpawnDelayAndStart());
    }

    private IEnumerator SpawnDelayAndStart()
    {
        _rb.linearVelocity = Vector2.zero;

        if (_skillSystem != null)
        {
            for (int i = 1; i < _skillSystem.myLoadout.Count; i++)
            {
                _skillSystem.TriggerCooldown(i);
            }
        }

        yield return new WaitForSeconds(0.1f);
        StartCoroutine(AICycleRoutine());
    }

    private IEnumerator AICycleRoutine()
    {
        while (GameManager.Instance.player == null) yield return null;

        while (!_character.Health.IsDead)
        {
            yield return StartCoroutine(SmartMoveState());

            yield return new WaitForSeconds(0.15f);

            if (CanAction && _skillSystem != null)
            {
                int finalSlot = -1;

                if (Random.value < 0.7f)
                {
                    if (_skillSystem.IsSkillReady(0)) finalSlot = 0;
                }

                if (finalSlot == -1)
                {
                    SkillType preferredSkill = DecideBestSkill();
                    int preferredSlot = _skillSystem.myLoadout.FindIndex(s => s == preferredSkill);

                    if (preferredSlot != -1 && _skillSystem.IsSkillReady(preferredSlot))
                    {
                        finalSlot = preferredSlot;
                    }
                    else if (_skillSystem.IsSkillReady(0))
                    {
                        finalSlot = 0;
                    }
                }

                if (finalSlot != -1)
                {
                    SkillType finalType = _skillSystem.myLoadout[finalSlot];
                    SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(finalType);
                    if (info != null)
                    {
                        yield return StartCoroutine(PlaySkillAction(info));
                        _skillSystem.TriggerCooldown(finalSlot);
                    }
                }
            }

            yield return new WaitForSeconds(Random.Range(0.4f, 0.6f));
        }
    }

    private IEnumerator MoveState()
    {
        _character.Anim.SetBool(AnimationKey.IsRun, true);

        float moveTime = 0f;
        float duration = Random.Range(0.8f, 1.5f);

        float decision = Random.value;
        Transform target = GetTarget();
        float dirX = 0f;

        if (target != null)
        {
            float toTarget = (target.position.x > transform.position.x) ? 1f : -1f;
            dirX = (decision < 0.7f) ? toTarget : -toTarget;
        }

        while (moveTime < duration)
        {
            if (_character.Health.IsDead) break;

            _character.Sprite.flipX = (dirX < 0);
            _rb.linearVelocity = new Vector2(dirX * moveSpeed, _rb.linearVelocity.y);

            moveTime += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
        _character.Anim.SetBool(AnimationKey.IsRun, false);
    }

    public IEnumerator PlaySkillAction(SkillInfo info)
    {
        if (_character.Health.IsDead) yield break;

        _isActing = true;
        _rb.linearVelocity = Vector2.zero;

        Transform target = GetTarget();
        if (target != null) _character.Sprite.flipX = target.position.x < transform.position.x;

        if (info.type == SkillType.JumpShot)
        {
            _character.Anim.SetTrigger("Skill2");
            yield return new WaitForSeconds(0.4f);
            UseSkillLogic(info);
            yield return new WaitUntil(() => Mathf.Abs(_rb.linearVelocity.y) < 0.01f);
        }
        else
        {
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
                    _character.Anim.SetBool("IsAttack", true);
                    yield return new WaitForSeconds(0.4f);
                    break;
            }

            UseSkillLogic(info);
            yield return new WaitForSeconds(0.35f);
        }

        _character.Anim.SetBool("IsAttack", false);
        _isActing = false;
    }

    private void UseSkillLogic(SkillInfo info)
    {
        if (_skillStrategies.TryGetValue(info.type, out SkillStrategy strategy))
        {
            strategy.Use(this, info);
        }
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

    private IEnumerator SmartMoveState()
    {
        Transform target = GetTarget();
        if (target == null) yield break;

        _character.Anim.SetBool(AnimationKey.IsRun, true);

        float duration = Random.Range(0.6f, 1.0f);
        float moveTime = 0f;

        while (moveTime < duration)
        {
            if (_character.Health.IsDead || _isActing) break;

            float distance = Vector2.Distance(transform.position, target.position);
            float dirX = 0f;

            if (distance < 4.5f)
            {
                dirX = (transform.position.x > target.position.x) ? 1f : -1f;
            }
            else if (distance > 7.5f)
            {
                dirX = (target.position.x > transform.position.x) ? 1f : -1f;
            }
            else
            {
                dirX = Mathf.Sin(Time.time * 5f) * 0.5f;
            }

            if (Mathf.Abs(dirX) > 0.1f)
            {
                _character.Sprite.flipX = (dirX < 0);
                _rb.linearVelocity = new Vector2(dirX * moveSpeed, _rb.linearVelocity.y);
            }
            else
            {
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            }

            moveTime += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
        _character.Anim.SetBool(AnimationKey.IsRun, false);
    }

    private SkillType DecideBestSkill()
    {
        Transform target = GetTarget();
        if (target == null) return SkillType.Normal;

        float distance = Vector2.Distance(transform.position, target.position);
        float rand = Random.value;

        if (distance > 3f && distance < 7f)
        {
            if (rand < 0.6f && _skillSystem.myLoadout.Contains(SkillType.JumpShot))
                return SkillType.JumpShot;
        }

        if (distance < 2.5f)
        {
            if (rand < 0.3f && _skillSystem.myLoadout.Contains(SkillType.Dash))
                return SkillType.Dash;
        }

        if (distance > 7f)
        {
            if (rand < 0.5f && _skillSystem.myLoadout.Contains(SkillType.ClusterShot))
                return SkillType.ClusterShot;
            if (rand < 0.8f && _skillSystem.myLoadout.Contains(SkillType.MultiShot))
                return SkillType.MultiShot;
        }

        return SkillType.Normal;
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


    public void StartJumpShotSequence(SkillInfo info)
    {
        StartCoroutine(JumpShotRoutine(info));
    }

    private IEnumerator JumpShotRoutine(SkillInfo info)
    {
        _isActing = true;

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

        yield return new WaitUntil(() => Mathf.Abs(_rb.linearVelocity.y) < 0.01f);

        _isActing = false;
    }

    private void FireSkillArrow(SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = GetFirePos().position;
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
                arrow.SetGravity(gravity);
                arrow.Launch(info, info.arrowPrefab, launchVelocity, gameObject);
            }
        }
    }
}