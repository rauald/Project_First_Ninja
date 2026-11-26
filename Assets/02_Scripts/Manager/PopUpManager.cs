using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public sealed class PopUpManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _popUps = new GameObject[(int)PopUpType.Max];
    [SerializeField] private GameObject _alertOverlay;
    [SerializeField] private TextMeshProUGUI _alertMessage;
    [SerializeField] private Button _alertConfirm;
    [SerializeField] private Button _alertCancel;
    [SerializeField] private Button _alertDismiss;
    [SerializeField] private GrowthMaterialRow[] _alertMaterials;
    [SerializeField] private Font _alertSourceFont;

    private GameObject _current;
    private Action _onConfirm;
    private TMP_FontAsset _supplementalFont;
    private GameObject _previousSelection;

    private void Awake()
    {
        _alertConfirm.onClick.AddListener(ConfirmAlert);
        _alertCancel.onClick.AddListener(CloseAlert);
        _alertDismiss.onClick.AddListener(CloseAlert);
        _alertConfirm.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _alertCancel };
        _alertCancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _alertConfirm };
        _alertDismiss.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    private void OnDestroy()
    {
        if (_supplementalFont == null)
        {
            return;
        }

        _alertMessage.font.fallbackFontAssetTable.Remove(_supplementalFont);
        foreach (Texture2D atlas in _supplementalFont.atlasTextures)
        {
            Destroy(atlas);
        }
        Destroy(_supplementalFont.material);
        Destroy(_supplementalFont);
    }

    public void ShowChoice(string message, Action onConfirm,
        params (Sprite Icon, long Owned, long Required, bool IsCurrency)[] materials)
    {
        _onConfirm = onConfirm;
        DisplayAlert(message, true);
        for (int index = 0; index < materials.Length; index++)
        {
            var material = materials[index];
            int slot = materials.Length == 2 ? index + 1 : index;
            _alertMaterials[slot].Show(material.Icon, material.Owned, material.Required, material.IsCurrency);
        }
    }

    public void ShowNotice(string message)
    {
        _onConfirm = null;
        DisplayAlert(message, false);
    }

    private void DisplayAlert(string message, bool hasChoice)
    {
        if (message.Contains('괄') && !_alertMessage.font.HasCharacter('괄', true))
        {
            _supplementalFont ??= TMP_FontAsset.CreateFontAsset(_alertSourceFont, 64, 6, GlyphRenderMode.SDFAA,
                128, 128, AtlasPopulationMode.Dynamic, false);
            _supplementalFont.TryAddCharacters("괄");
            if (!_alertMessage.font.fallbackFontAssetTable.Contains(_supplementalFont))
            {
                _alertMessage.font.fallbackFontAssetTable.Add(_supplementalFont);
            }
        }

        foreach (GrowthMaterialRow material in _alertMaterials)
        {
            material.Hide();
        }
        EventSystem events = EventSystem.current;
        if (!_alertOverlay.activeSelf && events != null)
        {
            _previousSelection = events.currentSelectedGameObject;
        }
        _alertMessage.text = message;
        _alertConfirm.gameObject.SetActive(hasChoice);
        _alertCancel.gameObject.SetActive(hasChoice);
        _alertDismiss.gameObject.SetActive(!hasChoice);
        _alertOverlay.SetActive(true);
        events?.SetSelectedGameObject(hasChoice ? _alertCancel.gameObject : _alertDismiss.gameObject);
    }

    private void ConfirmAlert()
    {
        Action action = _onConfirm;
        CloseAlert();
        action?.Invoke();
    }

    private void CloseAlert()
    {
        _onConfirm = null;
        if (!_alertOverlay.activeSelf)
        {
            return;
        }

        _alertOverlay.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy
            ? _previousSelection : null);
        _previousSelection = null;
    }

    public void Open(PopUpType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _popUps.Length)
        {
            return;
        }

        GameObject popUp = _popUps[index];
        if (popUp == null || popUp == _current)
        {
            return;
        }

        Close();
        _current = popUp;
        _current.SetActive(true);
    }

    public void Close()
    {
        CloseAlert();
        if (_current != null)
        {
            _current.SetActive(false);
        }

        _current = null;
    }
}
