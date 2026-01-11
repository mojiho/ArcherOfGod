using System.Collections;
using UnityEngine;

/*
 *  스킬 시전자 인터페이스 입니다.
 *  스킬을 사용하는 캐릭터(플레이어, 적 등)는 이 인터페이스를 구현해야 합니다.
 */
public interface ISkillCaster
{
    bool CanAction { get; }
    IEnumerator PlaySkillAction(SkillInfo info);

    Transform GetTransform();      // 캐릭터의 위치/방향 참조용
    Transform GetFirePos();        // 화살 발사 위치 참조용
    GameObject GetGameObject();    // 화살의 주인(Owner) 설정용
    Transform GetTarget();
}