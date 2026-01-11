using System.Collections;
using UnityEngine;

/*
 *  화살의 비행을 담당하는 스크립트 입니다.
 */
    
public class Arrow : ArrowBase
{
    private float _gravity = 9.81f;

    public void SetGravity(float g)
    {
        _gravity = g;
    }

    protected override IEnumerator FlightSequence()
    {
        rb.linearVelocity = initialVelocity;

        while (isLaunched)
        {
            Vector2 currentVelocity = rb.linearVelocity;
            currentVelocity.y -= _gravity * Time.deltaTime;
            rb.linearVelocity = currentVelocity;

            if (currentVelocity.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
                rb.rotation = angle;
            }
            yield return null;
        }
    }
}