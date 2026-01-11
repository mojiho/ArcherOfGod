using UnityEngine;
using TMPro;
using System.Collections;

/* 
 *  데미지 팝업 클래스 입니다.
 *  적이나 플레이어가 데미지를 입었을 때 나타나는 숫자 팝업을 관리합니다.
 */

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    // 연출 설정값
    private const float DISAPPEAR_TIMER_MAX = 1f; // 사라지는 시간
    private const float GRAVITY = 20f;            // 아래로 떨어지는 힘 (중력)
    private const float INITIAL_JUMP_VELOCITY = 2f; // 위로 솟구치는 힘

    private Vector3 _moveVector;
    private Color _textColor;

    public void Setup(int damageAmount, Vector3 direction)
    {
        textMesh.SetText(damageAmount.ToString());

        _textColor = textMesh.color;
        _textColor.a = 1f;
        textMesh.color = _textColor;

        float pushDirection = (direction.x >= 0) ? 1f : -1f;

        float xForce = pushDirection * Random.Range(1f, 3f);

        _moveVector = new Vector3(xForce, INITIAL_JUMP_VELOCITY, 0);

        transform.localScale = Vector3.one;

        StopAllCoroutines();
        StartCoroutine(BounceAnimationRoutine());
    }

    private IEnumerator BounceAnimationRoutine()
    {
        float timer = 0f;

        while (timer < DISAPPEAR_TIMER_MAX)
        {
            timer += Time.deltaTime;

            _moveVector.y -= GRAVITY * Time.deltaTime;

            transform.position += _moveVector * Time.deltaTime;

            if (timer < 0.5f)
            {
                float scaleAmount = 1f + Mathf.Sin(timer * Mathf.PI * 2) * 0.3f; // 1.3배까지 커짐
                transform.localScale = Vector3.one * scaleAmount;
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            if (timer > DISAPPEAR_TIMER_MAX * 0.5f)
            {
                float fadeSpeed = 3f;
                _textColor.a -= fadeSpeed * Time.deltaTime;
                textMesh.color = _textColor;
            }

            yield return null;
        }

        DamagePopupManager.Instance.ReturnPopup(this);
    }
}