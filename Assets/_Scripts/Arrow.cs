using System.Collections;
using UnityEngine;

/* 구형 Arrow를 신형 시스템(ArrowBase)에 맞게 개조한 코드 */
public class Arrow : ArrowBase
{
    // ArrowBase의 FlightSequence(날아가는 동작)를 구현
    protected override IEnumerator FlightSequence()
    {
        // 날아가는 동안 화살촉 회전 처리
        while (isLaunched)
        {
            // 물리 엔진이 계산한 속도 방향으로 화살 회전
            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                rb.rotation = angle;
            }
            yield return null;
        }
    }
}