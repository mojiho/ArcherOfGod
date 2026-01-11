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

    private SkillInfo _normalData;
    private bool _isActing;
    private Character _character;
    private Rigidbody2D _rb; 

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

        _skillStrategies = new Dictionary<SkillType, SkillStrategy>
        {
            { SkillType.Normal, new NormalSkill() }
        };
    }

    private void Start()
    {
        if (_character != null) _character.Health.OnDead += HandleDeath;
        if (GameManager.Instance != null && GameManager.Instance.skillDatabase != null)
        {
            _normalData = GameManager.Instance.skillDatabase.GetSkill(SkillType.Normal);
        }
        StartCoroutine(AICycleRoutine());
    }

    private void HandleDeath()
    {
        StopAllCoroutines();
        _rb.linearVelocity = Vector2.zero;
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

    private IEnumerator AICycleRoutine()
    {
        while (GameManager.Instance.player == null) yield return null;

        while (!_character.Health.IsDead)
        {
            yield return StartCoroutine(MoveState());

            _rb.linearVelocity = Vector2.zero;

            bool isPlayerAlive = !GameManager.Instance.player.GetComponent<Character>().Health.IsDead;
            if (!_character.Health.IsDead && isPlayerAlive && _normalData != null)
            {
                yield return StartCoroutine(PlaySkillAction(_normalData));
            }

            yield return new WaitForSeconds(1.2f);
        }
    }

    private IEnumerator MoveState()
    {
        Transform target = GetTarget();
        if (target == null) yield break;

        _character.Anim.SetBool(AnimationKey.IsRun, true);

        float moveTime = 0f;
        float duration = 1.0f; 

        while (moveTime < duration)
        {
            if (_character.Health.IsDead) break;

            float dirX = (target.position.x > transform.position.x) ? 1f : -1f;

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
        if (target != null)
        {
            _character.Sprite.flipX = target.position.x < transform.position.x;
        }

        _character.Anim.SetBool(AnimationKey.IsAttack, true);
        yield return new WaitForSeconds(0.7f);
        UseSkillLogic(info);
        yield return new WaitForSeconds(0.1f);

        _character.Anim.SetBool(AnimationKey.IsAttack, false);
        _isActing = false;
    }

    private void UseSkillLogic(SkillInfo info)
    {
        if (_skillStrategies.TryGetValue(info.type, out SkillStrategy strategy))
            strategy.Use(this, info);
    }
}