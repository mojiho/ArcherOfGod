using System.Collections;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public interface ISkillCaster
{
    bool CanAction { get; }
    IEnumerator PlaySkillAction(SkillInfo info);

    Transform GetTransform();      // 캐릭터의 위치/방향 참조용
    Transform GetFirePos();        // 화살 발사 위치 참조용
    GameObject GetGameObject();    // 화살의 주인(Owner) 설정용
    Transform GetTarget();
}