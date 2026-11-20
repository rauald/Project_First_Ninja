using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSettingsPopUp : MonoBehaviour
{
    [SerializeField] private Slider _bgm;
    [SerializeField] private Slider _sfx;
    [SerializeField] private Text _bgmValue;
    [SerializeField] private Text _sfxValue;
    [SerializeField] private Text _damageValue;
    [SerializeField] private Text _status;

    private void OnEnable()
    {
        SaveLoadData data = GameManager.Instance.Save.CurData;
        _bgm.SetValueWithoutNotify(data.BgmVolume);
        _sfx.SetValueWithoutNotify(data.SfxVolume);
        _status.text = string.Empty;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        _bgmValue.text = $"BGM  {Mathf.RoundToInt(_bgm.value * 100)}";
        _sfxValue.text = $"SFX  {Mathf.RoundToInt(_sfx.value * 100)}";
        _damageValue.text = "데미지 폰트  " + (GameManager.Instance.Save.CurData.IsDamageTextEnabled ? "ON" : "OFF");
    }

    public void SetVolume(float value)
    {
        bool saved = SoundManager.Instance.SetVolumes(_bgm.value, _sfx.value);
        _status.text = saved ? string.Empty : "설정을 저장하지 못했습니다.";
        RefreshLabels();
    }

    public void SetDamageText(int enabled)
    {
        SaveLoadManager save = GameManager.Instance.Save;
        bool previous = save.CurData.IsDamageTextEnabled;
        save.CurData.IsDamageTextEnabled = enabled != 0;
        if (!save.SaveData())
        {
            save.CurData.IsDamageTextEnabled = previous;
            _status.text = "설정을 저장하지 못했습니다.";
        }
        RefreshLabels();
    }

    public void Close()
    {
        UIManager.Instance.PopUp.Close();
    }
}
