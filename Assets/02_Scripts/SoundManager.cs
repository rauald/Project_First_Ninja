using UnityEngine;

public sealed class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource _bgm;
    [SerializeField] private AudioSource _sfx;
    private SaveLoadManager _save;

    public void Initialize(SaveLoadManager save)
    {
        _save = save;
        _bgm.volume = Mathf.Clamp01(save.CurData.BgmVolume);
        _sfx.volume = Mathf.Clamp01(save.CurData.SfxVolume);
    }

    public void PlayBgm(AudioClip clip)
    {
        _bgm.clip = clip;
        if (clip != null)
        {
            _bgm.Play();
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            _sfx.PlayOneShot(clip);
        }
    }

    public bool SetVolumes(float bgm, float sfx)
    {
        _bgm.volume = Mathf.Clamp01(bgm);
        _sfx.volume = Mathf.Clamp01(sfx);
        _save.CurData.BgmVolume = _bgm.volume;
        _save.CurData.SfxVolume = _sfx.volume;
        return _save.SaveData();
    }
}
