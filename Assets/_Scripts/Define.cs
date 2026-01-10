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


public enum SkillType
{
    Normal = 1001,
    MultiShot = 1002,
    DirectShot = 1003, // 직사 (Sniper)
    Dash = 1004,       // 대쉬 (이동)
    JumpShot = 1005,   // 점프샷 (백스텝 사격)
    ClusterShot = 1006  // 방사포 (360도)
}

public enum GameState
{
    Ready,      // 대기 (시작 버튼 누르기 전)
    Countdown,  // 3, 2, 1
    Playing,    // 게임 중 (시간 흐름)
    GameOver    // 결과 화면
}

public static class TrajectoryMath
{
    /// <summary>
    /// 대상 위치에 도달하기 위한 초기 속도 벡터를 계산합니다.
    /// </summary>
    /// <param name="start">발사 지점</param>
    /// <param name="target">목표 지점</param>
    /// <param name="time">체공 시간 (초)</param>
    /// <param name="gravity">중력값</param>
    public static Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float time, float gravity)
    {
        Vector3 displacement = target - start;
        // 수평 거리(x축만 고려)
        Vector3 displacementX = new Vector3(displacement.x, 0, 0);

        float vx = displacementX.magnitude / time;
        // 수직 속도 공식: (높이 차이 + 0.5 * g * t^2) / t
        float vy = (displacement.y + 0.5f * gravity * time * time) / time;

        Vector3 velocity = displacementX.normalized * vx;
        velocity.y = vy;

        return velocity;
    }
}