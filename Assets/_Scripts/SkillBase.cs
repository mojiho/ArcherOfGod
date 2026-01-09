public abstract class SkillBase
{
    protected PlayerController _owner;

    protected SkillBase(PlayerController owner)
    {
        _owner = owner;
    }

    // 기본 조건은 항상 true
    public virtual bool CanUse()
    {
        return true;
    }

    // 행동은 반드시 자식이 정의
    public abstract void Use();
}
