using System.Collections.Generic;
using UnityEngine;

/*
 *  스킬 UI 매니저 입니다.
 *  플레이어의 스킬 시스템과 연동되어 스킬 카드들을 생성 및 업데이트 합니다.
 */
public class SkillUIManager : MonoBehaviour
{
    [Header("UI References")]
    public SkillCard skillCardPrefab;
    public Transform container;
    private readonly string[] _keyMaps = { "Z", "X", "C", "V", "B" };
    private List<SkillCard> _activeCards = new List<SkillCard>();

    private SkillSystem _connectedSkillSystem;

    public void Setup(GameObject player)
    {
        if (player == null) return;

        if (_connectedSkillSystem != null)
        {
            _connectedSkillSystem.OnCooldownChanged -= UpdateCardCooldown;
            _connectedSkillSystem = null;
        }

        var skillSystem = player.GetComponent<SkillSystem>();
        if (skillSystem != null)
        {
            _connectedSkillSystem = skillSystem;
            _connectedSkillSystem.OnCooldownChanged += UpdateCardCooldown;

            InitUI(skillSystem.myLoadout);
        }
        else
        {
            Debug.LogError("[SkillUI] 플레이어에게 SkillSystem이 없습니다.");
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
        if (GameManager.Instance.player != null)
        {
            var controller = GameManager.Instance.player.GetComponent<PlayerController>();
            if (controller != null)
            {
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

    private void OnDestroy()
    {
        if (_connectedSkillSystem != null)
        {
            _connectedSkillSystem.OnCooldownChanged -= UpdateCardCooldown;
        }
    }
}