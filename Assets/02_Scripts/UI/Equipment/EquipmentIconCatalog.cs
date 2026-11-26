using UnityEngine;

[CreateAssetMenu(menuName = "UI/Equipment Icon Catalog")]
public sealed class EquipmentIconCatalog : ScriptableObject
{
    [Tooltip("장비 ID 1은 Element 0입니다.")]
    [SerializeField] private Sprite[] _icons = System.Array.Empty<Sprite>();

    public Sprite GetIcon(int definitionId)
    {
        int index = definitionId - 1;
        return (uint)index < (uint)_icons.Length ? _icons[index] : null;
    }
}
