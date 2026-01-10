using System.Collections.Generic;
using UnityEngine;

public class SkillUIManager : MonoBehaviour
{
    [Header("UI References")]
    public SkillCard skillCardPrefab;
    public Transform container;
    private readonly string[] _keyMaps = { "Z", "X", "C", "V", "B" };
    private List<SkillCard> _activeCards = new List<SkillCard>();

    // 연결된 시스템 기억용 (재시작 시 이벤트 끊기 위해)
    private SkillSystem _connectedSkillSystem;

    // [변경] Start() 제거. 이제 매니저가 Setup()을 호출할 때까지 기다립니다.

    // [추가] 외부(GameManager)에서 플레이어를 넘겨주며 초기화 요청
    public void Setup(GameObject player)
    {
        if (player == null) return;

        // 재시작 시 기존 연결 해제 (중복 구독 방지)
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

            // 스킬 데이터 로드 및 UI 생성
            InitUI(skillSystem.myLoadout);
        }
        else
        {
            Debug.LogError("[SkillUI] 플레이어에게 SkillSystem이 없습니다.");
        }
    }

    public void InitUI(List<SkillType> loadout)
    {
        // 기존 카드 싹 지우기 (재시작 대비)
        foreach (Transform child in container) Destroy(child.gameObject);
        _activeCards.Clear();

        if (loadout == null || loadout.Count == 0) return;

        for (int i = 0; i < loadout.Count; i++)
        {
            SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(loadout[i]);

            SkillCard card = Instantiate(skillCardPrefab, container);
            string keyStr = (i < _keyMaps.Length) ? _keyMaps[i] : "";

            // 카드 초기화
            card.Setup(info, keyStr, i, OnCardClicked);

            _activeCards.Add(card);
        }
    }

    private void OnCardClicked(int slotIndex)
    {
        // 클릭 시 현재 게임 매니저에 등록된 플레이어에게 명령
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

    // 오브젝트 파괴 시 이벤트 정리
    private void OnDestroy()
    {
        if (_connectedSkillSystem != null)
        {
            _connectedSkillSystem.OnCooldownChanged -= UpdateCardCooldown;
        }
    }
}