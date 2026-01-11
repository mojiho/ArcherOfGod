using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/* 
 *  스킬 시스템 클래스 입니다.
 *  플레이어가 장착한 스킬들의 쿨타임을 관리합니다.
 */

public class SkillSystem : MonoBehaviour
{
    [Header("Equipped Skills (Loadout)")]
    public List<SkillType> myLoadout = new List<SkillType>();

    public event Action<int, float> OnCooldownChanged;

    private HashSet<int> _activeSlotCooldowns = new HashSet<int>();

    public bool IsSkillReady(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= myLoadout.Count) return false;

        return !_activeSlotCooldowns.Contains(slotIndex);
    }

    public void TriggerCooldown(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= myLoadout.Count) return;

        SkillType type = myLoadout[slotIndex];
        SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(type);

        if (info != null && info.cooldown > 0)
        {
            StartCoroutine(CooldownRoutine(slotIndex, info.cooldown));
        }
    }

    private IEnumerator CooldownRoutine(int slotIndex, float duration)
    {
        _activeSlotCooldowns.Add(slotIndex); 

        float timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            float ratio = timer / duration;
            OnCooldownChanged?.Invoke(slotIndex, ratio);

            yield return null;
        }

        _activeSlotCooldowns.Remove(slotIndex); 
        OnCooldownChanged?.Invoke(slotIndex, 0f);
    }
}