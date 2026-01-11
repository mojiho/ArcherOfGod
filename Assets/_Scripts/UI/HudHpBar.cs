using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 *  HUD 체력 바 UI 입니다.
 *  캐릭터의 체력 변화를 감지하여 UI를 갱신합니다.
 */

public class HudHpBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    public TextMeshProUGUI text_hp;


    private Character _targetCharacter;
    public void Setup(Character target)
    {
        if (_targetCharacter != null)
        {
            _targetCharacter.Health.OnHpChanged -= UpdateHpUI;
        }

        _targetCharacter = target;

        if (_targetCharacter != null)
        {
            _targetCharacter.Health.OnHpChanged += UpdateHpUI;

            UpdateHpUI(_targetCharacter.Health._currentHp, _targetCharacter.Health.maxHp);
        }
    }

    private void UpdateHpUI(int current, int max)
    {
        if (hpSlider != null)
        {
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