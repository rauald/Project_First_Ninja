using UnityEngine;

public sealed class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource _bgm;
    [SerializeField] private AudioSource _sfx;
    [SerializeField] private AudioClip _titleBgm;
    [SerializeField] private AudioClip _lobbyBgm;
    [SerializeField] private AudioClip _gameBgm;

    private SaveLoadManager _save;

    public void Initialize(SaveLoadManager save)
    {
        _save = save;
        _bgm.volume = Mathf.Clamp01(save.CurData.BgmVolume);
        _sfx.volume = Mathf.Clamp01(save.CurData.SfxVolume);
    }

    public void PlaySceneBgm(SceneType scene)
    {
        AudioClip clip = scene switch
        {
            SceneType.Title => _titleBgm,
            SceneType.Lobby => _lobbyBgm,
            SceneType.Game => _gameBgm,
            _ => null
        };
        PlayBgm(clip);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (_bgm.clip == clip && _bgm.isPlaying)
        {
            return;
        }

        _bgm.Stop();
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

    public void SetMute(bool isBgmMuted, bool isSfxMuted)
    {
        _bgm.mute = isBgmMuted;
        _sfx.mute = isSfxMuted;
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
