using System.Collections.Generic;
using UnityEngine;

/* 스킬 데이터를 저장하는 ScriptableObject 클래스 입니다. */

[CreateAssetMenu(fileName = "NewSkillData", menuName = "ScriptableObjects/SkillData")]
public class SkillData : ScriptableObject
{
    public List<SkillInfo> skills = new List<SkillInfo>(); // 여기에 스킬들을 미리 만들어둡니다.
}
