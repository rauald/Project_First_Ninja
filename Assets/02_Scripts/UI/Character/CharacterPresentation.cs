using System;
using UnityEngine;

[Serializable]
public sealed class CharacterPresentation
{
    public int CharacterId;
    public Sprite Portrait;
    public RuntimeAnimatorController Controller;
    public string[] SkillNames;
    public Sprite[] SkillIcons;
}
