using UnityEngine;

public abstract class SkillStrategy
{
    public abstract void Use(ISkillCaster caster, SkillInfo info);
}

// 1. 기본 사격, 2. 멀티샷 (기존 유지 - 생략 가능하지만 전체 코드를 위해 포함)
public class NormalSkill : SkillStrategy
{ /* ...기존 코드 유지... */
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;
        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();
        float gravity = 9.81f;
        Vector3 launchVelocity;
        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            launchVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, targetPos, 1.0f, gravity);
            float rotationZ = (target.position.x < caster.GetTransform().position.x) ? -5f : 5f;
            launchVelocity = Quaternion.Euler(0, 0, rotationZ) * launchVelocity;
        }
        else
        {
            Vector3 dir = caster.GetTransform().localScale.x > 0 ? Vector3.right : Vector3.left;
            launchVelocity = dir * (info.projectileSpeed > 0 ? info.projectileSpeed : 15f);
        }
        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null) arrowObj.GetComponent<Arrow>().Launch(info, info.arrowPrefab, launchVelocity, caster.GetGameObject());
    }
}

public class MultiShotSkill : SkillStrategy
{ /* ...기존 코드 유지... */
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        // (기존 코드와 동일하여 생략, 필요시 이전 코드 참고)
        new NormalSkill().Use(caster, info); // 임시
    }
}

// ========================================================================
// [수정] 다이렉트 샷 (DirectShot) - 완전 직선 레이저
// ========================================================================
public class DirectShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();

        // 속도 대폭 증가 (레이저 느낌)
        float speed = 30f;

        Vector3 launchVelocity;

        if (target != null)
        {
            // [수정] TrajectoryMath 안 씀. 타겟 방향으로 그냥 꽂음
            Vector3 targetPos = target.position + Vector3.up * 0.5f; // 몸통 조준
            Vector3 dir = (targetPos - spawnPos).normalized;
            launchVelocity = dir * speed;
        }
        else
        {
            Vector3 dir = caster.GetTransform().localScale.x > 0 ? Vector3.right : Vector3.left;
            launchVelocity = dir * speed;
        }

        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null)
        {
            Arrow arrowScript = arrowObj.GetComponent<Arrow>();
            if (arrowScript != null)
            {
                // [핵심] 중력 0 -> 직선 비행
                arrowScript.SetGravity(0f);
                arrowScript.Launch(info, info.arrowPrefab, launchVelocity, caster.GetGameObject());
            }
        }
    }
}

// ========================================================================
// [수정] 대쉬 (Dash) - 이동 중인 방향으로 돌진 + 이동 안됨 수정
// ========================================================================
public class DashSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        Rigidbody2D rb = caster.GetGameObject().GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // 1. 현재 이동 입력이나 속도가 있는지 확인
        float dirX = 0f;

        // 속도가 일정 이상이면 그 방향으로 대쉬 (이동 중 대쉬)
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            dirX = Mathf.Sign(rb.linearVelocity.x);
        }
        else
        {
            // 멈춰있으면 바라보는 방향으로 대쉬
            dirX = caster.GetTransform().localScale.x > 0 ? 1f : -1f;
            // 만약 SpriteFlip을 쓴다면 아래 사용
            // dirX = caster.GetGameObject().GetComponent<SpriteRenderer>().flipX ? -1f : 1f;
        }

        // 2. PlayerController에게 "나 대쉬중이야"라고 알려줌 (물리 간섭 방지)
        PlayerController player = caster as PlayerController;
        if (player != null)
        {
            player.StartDashPhysics();
        }

        // 3. 강력한 힘 가하기 (기존 속도 무시)
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Y축은 유지
        rb.AddForce(new Vector2(dirX * 25f, 0), ForceMode2D.Impulse); // 파워 25 (강하게)
    }
}

// ========================================================================
// [수정] 점프샷 (JumpShot) - 높게 점프 + 360도 회전 + 사격 + 무적/봉인
// ========================================================================
public class JumpShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        // PlayerController에게 복잡한 동작(코루틴)을 위임합니다.
        PlayerController player = caster as PlayerController;
        if (player != null)
        {
            player.StartJumpShotSequence(info);
        }
    }
}

// ========================================================================
// [수정] 클러스터 샷 (Cluster) - 45도 확산
// ========================================================================
public class ClusterShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();

        float flyTime = 0.8f; // 좀 더 빨리 터지게
        float gravity = 9.81f;
        Vector3 targetPos;

        if (target != null)
        {
            // 적 머리 위 3.5m (대쉬로 피할 공간 확보)
            targetPos = target.position + Vector3.up * 3.5f;
        }
        else
        {
            Vector3 dir = caster.GetTransform().localScale.x > 0 ? Vector3.right : Vector3.left;
            targetPos = spawnPos + (dir * 6f) + (Vector3.up * 3.5f);
        }

        Vector3 launchVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, targetPos, flyTime, gravity);

        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null)
        {
            ClusterArrow clusterScript = arrowObj.GetComponent<ClusterArrow>();
            if (clusterScript != null)
            {
                clusterScript.SetFuseTime(flyTime);
                clusterScript.Launch(info, info.arrowPrefab, launchVelocity, caster.GetGameObject());
            }
        }
    }
}