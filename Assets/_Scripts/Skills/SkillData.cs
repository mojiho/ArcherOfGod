using UnityEngine;
using System;
using NUnit.Framework;

/*
 *  스킬 데이터베이스 스크립터블 오브젝트 입니다.
 *  모든 스킬 정보를 담고 있으며, 스킬 타입으로 스킬 정보를 조회할 수 있습니다.
 *  또 스킬이펙트 프리팹들도 관리합니다.
 */

[Serializable]
public class SkillDict : SerializableDictionary<SkillType, SkillInfo> { }
[Serializable]
public class SkillEffect : SerializableDictionary<string, GameObject> { }

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Data/Skill Database")]
public class SkillData : ScriptableObject
{
    public SkillDict skillMap;
    public SkillEffect skillEffectMap;

    public SkillInfo GetSkill(SkillType type)
    {
        if (skillMap != null && skillMap.ContainsKey(type))
        {
            return skillMap[type];
        }
        return null;
    }
}