using System;
using UnityEngine;

[Serializable]
public sealed class CharacterPresentation
{
    public int CharacterId;
    [Tooltip("캐릭터 목록 및 상세 화면에 사용되는 초상화입니다.")]
    public Sprite Portrait;
    [Tooltip("상세·장비 화면의 캐릭터 애니메이션입니다. 프레임은 컨트롤러의 Animation Clip에서 교체합니다.")]
    public RuntimeAnimatorController Controller;
    public string[] SkillNames;
    [Tooltip("캐릭터 상세와 스킬 설명 팝업의 아이콘입니다. Skill Names와 동일한 순서입니다.")]
    public Sprite[] SkillIcons;
}
