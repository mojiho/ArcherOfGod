using System.Collections;
using UnityEngine;

/* 목표 지점 상공에서 터져서 파편을 뿌리는 화살 */
public class ClusterArrow : Arrow
{
    [Header("Cluster Settings")]
    [SerializeField] private GameObject subArrowPrefab; // 뿌려질 파편 화살 프리팹
    [SerializeField] private int fragmentCount = 5;     // 파편 개수
    [SerializeField] private float spreadPower = 5f;    // 파편이 퍼지는 힘

    // 폭발까지 걸리는 시간 (전략 클래스에서 설정해줌)
    private float _fuseTime = 1.0f;

    // 외부에서 퓨즈 시간을 설정하는 함수
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
            currentVelocity.y -= 9.81f * Time.deltaTime; // 중력값 하드코딩 혹은 변수 사용
            rb.linearVelocity = currentVelocity;

            // 회전
            if (currentVelocity.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
                rb.rotation = angle;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. 시간이 되면 폭발!
        Explode();
    }

    private void Explode()
    {
        if (!isLaunched) return;

        // 내 몸체는 숨김 (터졌으니까)
        // (이펙트가 있다면 여기서 Instantiate(explosionEffect, ...))

        // 4. 파편 생성
        if (ArrowPool.Instance != null && subArrowPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                // 현재 내 위치에서 생성
                GameObject subObj = ArrowPool.Instance.GetArrow(subArrowPrefab, transform.position, Quaternion.identity);
                Arrow subArrow = subObj.GetComponent<Arrow>();

                if (subArrow != null)
                {
                    // 아래로 쏟아지되, 약간씩 좌우로 퍼짐
                    // Random.Range(-1f, 1f) : 좌우 랜덤 확산
                    Vector3 spreadDir = new Vector3(Random.Range(-0.5f, 0.5f), -1f, 0).normalized;
                    Vector3 subVelocity = spreadDir * spreadPower;

                    // 파편 발사! (중력 적용)
                    subArrow.SetGravity(9.81f);
                    subArrow.Launch(info, subArrowPrefab, subVelocity, owner);
                }
            }
        }

        // 나는 할 일을 다 했으니 풀로 반환
        ReturnToPool();
    }
}