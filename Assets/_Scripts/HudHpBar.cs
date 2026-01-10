using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
*   HP Bar UI 를 담당하는 스크립트 입니다.
*/
public class HudHpBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private bool isPlayer;
    public TextMeshProUGUI text_hp;

    private Character _targetCharacter;

    private void Start()
    {
        if (isPlayer)
        {
            _targetCharacter = GameManager.Instance.player.GetComponent<Character>();
        }
        else
        {
            GameObject enemyObj = GameObject.FindGameObjectWithTag("Enemy");
            if (enemyObj != null) _targetCharacter = enemyObj.GetComponent<Character>();
        }

        if (_targetCharacter != null)
        {
            _targetCharacter.Health.OnHpChanged += UpdateHpUI;
            UpdateHpUI(111, 111);
        }
    }

    private void UpdateHpUI(int current, int max)
    {
        if (hpSlider != null)
        {
            text_hp.text = current.ToString();
            hpSlider.value = (float)current / max;
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