using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public sealed class TitleScreen : MonoBehaviour, IWindowable
{
    [SerializeField] private Text _status;
    [SerializeField] private GameObject _desktopPrompt;
    [SerializeField] private GameObject _mobilePrompt;
    [SerializeField] private GameObject _loadingBar;
    [SerializeField] private Image _fill;
    [SerializeField] private Text _percentage;
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private Animator _fadeAnimator;
    [SerializeField] private Animator _introAnimator;

    private GameObject _prompt;

    public bool CanStart { get; private set; }
    public bool IsIntroComplete { get; private set; }

    public void ResetUI()
    {
        IsIntroComplete = false;
        CanStart = false;
        _status.gameObject.SetActive(false);
        _desktopPrompt.SetActive(false);
        _mobilePrompt.SetActive(false);
        _prompt = Application.isMobilePlatform ? _mobilePrompt : _desktopPrompt;
        _loadingBar.SetActive(false);
        _fade.alpha = 0f;
        _introAnimator.Play("Intro", 0, 0f);
        _introAnimator.Update(0f);
    }

    private void Update()
    {
        if (Mouse.current?.leftButton.wasPressedThisFrame != true
            && Touchscreen.current?.primaryTouch.press.wasPressedThisFrame != true)
        {
            return;
        }

        if (!IsIntroComplete)
        {
            SkipIntro();
            return;
        }

        if (CanStart)
        {
            GameManager.Instance.StartAsyncLoad(SceneType.Lobby);
        }
    }

    public void CompleteIntro()
    {
        if (IsIntroComplete)
        {
            return;
        }

        IsIntroComplete = true;
        CanStart = true;
        _prompt.SetActive(true);
    }

    public void SkipIntro()
    {
        if (IsIntroComplete)
        {
            return;
        }

        // Animator를 클립 끝으로 이동시켜 마지막 프레임과 알파를 즉시 적용한다.
        _introAnimator.Play("Intro", 0, 1f);
        _introAnimator.Update(0f);
        CompleteIntro();
    }

    public void SetProgress(float progress)
    {
        CanStart = false;
        _status.gameObject.SetActive(false);
        _prompt.SetActive(false);
        _loadingBar.SetActive(true);
        _fill.fillAmount = Mathf.Clamp01(progress);
        _percentage.text = $"{Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
    }

    public void ShowRetry()
    {
        _status.text = "Loading failed. Click or tap to retry.";
        _status.gameObject.SetActive(true);
        _loadingBar.SetActive(false);
        _prompt.SetActive(true);
        CanStart = true;
    }

    public IEnumerator FadeTo(float alpha)
    {
        _fade.transform.SetAsLastSibling();
        int state = Animator.StringToHash(alpha >= 1f ? "FadeOut" : "FadeIn");
        _fadeAnimator.Play(state, 0, 0f);
        yield return null;
        while (_fadeAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != state
            || _fadeAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }

}
