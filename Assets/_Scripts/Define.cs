using UnityEngine;

/* 정의들을 모아둔 네임스페이스 */

// 애니메이션 해시 키를 모아둔 정적 클래스
public static class AnimationKey
{
    public static readonly int IsRun = Animator.StringToHash("IsRun");
    public static readonly int IsAttack = Animator.StringToHash("IsAttack");
    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int Die = Animator.StringToHash("Die");
}


// 스킬 종류 정의
public enum SkillType
{
    TripleShot,
    PowerShot,
    ArrowRain
}