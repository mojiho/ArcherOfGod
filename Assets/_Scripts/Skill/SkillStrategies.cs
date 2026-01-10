using UnityEngine;

public abstract class SkillStrategy
{
    public abstract void Use(ISkillCaster caster, SkillInfo info);
}

// 1. 기본 사격
public class NormalSkill : SkillStrategy
{
    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();
        Vector3 launchVelocity;

        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            launchVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, targetPos, 1.0f, 9.81f);

            float rotationZ = (target.position.x < caster.GetTransform().position.x) ? -5f : 5f;
            launchVelocity = Quaternion.Euler(0, 0, rotationZ) * launchVelocity;
        }
        else
        {
            Vector3 dir = caster.GetTransform().localScale.x > 0 ? Vector3.right : Vector3.left;
            launchVelocity = dir * (info.projectileSpeed > 0 ? info.projectileSpeed : 15f);
        }

        GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
        if (arrowObj != null)
        {
            // [수정] ArrowBase -> Arrow 로 변경
            ArrowBase arrowScript = arrowObj.GetComponent<ArrowBase>();
            if (arrowScript != null)
            {
                arrowScript.Launch(info, info.arrowPrefab, launchVelocity, caster.GetGameObject());
            }
        }
    }
}

// 2. 멀티샷
public class MultiShotSkill : SkillStrategy
{
    private int _count = 3;
    private float _angle = 15f;

    public override void Use(ISkillCaster caster, SkillInfo info)
    {
        if (ArrowPool.Instance == null) return;

        Vector3 spawnPos = caster.GetFirePos().position;
        Transform target = caster.GetTarget();
        Vector3 centerVelocity;

        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            centerVelocity = TrajectoryMath.CalculateLaunchVelocity(spawnPos, targetPos, 1.0f, 9.81f);
            float rotationZ = (target.position.x < caster.GetTransform().position.x) ? -5f : 5f;
            centerVelocity = Quaternion.Euler(0, 0, rotationZ) * centerVelocity;
        }
        else
        {
            Vector3 dir = caster.GetTransform().localScale.x > 0 ? Vector3.right : Vector3.left;
            float speed = info.projectileSpeed > 0 ? info.projectileSpeed : 15f;
            centerVelocity = dir * speed;
        }

        int startAngleIndex = -(_count / 2);
        for (int i = 0; i < _count; i++)
        {
            float currentAngle = (startAngleIndex + i) * _angle;
            Vector3 finalVelocity = Quaternion.Euler(0, 0, currentAngle) * centerVelocity;

            GameObject arrowObj = ArrowPool.Instance.GetArrow(info.arrowPrefab, spawnPos, Quaternion.identity);
            if (arrowObj != null)
            {
                // [수정] ArrowBase -> Arrow 로 변경
                ArrowBase arrowScript = arrowObj.GetComponent<ArrowBase>();
                if (arrowScript != null)
                {
                    arrowScript.Launch(info, info.arrowPrefab, finalVelocity, caster.GetGameObject());
                }
            }
        }
    }
}