using UnityEngine;

public sealed class SoundManager : Singleton<SoundManager>
{
    // 장면 배경음을 반복 재생하는 소스다.
    [SerializeField] private AudioSource _bgm;
    // 짧은 효과음을 겹쳐 재생하는 소스다.
    [SerializeField] private AudioSource _sfx;
    // 타이틀 인트로와 시작 대기 화면의 반복 음악이다.
    [SerializeField] private AudioClip _titleBgm;
    // 로비와 하단 메뉴 화면의 반복 음악이다.
    [SerializeField] private AudioClip _lobbyBgm;
    // 스테이지 전투의 반복 음악이다.
    [SerializeField] private AudioClip _gameBgm;

    // 사용자 입력과 전투 상태 변화에 사용하는 효과음이다.
    // 메뉴 이동 및 출전 확인음이다.
    [SerializeField] private AudioClip _confirm;
    // 플레이어 피격음이다.
    [SerializeField] private AudioClip _hit;
    // 일반 적과 보스 처치음이다.
    [SerializeField] private AudioClip _defeatEnemy;
    // 전투 레벨이 오를 때의 알림음이다.
    [SerializeField] private AudioClip _levelUp;
    // 스테이지 클리어 알림음이다.
    [SerializeField] private AudioClip _victory;
    // 전투 실패 알림음이다.
    [SerializeField] private AudioClip _defeat;
    // 각성기 시작을 알리는 효과음이다.
    [SerializeField] private AudioClip _awakening;

    // 한 번에 많은 적이 처치될 때 효과음이 과도하게 겹치지 않도록 한다.
    private float _nextEnemyDefeatSound;

    // 음량 설정을 읽고 기록할 로컬 저장 서비스다.
    private SaveLoadManager _save;

    // 화면 전환과 플레이어 피격에 짧은 피드백을 준다.
    public void PlayConfirm() => PlaySfx(_confirm);
    public void PlayHit() => PlaySfx(_hit);

    // 전투 성장과 결과, 각성기 발동에 각각 다른 신호를 재생한다.
    public void PlayLevelUp() => PlaySfx(_levelUp);
    public void PlayResult(bool isVictory) => PlaySfx(isVictory ? _victory : _defeat);
    public void PlayAwakening() => PlaySfx(_awakening);

    // 광역 공격으로 적이 동시에 쓰러져도 짧은 간격으로 한 번만 재생한다.
    public void PlayEnemyDefeat()
    {
        if (Time.unscaledTime < _nextEnemyDefeatSound)
        {
            return;
        }

        _nextEnemyDefeatSound = Time.unscaledTime + .10f;
        PlaySfx(_defeatEnemy);
    }

    // 저장 데이터를 받은 뒤 두 오디오 채널의 초기 음량을 적용한다.
    public void Initialize(SaveLoadManager save)
    {
        _save = save;
        _bgm.volume = Mathf.Clamp01(save.CurData.BgmVolume);
        _sfx.volume = Mathf.Clamp01(save.CurData.SfxVolume);
    }

    // 현재 씬 종류에 맞는 반복 음악을 선택한다.
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

    // 다른 배경음으로 전환한다.
    public void PlayBgm(AudioClip clip)
    {
        // 로비 내부 화면을 바꿀 때 현재 곡을 처음부터 다시 재생하지 않는다.
        if (_bgm.clip == clip && _bgm.isPlaying)
        {
            return;
        }

        // 이전 곡을 중지하고 새 클립이 연결된 경우에만 재생한다.
        _bgm.Stop();
        _bgm.clip = clip;
        if (clip != null)
        {
            _bgm.Play();
        }
    }

    // 지정된 효과음을 효과음 채널에서 한 번 재생한다.
    public void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            _sfx.PlayOneShot(clip);
        }
    }

    // 배경음과 효과음 채널의 음소거 상태를 따로 적용한다.
    public void SetMute(bool isBgmMuted, bool isSfxMuted)
    {
        _bgm.mute = isBgmMuted;
        _sfx.mute = isSfxMuted;
    }

    // 유효 범위의 음량을 채널에 적용하고 저장 결과를 반환한다.
    public bool SetVolumes(float bgm, float sfx)
    {
        _bgm.volume = Mathf.Clamp01(bgm);
        _sfx.volume = Mathf.Clamp01(sfx);
        // 소스에 적용된 정규화 값을 저장해 다음 실행에서 같은 음량을 사용한다.
        _save.CurData.BgmVolume = _bgm.volume;
        _save.CurData.SfxVolume = _sfx.volume;
        return _save.SaveData();
    }
}
