using System;

[Serializable]
public sealed class EquipmentInstanceData
{
    public int DefinitionId;
    public int Level;
    public int Rank = -1;
    public bool IsEquipped;
}
