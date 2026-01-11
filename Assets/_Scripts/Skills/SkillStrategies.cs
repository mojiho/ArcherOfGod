using UnityEngine;
using System.Collections;
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
            launchVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, targetPos, 1.5f, gravity);
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

        MonoBehaviour mono = caster.GetGameObject().GetComponent<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(ExecuteMultiShot(caster, info));
        }
    }

    private IEnumerator ExecuteMultiShot(ISkillCaster caster, SkillInfo info)
    {
        int shotCount = 3;
        float interval = 0.12f;

        for (int i = 0; i < shotCount; i++)
        {
            if (caster == null || caster.GetGameObject() == null) yield break;

            FireSingleArrow(caster, info);

            yield return new WaitForSeconds(interval);
        }
    }

    private void FireSingleArrow(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();
        float gravity = 9.81f;
        Vector3 launchVelocity;

        GameObject multiFX = GameManager.Instance.skillDatabase.skillEffectMap["multi"];
        if (multiFX != null && EffectPool.Instance != null)
            EffectPool.Instance.PlayEffect(multiFX, caster.GetFirePos().position, caster.GetTransform().rotation);

        if (target != null)
        {
            Vector3 highTargetPos = target.position + Vector3.up * 1.5f;

            launchVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, highTargetPos, 1.5f, gravity);

            float spread = Random.Range(-4f, 4f);
            launchVelocity = Quaternion.Euler(0, 0, spread) * launchVelocity;
        }
        else
        {
            Vector3 dir = caster.GetTransform().localScale.x > 0 ? new Vector3(1, 1, 0) : new Vector3(-1, 1, 0);
            launchVelocity = dir.normalized * 18f;
        }

        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null)
        {
            arrowObj.GetComponent<Arrow>().Launch(info, info.arrowPrefab, launchVelocity, caster.GetGameObject());
        }

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

        GameObject directFX = GameManager.Instance.skillDatabase.skillEffectMap["Direct"];
        if (directFX != null && EffectPool.Instance != null)
            EffectPool.Instance.PlayEffect(directFX, caster.GetTransform().position, Quaternion.identity);

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

public class DashSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        Rigidbody2D rb = caster.GetGameObject().GetComponent<Rigidbody2D>();
        if (rb == null) return;

        float dirX = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(dirX) < 0.1f)
        {
            dirX = caster.GetTransform().localScale.x > 0 ? 1f : -1f;
        }

        PlayerController player = caster as PlayerController;
        if (player != null)
        {
            player.StartDashPhysics();
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        rb.AddForce(new Vector2(dirX * 25f, 0), ForceMode2D.Impulse);
    }
}

public class JumpShotSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (caster is PlayerController player)
        {
            player.StartJumpShotSequence(info);
        }
        else if (caster is EnemyController enemy)
        {
            enemy.StartJumpShotSequence(info);
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