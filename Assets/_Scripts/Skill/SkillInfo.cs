using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillInfo
{
    public string skillName;
    public Sprite icon;
    public float cooldown;
    public SkillType type;
}