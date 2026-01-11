using UnityEngine;

/* 
 * 스킬 전략 패턴의 기본 클래스입니다.
 * 각 스킬별로 구체적인 동작을 구현합니다.
 */
public abstract class SkillStrategy
{
    public abstract void Use(ISkillCaster caster, SkillInfo info);
}

public class NormalSkill : SkillStrategy
{ 
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
{ 
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        new MultiShotSkill().Use(caster, info);
    }
}

public class DirectShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();

        float speed = 10f;

        Vector3 launchVelocity;

        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
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

        float dirX = 0f;
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            dirX = Mathf.Sign(rb.linearVelocity.x);
        }
        else
        {
            dirX = caster.GetTransform().localScale.x > 0 ? 1f : -1f;
        }

        PlayerController player = caster as PlayerController;
        if (player != null)
        {
            player.StartDashPhysics();
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
        rb.AddForce(new Vector2(dirX * 15f, 0), ForceMode2D.Impulse);
    }
}

public class JumpShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        PlayerController player = caster as PlayerController;
        if (player != null)
        {
            player.StartJumpShotSequence(info);
        }
    }
}

public class ClusterShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();

        float flyTime = 0.7f;
        float gravity = 9.81f;
        Vector3 targetPos;

        if (target != null)
        {
            targetPos = target.position + Vector3.up * 4f;
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