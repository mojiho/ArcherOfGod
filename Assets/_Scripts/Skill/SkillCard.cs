using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;         // Image_Icon
    [SerializeField] private TextMeshProUGUI keyText; // Text_Key (Z, X...)

    [Header("Cooldown UI")]
    [SerializeField] private Image coolTimeImage;     // Image_CollTime (Filled Type)
    [SerializeField] private TextMeshProUGUI coolTimeText; // Text_CollTime (3.5 ...)
    [SerializeField] private Button interactionButton; // Button_Interaction (선택사항)

    // 쿨타임 숫자를 계산하기 위해 전체 시간을 기억해둠
    private float _maxCooldownDuration;

    private int _mySlotIndex;
    private Action<int> _onClickCallback;

    private void Awake()
    {
        // 버튼에 리스너 연결 (한 번만 하면 됨)
        if (interactionButton != null)
        {
            interactionButton.onClick.AddListener(OnClicked);
        }
    }

    public void Setup(SkillInfo info, string keyString, int index, Action<int> onClick)
    {
        _mySlotIndex = index;
        _onClickCallback = onClick;

        if (info != null)
        {
            iconImage.sprite = info.icon;
            iconImage.enabled = true;
            _maxCooldownDuration = info.cooldown;
        }
        else
        {
            iconImage.enabled = false;
            _maxCooldownDuration = 0;
        }

        if (keyText != null) keyText.text = keyString;
        if (coolTimeImage != null) coolTimeImage.fillAmount = 0;
        if (coolTimeText != null) coolTimeText.text = "";

        // 시작할 때 버튼 활성화
        if (interactionButton != null) interactionButton.interactable = true;
    }

    public void SetCooldown(float ratio)
    {
        if (coolTimeImage != null)
        {
            coolTimeImage.fillAmount = ratio;
        }

        if (coolTimeText != null)
        {
            if (ratio > 0)
            {
                float timeLeft = ratio * _maxCooldownDuration;

                coolTimeText.text = Mathf.Ceil(timeLeft).ToString();
                coolTimeText.gameObject.SetActive(true);
            }
            else
            {
                coolTimeText.text = "";
                coolTimeText.gameObject.SetActive(false);
            }
        }

        if (interactionButton != null)
        {
            interactionButton.interactable = (ratio <= 0);
        }
    }

    private void OnClicked()
    {
        _onClickCallback?.Invoke(_mySlotIndex);
    }
}