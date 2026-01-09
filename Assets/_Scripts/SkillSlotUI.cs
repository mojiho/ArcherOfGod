using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SkillSlotUI
{
    public Button button;          // 클릭 입력
    public Image cooldownMask;     // Filled 이미지(원형/사각 상관X)
    public Text cooldownText;      // 선택
}
