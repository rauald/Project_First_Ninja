using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleHud : MonoBehaviour
{
    [SerializeField] private BattleSession _battle;
    [SerializeField] private BattleInput _input;
    [SerializeField] private Camera _camera;
    [SerializeField] private Text _level;
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;
    [SerializeField] private Image _experience;
    [SerializeField] private RectTransform _healthBar;
    [SerializeField] private Image _health;
    [SerializeField] private Button _awakening;
    [SerializeField] private Text _awakeningLabel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _choicesPanel;
    [SerializeField] private GameObject _skillsPanel;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private GameObject _unavailablePanel;
    [SerializeField] private Slider _bgm;
    [SerializeField] private Slider _sfx;
    [SerializeField] private Text _bgmValue;
    [SerializeField] private Text _sfxValue;
    [SerializeField] private Text _settingsStatus;
    [SerializeField] private Text _damageSetting;
    [SerializeField] private Image[] _learnedActiveIcons;
    [SerializeField] private Image[] _learnedPassiveIcons;
    [SerializeField] private Button[] _choiceButtons;
    [SerializeField] private Text[] _choiceNames;
    [SerializeField] private Text[] _choiceLabels;
    [SerializeField] private Image[] _choiceIcons;
    [SerializeField] private Image[] _openScrolls;
    [SerializeField] private Image[] _upperScrollHandles;
    [SerializeField] private Image[] _lowerScrollHandles;
    [SerializeField] private RectTransform[] _scrollMasks;
    [SerializeField] private Sprite[] _openScrollSprites;
    [SerializeField] private Sprite[] _scrollHandleSprites;
    [SerializeField] private Button _refreshChoices;
    [SerializeField] private Text _refreshChoicesLabel;
    [SerializeField] private Sprite[] _activeIcons;
    [SerializeField] private Sprite[] _passiveIcons;
    [SerializeField] private Text[] _activeSlots;
    [SerializeField] private Text[] _passiveSlots;
    [SerializeField] private Text _resultTitle;
    [SerializeField] private Text _resultDetails;
    [SerializeField] private Button _lobby;
    [SerializeField] private Button _retry;
    [SerializeField] private Button _next;
    [SerializeField] private Button _retrySave;
    [SerializeField] private RectTransform _damageLayer;
    [SerializeField] private Text _damageTemplate;

    private readonly List<DamageLabel> _damageLabels = new();
    private GameObject _modal;
    private Coroutine _unfoldChoices;

    private sealed class DamageLabel
    {
        public Text View;
        public Vector3 Position;
        public float Remaining = .7f;
    }

    public bool HasModal => _modal != null;

    public void Refresh(int level, float experience, float health, long silver, long gold, bool hasAwakening,
        bool hasUsedAwakening)
    {
        _level.text = $"Lv.{level}";
        SetProgress(_experience.rectTransform, experience);
        SetProgress(_health.rectTransform, health);
        _silver.text = CurrencyFormatter.Format(silver);
        _gold.text = CurrencyFormatter.Format(gold);
        _awakening.gameObject.SetActive(hasAwakening);
        _awakening.interactable = !hasUsedAwakening;
        _awakeningLabel.text = hasUsedAwakening ? "각성기\n사용 완료" : "각성기";
    }

    private static void SetProgress(RectTransform fill, float amount)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
    }

    public void MoveHealthBar(Vector3 screen)
    {
        RectTransform parent = (RectTransform)_healthBar.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 point))
        {
            _healthBar.anchoredPosition = point;
        }
    }

    public void OpenSettings()
    {
        if (HasModal || _battle.Skills == null)
        {
            return;
        }
        SaveLoadData data = _battle.Settings;
        _bgm.SetValueWithoutNotify(data.BgmVolume);
        _sfx.SetValueWithoutNotify(data.SfxVolume);
        _settingsStatus.text = string.Empty;
        UpdateVolumeLabels();
        UpdateDamageSetting();
        ShowModal(_settingsPanel);
    }

    public void SetVolumes(float value)
    {
        _settingsStatus.text = SoundManager.Instance.SetVolumes(_bgm.value, _sfx.value)
            ? string.Empty : "설정을 저장하지 못했습니다.";
        UpdateVolumeLabels();
    }

    private void UpdateVolumeLabels()
    {
        _bgmValue.text = $"BGM  {Mathf.RoundToInt(_bgm.value * 100f)}";
        _sfxValue.text = $"SFX  {Mathf.RoundToInt(_sfx.value * 100f)}";
    }

    public void SetDamageText(int enabled)
    {
        SaveLoadData data = _battle.Settings;
        bool previous = data.IsDamageTextEnabled;
        data.IsDamageTextEnabled = enabled != 0;
        if (!GameManager.Instance.Save.SaveData())
        {
            data.IsDamageTextEnabled = previous;
            _settingsStatus.text = "설정을 저장하지 못했습니다.";
        }
        else
        {
            _settingsStatus.text = string.Empty;
        }
        UpdateDamageSetting();
    }

    private void UpdateDamageSetting()
    {
        _damageSetting.text = _battle.Settings.IsDamageTextEnabled ? "데미지 표시  ON" : "데미지 표시  OFF";
    }

    public void ShowChoices(IReadOnlyList<BattleSkills.Choice> choices, bool canRefresh)
    {
        if (_unfoldChoices != null)
        {
            StopCoroutine(_unfoldChoices);
        }
        BattleSkills skills = _battle.Skills;
        FillLearnedIcons(_learnedActiveIcons, skills.ActiveOrder, true);
        FillLearnedIcons(_learnedPassiveIcons, skills.PassiveOrder, false);
        _refreshChoices.interactable = false;
        _refreshChoicesLabel.text = canRefresh ? "새로고침  1/1" : "새로고침  0/1";
        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            bool isAvailable = i < choices.Count;
            _choiceButtons[i].gameObject.SetActive(isAvailable);
            if (isAvailable)
            {
                BattleSkills.Choice choice = choices[i];
                int element = choice.IsActive ? BattleSkills.GetElement(choice) : _openScrollSprites.Length - 1;
                _choiceButtons[i].interactable = false;
                _choiceIcons[i].sprite = GetSkillIcon(choice.Id, choice.IsActive);
                _choiceNames[i].text = skills.GetName(choice.Id, choice.IsActive);
                _choiceLabels[i].text = skills.Describe(choice);
                _openScrolls[i].sprite = _openScrollSprites[element];
                Sprite handle = _scrollHandleSprites[element];
                _upperScrollHandles[i].sprite = handle;
                _lowerScrollHandles[i].sprite = handle;
            }
        }
        ShowModal(_choicesPanel);
        _unfoldChoices = StartCoroutine(UnfoldChoices(choices.Count, canRefresh));
    }

    private void FillLearnedIcons(Image[] slots, IReadOnlyList<int> skills, bool isActive)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Image slot = slots[isActive ? i : slots.Length - 1 - i];
            slot.enabled = i < skills.Count;
            slot.sprite = slot.enabled ? GetSkillIcon(skills[i], isActive) : null;
        }
    }

    private IEnumerator UnfoldChoices(int count, bool canRefresh)
    {
        const float animationDuration = .45f;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            SetScrollProgress(count, Mathf.SmoothStep(0f, 1f, elapsed / animationDuration));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        SetScrollProgress(count, 1f);
        for (int i = 0; i < count; i++)
        {
            _choiceButtons[i].interactable = true;
        }
        _refreshChoices.interactable = canRefresh;
        _unfoldChoices = null;
    }

    private void SetScrollProgress(int count, float progress)
    {
        for (int i = 0; i < count; i++)
        {
            float height = ((RectTransform)_choiceButtons[i].transform).rect.height;
            _openScrolls[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            _scrollMasks[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height * progress);
            _lowerScrollHandles[i].rectTransform.anchoredPosition = Vector2.down * height * progress;
        }
    }

    public void ShowCurrentSkills()
    {
        if (HasModal || _battle.Skills == null)
        {
            return;
        }
        PopulateSkills(_activeSlots, _battle.Skills.Active, true);
        PopulateSkills(_passiveSlots, _battle.Skills.Passive, false);
        ShowModal(_skillsPanel);
    }

    private void PopulateSkills(Text[] slots, IReadOnlyDictionary<int, int> skills, bool isActive)
    {
        int index = 0;
        foreach (var skill in skills)
        {
            Text label = slots[index++];
            label.transform.parent.gameObject.SetActive(true);
            label.transform.parent.GetComponent<Image>().sprite = GetSkillIcon(skill.Key, isActive);
            label.text = $"{_battle.Skills.GetName(skill.Key, isActive)}\nLv.{skill.Value}";
        }
        for (; index < slots.Length; index++)
        {
            slots[index].transform.parent.gameObject.SetActive(false);
        }
    }

    public Sprite GetSkillIcon(int id, bool isActive)
    {
        return (isActive ? _activeIcons : _passiveIcons)[id - 1];
    }

    public void ShowResult(bool isVictory, int kills, long silver, long gold, bool isSaved, bool canAdvance)
    {
        _resultTitle.text = isVictory ? "승리" : "실패";
        _resultDetails.text = $"처치한 몬스터  {kills:N0}\n획득 은자  {silver:N0}\n획득 금자  {gold:N0}"
            + (isSaved ? string.Empty : "\n보상을 저장하지 못했습니다.");
        _lobby.interactable = isSaved;
        _retry.interactable = isSaved;
        _next.gameObject.SetActive(isSaved && canAdvance);
        _retrySave.gameObject.SetActive(!isSaved);
        ShowModal(_resultPanel);
    }

    public void ShowUnavailable()
    {
        ShowModal(_unavailablePanel);
    }

    private void ShowModal(GameObject panel)
    {
        if (_modal != panel)
        {
            CloseModal();
            _modal = panel;
            _modal.SetActive(true);
        }
        _input.ResetStick();
    }

    public void CloseModal()
    {
        if (_unfoldChoices != null)
        {
            StopCoroutine(_unfoldChoices);
            _unfoldChoices = null;
        }
        if (_modal != null)
        {
            _modal.SetActive(false);
            _modal = null;
        }
    }

    public void ShowDamage(Vector3 position, float damage, bool isPlayer)
    {
        if (!_battle.Settings.IsDamageTextEnabled)
        {
            return;
        }
        Text label = Instantiate(_damageTemplate, _damageLayer);
        label.text = Mathf.Max(1, Mathf.RoundToInt(damage)).ToString();
        label.color = isPlayer ? new Color(1f, .38f, .3f) : new Color(1f, .9f, .6f);
        label.gameObject.SetActive(true);
        _damageLabels.Add(new DamageLabel { View = label, Position = position });
    }

    private void LateUpdate()
    {
        for (int i = _damageLabels.Count - 1; i >= 0; i--)
        {
            DamageLabel damage = _damageLabels[i];
            damage.Remaining -= Time.unscaledDeltaTime;
            if (damage.Remaining <= 0f)
            {
                Destroy(damage.View.gameObject);
                _damageLabels.RemoveAt(i);
                continue;
            }
            Vector3 world = damage.Position + Vector3.up * (.95f - damage.Remaining);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_damageLayer, _camera.WorldToScreenPoint(world),
                null, out Vector2 point);
            damage.View.rectTransform.anchoredPosition = point;
            damage.View.canvasRenderer.SetAlpha(damage.Remaining / .7f);
        }
    }
}
