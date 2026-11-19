using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class TitleScreen : MonoBehaviour
{
    [SerializeField] private GameObject _content;
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

    private void Start()
    {
        _desktopPrompt.SetActive(false);
        _mobilePrompt.SetActive(false);
        _prompt = Application.isMobilePlatform ? _mobilePrompt : _desktopPrompt;
        _loadingBar.SetActive(false);
        _fade.alpha = 0f;
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
        _prompt.SetActive(false);
        _loadingBar.SetActive(true);
        ((RectTransform)_fill.transform).anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        _percentage.text = $"{Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
    }

    public void ShowRetry()
    {
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

    public void HideTitle()
    {
        _content.SetActive(false);
        _prompt.SetActive(false);
        _loadingBar.SetActive(false);
    }
}
