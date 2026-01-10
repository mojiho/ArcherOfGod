using System.Collections.Generic;
using UnityEngine;

public class SkillUIManager : MonoBehaviour
{
    [Header("UI References")]
    public SkillCard skillCardPrefab;
    public Transform container;
    private readonly string[] _keyMaps = { "Z", "X", "C", "V", "B" };
    private List<SkillCard> _activeCards = new List<SkillCard>();

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
        {
            //Debug.LogError("[SkillUI] GameManager 혹은 Player가 연결되지 않았습니다.");
            return;
        }

        var skillSystem = GameManager.Instance.player.GetComponent<SkillSystem>();
        if (skillSystem != null)
        {
            skillSystem.OnCooldownChanged += UpdateCardCooldown;
            InitUI(skillSystem.myLoadout);
        }
        else
        {
            //Debug.LogError("[SkillUI] 플레이어에게 SkillSystem이 없습니다.");
        }
    }


    public void InitUI(List<SkillType> loadout)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
        _activeCards.Clear();

        if (loadout == null || loadout.Count == 0) return;

        for (int i = 0; i < loadout.Count; i++)
        {
            SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(loadout[i]);

            SkillCard card = Instantiate(skillCardPrefab, container);
            string keyStr = (i < _keyMaps.Length) ? _keyMaps[i] : "";

            card.Setup(info, keyStr, i, OnCardClicked);

            _activeCards.Add(card);
        }
    }
    private void OnCardClicked(int slotIndex)
    {
        // 플레이어를 찾아서 해당 슬롯의 스킬을 쓰라고 명령
        if (GameManager.Instance.player != null)
        {
            var controller = GameManager.Instance.player.GetComponent<PlayerController>();
            if (controller != null)
            {
                // PlayerController의 함수를 호출! (public이어야 함)
                controller.TryUseSkill(slotIndex);
            }
        }
    }
    private void UpdateCardCooldown(int index, float ratio)
    {
        if (index >= 0 && index < _activeCards.Count)
        {
            _activeCards[index].SetCooldown(ratio);
        }
    }
}