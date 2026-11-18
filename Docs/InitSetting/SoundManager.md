# 사운드 매니저

> - 이 파일은 초기 세팅 중 여섯번째로 읽어야 하는 파일이다.
>
> - BGM(배경음) 과 SFX(효과음)을 담당하는 스크립트이다.
>
> - 배경음은 타이틀 씬, 로비 씬, 게임 씬 3개로 나눈다.
>
> - 효과음은 AudioSource에 있는 PlayOneShot를 사용하여 여러 소리가 겹치게 나올 수 있게 한다.

## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 삭제하지 않는다.
- 해당 스크립트 수정 할 때엔 사용자 승인 없이 절대로 수정하지 않는다.
- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- SoundManager는 monobehaviour 대신 Singleton 을 상속받는다.

## 사운드 매니저 코드

```csharp
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    // BGM 오디오 소스
    [SerializeField] private AudioSource _bgm;
    // SFX 오디오 소스 풀링용
    [SerializeField] private AudioSource _sfx;

    // BGM 모음
    [SerializeField] private AudioClip[] _arrBGM;
    // SFX 모음
    [SerializeField] private AudioClip[] _arrSFX;

    // BGM 검색
    private Dictionary<BGMType, AudioClip> _dicBGM;
    // SFX 검색
    private Dictionary<SFXType, AudioClip> _dicSFX;

    // 현재 BGM
    private BGMType _curBGM;

    // BGM 볼륨
    private float _bgmVolume;
    // SFX 볼륨
    private float _sfxVolume;

    // BGM 음소거
    private bool _isMuteBGM;
    private bool _isMuteSFX;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake() 호출 필수
        
        // 초기화 로직 작성
    }

    // 초기화
    public void Init()
    {
        // 현재 로컬 저장x
        // 로컬 저장을 만들면 뮤트 불러오기
        _isMuteBGM = false;
        _isMuteSFX = false;

        // 배경음 dic에 넣기
        for (int i = 0; i < _arrBGM.Length; i++)
        {
            _dicBGM.Add((BGMType)i, _arrBGM[i]);
        }
        // 효과음 dic에 넣기
        for (int i = 0; i < _arrSFX.Length; i++)
        {
            _dicSFX.Add((SFXType)i, _arrSFX[i]);
        }
    }

    /// <summary> 배경음 플레이 </summary>
    /// <param name="bgm"> 배경음 종류 </param>
    public void PlayBGM(BGMType bgm)
    {
        // 현재 배경음이랑 같으면 스킵
        if (_curBGM == bgm)
            return;

        // 실행할 오디오
        AudioClip clip;
        
        // 오디오가 있는지 확인
        if(_dicBGM.TryGetValue(bgm, out clip))
        {
            // 있다면 멈추고
            _bgm.Stop();
            // 넣고
            _curBGM = bgm;
            _bgm.clip = clip;

            // 배경음이 뮤트인지 확인
            if (!_isMuteBGM)
            {
                // 뮤트가 아니라면 배경음 플레이
                _bgm.Play();
            }
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogError("배경음을 찾을 수 없습니다.");
        }
#endif
    }

    /// <summary> 배경음 볼륨 조절 </summary>
    /// <param name="volume"> 볼륨 </param>
    public void BGMVolume(float volume)
    {
        _bgmVolume = volume;
        _bgm.volume = _bgmVolume;
    }

    /// <summary> 배경음 음소거 (환경설정) </summary>
    public void MuteBGM()
    {
        // 음소거 ON / OFF
        _isMuteBGM = !_isMuteBGM;
        _bgm.mute = _isMuteBGM;

        if (_isMuteBGM)
            _bgm.Stop();
        else
            _bgm.Play();
    }

    /// <summary> 효과음 플레이 </summary>
    /// <param name="sfx"> 효과음 종류 </param>
    public void PlaySFX(SFXType sfx)
    {
        // SFX가 음소거 모드라면 틀지 않는다.
        if (_isMuteSFX)
            return;

        // 실행할 오디오
        AudioClip clip;

        // 오디오가 있는지 확인
        if(_dicSFX.TryGetValue(sfx, out clip))
        {
            // 있다면 넣기
            _sfx.PlayOneShot(clip);
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogError("효과음이 없습니다.");
        }
#endif
    }

    /// <summary> 효과음 볼륨 조절 </summary>
    /// <param name="volume"> 볼륨 </param>
    public void SFXVolume(float volume)
    {
        _sfxVolume = volume;
        _sfx.volume = _sfxVolume;
    }

    /// <summary> 효과음 음소거 (환경설정) </summary>
    public void MuteSFX()
    {
        _isMuteSFX = !_isMuteSFX;
        _sfx.mute = _isMuteSFX;
    }
}
```