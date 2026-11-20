using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaveLoadData
{
    public float BgmVolume = 1f;
    public float SfxVolume = 1f;
    public bool IsDamageTextEnabled = true;
    public int EquippedCharacterId = 1;
    public bool HasReceivedTestSilver;
    public List<CharacterProgressData> Characters = new();
    public List<CharacterMaterialData> CharacterMaterials = new();
}
