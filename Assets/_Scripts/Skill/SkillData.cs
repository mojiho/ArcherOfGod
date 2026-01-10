using UnityEngine;
using System;

[Serializable]
public class SkillDict : SerializableDictionary<SkillType, SkillInfo> { }

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Data/Skill Database")]
public class SkillData : ScriptableObject
{
    public SkillDict skillMap;

    public SkillInfo GetSkill(SkillType type)
    {
        if (skillMap.ContainsKey(type))
        {
            return skillMap[type];
        }
        return null;
    }
}