using System;

[Serializable]
public sealed class CharacterProgressData
{
    public int CharacterId;
    public bool IsUnlocked;
    public int Level = 1;
    public int Stars;
    public int Rank;

    public bool IsAvailable
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return IsUnlocked;
#endif
        }
    }
}
