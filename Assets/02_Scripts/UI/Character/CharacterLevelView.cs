using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterLevelView : MonoBehaviour
{
    [Tooltip("레벨 숫자 이미지 슬롯입니다. 왼쪽부터 표시합니다.")]
    [SerializeField] private Image[] _digits;
    [Tooltip("숫자 0부터 9까지 순서대로 연결합니다.")]
    [SerializeField] private Sprite[] _digitSprites;

    public void SetLevel(int level)
    {
        string digits = Mathf.Max(0, level).ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (int index = 0; index < _digits.Length; index++)
        {
            Image image = _digits[index];
            bool isVisible = index < digits.Length;
            if (isVisible)
            {
                image.sprite = _digitSprites[digits[index] - '0'];
            }
            _digits[index].gameObject.SetActive(isVisible);
            //image.enabled = isVisible;
        }
    }
}
