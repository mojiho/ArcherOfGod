using System;
using UnityEngine;

/*
 *  스킬 정보 클래스 입니다.
 */

[Serializable]
public class SkillInfo
{
    public int skillID;
    public string skillName;
    public Sprite icon;
    public float cooldown;
    public SkillType type;

    [TextArea] public string description;

    [Header("Projectile Settings")]
    public GameObject arrowPrefab; 
    public float projectileSpeed = 15f;
    public int damage = 10;
}