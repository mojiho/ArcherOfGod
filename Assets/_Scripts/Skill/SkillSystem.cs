using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SkillSystem : MonoBehaviour
{
    [Header("Equipped Skills (Loadout)")]
    public List<SkillType> myLoadout = new List<SkillType>();
    public event Action<int, float> OnCooldownChanged;
    public event Action<List<SkillType>> OnInitialize;

    private HashSet<SkillType> _activeCooldowns = new HashSet<SkillType>();

    public bool IsSkillReady(SkillType type)
    {
        if (!myLoadout.Contains(type)) return false;
        return !_activeCooldowns.Contains(type);
    }

    public void TriggerCooldown(SkillType type, int slotIndex)
    {
        SkillInfo info = GameManager.Instance.skillDatabase.GetSkill(type);

        if (info != null && info.cooldown > 0)
        {
            StartCoroutine(CooldownRoutine(type, slotIndex, info.cooldown));
        }
    }

    private IEnumerator CooldownRoutine(SkillType type, int index, float duration)
    {
        _activeCooldowns.Add(type);

        float timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            float ratio = timer / duration;
            OnCooldownChanged?.Invoke(index, ratio);

            yield return null;
        }
        _activeCooldowns.Remove(type);
        OnCooldownChanged?.Invoke(index, 0f);
    }
}