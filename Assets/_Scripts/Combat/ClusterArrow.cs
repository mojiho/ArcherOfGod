using System.Collections;
using UnityEngine;

/* 목표 지점 상공에서 터져서 파편을 뿌리는 화살 */
public class ClusterArrow : Arrow
{
    [Header("Cluster Settings")]
    [SerializeField] private GameObject subArrowPrefab;
    [SerializeField] private int fragmentCount = 5;
    [SerializeField] private float spreadPower = 5f;

    private float _fuseTime = 1.0f;

    public void SetFuseTime(float time)
    {
        _fuseTime = time;
    }

    protected override IEnumerator FlightSequence()
    {
        rb.linearVelocity = initialVelocity;

        float timer = 0f;

        while (isLaunched && timer < _fuseTime)
        {
            Vector2 currentVelocity = rb.linearVelocity;
            currentVelocity.y -= 9.81f * Time.deltaTime;
            rb.linearVelocity = currentVelocity;

            if (currentVelocity.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
                rb.rotation = angle;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        if (!isLaunched) return;

        GameObject expFX = GameManager.Instance.skillDatabase.skillEffectMap["Explosion"];
        if (expFX != null && EffectPool.Instance != null)
            EffectPool.Instance.PlayEffect(expFX, transform.position, Quaternion.identity);

        if (ArrowPool.Instance != null && subArrowPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                // 현재 내 위치에서 생성
                GameObject subObj = ArrowPool.Instance.GetArrow(subArrowPrefab, transform.position, Quaternion.identity);
                Arrow subArrow = subObj.GetComponent<Arrow>();

                if (subArrow != null)
                {
                    Vector3 spreadDir = new Vector3(Random.Range(-0.5f, 0.5f), -1f, 0).normalized;
                    Vector3 subVelocity = spreadDir * spreadPower;

                    subArrow.SetGravity(9.81f);
                    subArrow.Launch(info, subArrowPrefab, subVelocity, owner);
                }
            }
        }

        ReturnToPool();
    }
}