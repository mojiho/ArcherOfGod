using System.Collections;
using UnityEngine;

public class NormalArrow : ArrowBase
{
    protected override IEnumerator FlightSequence()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float gravity = 9.81f;

        // 발사 시점에 이미 initialVelocity(계산된 속도)가 주입된 상태입니다.
        while (elapsed < 3f)
        {
            elapsed += Time.deltaTime;

            // 포물선 공식: P = P0 + V0*t + 0.5 * g * t^2
            // initialVelocity가 Vector3(0,0,0)이면 이 값은 변하지 않습니다.
            Vector3 nextPos = startPos + (initialVelocity * elapsed) + (0.5f * Vector3.down * gravity * elapsed * elapsed);

            // 방향 처리
            Vector3 diff = nextPos - transform.position;
            if (diff.sqrMagnitude > 0.0001f)
            {
                transform.right = diff.normalized;
            }

            transform.position = nextPos;
            yield return null;
        }

        ReturnToPool();
    }
}