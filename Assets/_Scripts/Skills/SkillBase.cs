using UnityEngine;

/* * 
 * 스킬의 실시간 상태(쿨타임 등)를 관리하고 로직을 실행하는 클래스입니다.
 * ScriptableObject인 SkillData 시트 내의 SkillInfo를 참조합니다.
 */
public abstract class SkillBase
{
    protected PlayerController _caster;

    public SkillInfo Info { get; private set; }

    public float CurrentCooldown { get; protected set; }

    public float Cooldown => Info.cooldown;

    protected SkillBase(PlayerController caster, SkillInfo info)
    {
        _caster = caster;
        Info = info;
        CurrentCooldown = 0f;
    }

    // 쿨타임 체크
    public virtual bool CanUse() => CurrentCooldown <= 0;

    // 매 프레임 쿨타임 감소
    public void UpdateCooldown(float dt)
    {
        CurrentCooldown = Mathf.Max(0, CurrentCooldown - dt);
    }

    // 스킬 사용 시 쿨타임 시작
    public void StartCooldown()
    {
        CurrentCooldown = Cooldown;
    }

    // 실제 스킬 로직 (자식 클래스에서 구현)
    public abstract void Use();
}