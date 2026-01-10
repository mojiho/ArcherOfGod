using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
* GameManager가 캐릭터를 생성한 직후에 연결해 줍니다.
*/
public class HudHpBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    public TextMeshProUGUI text_hp;

    // [변경] isPlayer 불필요 (매니저가 알아서 넣어줌)

    private Character _targetCharacter;

    // Start() 제거! (매니저가 Setup 호출할 때까지 대기)

    // [추가] 외부에서 캐릭터를 연결해 주는 함수
    public void Setup(Character target)
    {
        // 재시작 시 기존 연결 끊기 (안전장치)
        if (_targetCharacter != null)
        {
            _targetCharacter.Health.OnHpChanged -= UpdateHpUI;
        }

        _targetCharacter = target;

        if (_targetCharacter != null)
        {
            // 이벤트 구독
            _targetCharacter.Health.OnHpChanged += UpdateHpUI;

            // 초기 UI 갱신 (현재 체력으로)
            UpdateHpUI(_targetCharacter.Health._currentHp, _targetCharacter.Health.maxHp);
        }
    }

    private void UpdateHpUI(int current, int max)
    {
        if (hpSlider != null)
        {
            // 0으로 나누기 방지
            float maxFloat = max > 0 ? (float)max : 1f;
            hpSlider.value = current / maxFloat;
        }

        if (text_hp != null)
        {
            text_hp.text = $"{current}";
        }
    }

    private void OnDestroy()
    {
        if (_targetCharacter != null)
        {
            _targetCharacter.Health.OnHpChanged -= UpdateHpUI;
        }
    }
}