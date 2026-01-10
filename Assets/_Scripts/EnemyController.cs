using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour, ISkillCaster
{
    [SerializeField] private Transform firePos;

    private SkillInfo _normalData;
    private bool _isActing;
    private Character _character;

    // [추가] 전략 보관함
    private Dictionary<SkillType, SkillStrategy> _skillStrategies;

    public bool CanAction => !_isActing && !(_character != null && _character.Health.IsDead);

    // ============================================
    // [ISkillCaster 구현]
    // ============================================
    public Transform GetTransform() => transform;
    public Transform GetFirePos() => firePos != null ? firePos : transform;
    public GameObject GetGameObject() => gameObject;

    // [핵심] 적의 타겟은 "플레이어"입니다.
    public Transform GetTarget()
    {
        if (GameManager.Instance.player != null)
            return GameManager.Instance.player.transform;
        return null;
    }

    private void Awake()
    {
        _character = GetComponent<Character>();

        // 전략 등록 (적도 이제 똑같은 NormalSkill을 씁니다!)
        _skillStrategies = new Dictionary<SkillType, SkillStrategy>
        {
            { SkillType.Normal, new NormalSkill() }
            // 적이 강해지면 여기에 MultiShot 추가 가능
        };
    }

    private void Start()
    {
        if (_character != null)
        {
            _character.Health.OnDead += HandleDeath;
        }

        if (GameManager.Instance != null && GameManager.Instance.skillDatabase != null)
        {
            _normalData = GameManager.Instance.skillDatabase.GetSkill(SkillType.Normal);
        }

        StartCoroutine(AICycleRoutine());
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

    private IEnumerator AICycleRoutine()
    {
        // 플레이어가 생길 때까지 대기
        while (GameManager.Instance.player == null) yield return null;

        while (!_character.Health.IsDead)
        {
            yield return StartCoroutine(MoveState());

            // 플레이어가 살아있고, 데이터가 있으면 공격
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
        // 이동 로직 (추후 구현)
        yield return null;
    }

    public IEnumerator PlaySkillAction(SkillInfo info)
    {
        if (_character.Health.IsDead) yield break;

        _isActing = true;

        // 공격 전 플레이어 바라보기 (Flip)
        Transform target = GetTarget();
        if (target != null)
        {
            if (target.position.x > transform.position.x)
                _character.Sprite.flipX = false; // 오른쪽
            else
                _character.Sprite.flipX = true;  // 왼쪽
        }

        _character.Anim.SetBool(AnimationKey.IsAttack, true);
        yield return new WaitForSeconds(0.7f); // 선딜레이

        // [변경] 전략 패턴 실행! (UseSkillLogic 호출)
        UseSkillLogic(info);

        yield return new WaitForSeconds(0.1f); // 후딜레이
        _character.Anim.SetBool(AnimationKey.IsAttack, false);
        _isActing = false;
    }

    // 전략 실행 함수
    private void UseSkillLogic(SkillInfo info)
    {
        if (_skillStrategies.TryGetValue(info.type, out SkillStrategy strategy))
        {
            // "내 정보(this) 줄 테니까 대신 쏴줘"
            strategy.Use(this, info);
        }
    }

    // 기존의 Fire 함수는 이제 필요 없어서 삭제했습니다.
}