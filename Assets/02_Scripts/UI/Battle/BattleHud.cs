using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleHud : MonoBehaviour
{
    [SerializeField] private BattleSession _battle;
    [SerializeField] private BattleInput _input;
    [SerializeField] private Camera _camera;
    [SerializeField] private TextMeshProUGUI _level;
    [SerializeField] private TextMeshProUGUI _silver;
    [SerializeField] private TextMeshProUGUI _gold;
    [SerializeField] private Image _experience;
    [SerializeField] private RectTransform _healthBar;
    [SerializeField] private Image _health;
    [SerializeField] private Button _awakening;
    [SerializeField] private TextMeshProUGUI _awakeningLabel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _choicesPanel;
    [SerializeField] private GameObject _skillsPanel;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private GameObject _unavailablePanel;
    [SerializeField] private Slider _bgm;
    [SerializeField] private Slider _sfx;
    [SerializeField] private TextMeshProUGUI _bgmValue;
    [SerializeField] private TextMeshProUGUI _sfxValue;
    [SerializeField] private TextMeshProUGUI _settingsStatus;
    [SerializeField] private TextMeshProUGUI _damageSetting;
    [SerializeField] private Image[] _learnedActiveIcons;
    [SerializeField] private Image[] _learnedPassiveIcons;
    [SerializeField] private Button[] _choiceButtons;
    [SerializeField] private TextMeshProUGUI[] _choiceNames;
    [SerializeField] private TextMeshProUGUI[] _choiceLabels;
    [SerializeField] private Image[] _choiceIcons;
    [SerializeField] private Image[] _openScrolls;
    [SerializeField] private Image[] _upperScrollHandles;
    [SerializeField] private Image[] _lowerScrollHandles;
    [SerializeField] private RectTransform[] _scrollMasks;
    [Tooltip("레벨업 선택지 · 속성별로 펼친 두루마리 본체입니다. 마지막 항목은 패시브 선택지입니다.")]
    [SerializeField] private Sprite[] _openScrollSprites;
    [Tooltip("레벨업 선택지 · 속성별 두루마리 손잡이입니다. 본체와 같은 순서입니다.")]
    [SerializeField] private Sprite[] _scrollHandleSprites;
    [SerializeField] private Button _refreshChoices;
    [SerializeField] private TextMeshProUGUI _refreshChoicesLabel;
    [Tooltip("전투 선택지 및 습득 슬롯의 액티브 스킬 아이콘입니다. 스킬 ID 1은 Element 0입니다.")]
    [SerializeField] private Sprite[] _activeIcons;
    [Tooltip("전투 선택지 및 습득 슬롯의 패시브 스킬 아이콘입니다. 스킬 ID 1은 Element 0입니다.")]
    [SerializeField] private Sprite[] _passiveIcons;
    [Tooltip("네임드 몬스터 등장 시 화면 중앙에 점멸하는 경고 이미지입니다.")]
    [SerializeField] private Sprite _namedWarningSprite;
    [Tooltip("보스 등장 시 화면 중앙에 점멸하는 경고 이미지입니다.")]
    [SerializeField] private Sprite _bossWarningSprite;
    [SerializeField] private TextMeshProUGUI[] _activeSlots;
    [SerializeField] private TextMeshProUGUI[] _passiveSlots;
    [SerializeField] private TextMeshProUGUI _resultTitle;
    [SerializeField] private TextMeshProUGUI _resultDetails;
    [SerializeField] private Button _lobby;
    [SerializeField] private Button _retry;
    [SerializeField] private Button _next;
    [SerializeField] private Button _retrySave;
    [SerializeField] private RectTransform _damageLayer;
    [SerializeField] private TextMeshProUGUI _damageTemplate;

    [SerializeField] private TextMeshProUGUI _waveLabel;
    [SerializeField] private TextMeshProUGUI _waveTimer;
    [SerializeField] private Image _warningImage;
    [SerializeField] private RawImage _warningBorder;

    // 웨이브나 표시 초가 달라질 때만 HUD 문구를 갱신한다.
    private int _shownWave = -1;
    private int _shownWaveSeconds = -1;
    private const int INITIAL_DAMAGE_LABEL_COUNT = 48;
    private readonly Stack<TextMeshProUGUI> _availableDamageLabels = new(INITIAL_DAMAGE_LABEL_COUNT);
    private int _createdDamageLabels;
    private readonly List<DamageLabel> _damageLabels = new();
    private GameObject _modal;
    private Coroutine _unfoldChoices;
    private Coroutine _spawnWarning;
    private Texture2D _warningBorderTexture;

    private struct DamageLabel
    {
        public TextMeshProUGUI View;
        public Vector3 Position;
        public float Remaining;
    }

    public bool HasModal => _modal != null;

    internal void SetWaveStatus(int wave, int finalWave, float delay)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, delay));
        if (wave == _shownWave && seconds == _shownWaveSeconds)
            return;

        _shownWave = wave;
        _shownWaveSeconds = seconds;
        _waveLabel.text = wave == 0 ? "전투 준비" : $"웨이브 {wave} / {finalWave}";
        _waveTimer.text = wave == finalWave ? "최종 웨이브" : $"다음 웨이브까지 {seconds}초";
    }

    public void ShowSpawnWarning(bool boss)
    {
        Sprite sprite = boss ? _bossWarningSprite : _namedWarningSprite;
        if (sprite == null)
        {
            return;
        }
        if (_warningBorder.texture == null)
        {
            _warningBorder.texture = CreateWarningBorderTexture();
        }
        if (_spawnWarning != null)
        {
            StopCoroutine(_spawnWarning);
        }

        _warningImage.sprite = sprite;
        _spawnWarning = StartCoroutine(FlashSpawnWarning());
    }

    private Texture2D CreateWarningBorderTexture()
    {
        const int size = 96;
        _warningBorderTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _warningBorderTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, edge / 9f);
                pixels[y * size + x] = new Color32(255, 20, 34, (byte)Mathf.RoundToInt(255f * alpha));
            }
        }
        _warningBorderTexture.SetPixels32(pixels);
        _warningBorderTexture.Apply(false, true);
        return _warningBorderTexture;
    }

    private IEnumerator FlashSpawnWarning()
    {
        const float duration = 2f;
        const int flashes = 5;
        _warningBorder.transform.SetAsLastSibling();
        _warningBorder.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float pulse = .5f + .5f * Mathf.Cos(elapsed / duration * flashes * Mathf.PI * 2f);
            _warningBorder.color = new Color(1f, 1f, 1f, pulse * .83f);
            _warningImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(.2f, 1f, pulse));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        _warningBorder.gameObject.SetActive(false);
        _spawnWarning = null;
    }

    private void OnDestroy()
    {
        if (_warningBorderTexture != null)
        {
            Destroy(_warningBorderTexture);
        }
    }

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

    // 전투 설정 팝업에서만 중도 로비 복귀를 요청한다.
    public void ReturnToLobbyFromSettings()
    {
        if (_modal == _settingsPanel && !_battle.ReturnToLobbyDuringBattle())
            _settingsStatus.text = "획득한 재화를 저장하지 못했습니다. 다시 시도해 주세요.";
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

    public void ShowChoices(IReadOnlyList<BattleSkills.Choice> choices, bool hasRefreshTalent, bool hasRefreshed)
    {
        if (_unfoldChoices != null)
        {
            StopCoroutine(_unfoldChoices);
        }
        BattleSkills skills = _battle.Skills;
        FillLearnedIcons(_learnedActiveIcons, skills.ActiveOrder, true);
        FillLearnedIcons(_learnedPassiveIcons, skills.PassiveOrder, false);
        _refreshChoices.interactable = false;
        // 특성 미습득과 사용 완료 상태를 구분해서 표시한다.
        bool canRefresh = hasRefreshTalent && !hasRefreshed;
        _refreshChoicesLabel.text = !hasRefreshTalent ? "특성 미습득"
            : canRefresh ? "새로고침  1/1" : "새로고침  0/1";
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

    private void PopulateSkills(TextMeshProUGUI[] slots, IReadOnlyDictionary<int, int> skills, bool isActive)
    {
        int index = 0;
        foreach (var skill in skills)
        {
            TextMeshProUGUI label = slots[index++];
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

    private void Awake()
    {
        // 로딩 화면 아래에서 전투 씬이 활성화될 때 최초 라벨을 준비한다.
        GameManager game = GameManager.Instance;
        if (game == null || game.ActiveStageId == 0)
        {
            return;
        }

        for (int index = 0; index < INITIAL_DAMAGE_LABEL_COUNT; index++)
        {
            AddDamageLabelToPool();
        }
    }

    // 재도전 때 사용 중인 라벨을 회수한다.
    internal void ResetDamageLabels()
    {
        foreach (DamageLabel damage in _damageLabels)
        {
            ReturnDamageLabel(damage.View);
        }
        _damageLabels.Clear();
    }

    public void ShowDamage(Vector3 position, float damage, bool isPlayer)
    {
        if (!_battle.Settings.IsDamageTextEnabled)
        {
            return;
        }
        if (_availableDamageLabels.Count == 0)
        {
            AddDamageLabelToPool();
        }

        TextMeshProUGUI label = _availableDamageLabels.Pop();
        label.canvasRenderer.SetAlpha(1f);
        label.text = Mathf.Max(1, Mathf.RoundToInt(damage)).ToString();
        label.color = isPlayer ? new Color(1f, .38f, .3f) : new Color(1f, .9f, .6f);
        PositionDamageLabel(label, position + Vector3.up * .25f);
        label.gameObject.SetActive(true);
        _damageLabels.Add(new DamageLabel { View = label, Position = position, Remaining = .7f });
    }

    private void LateUpdate()
    {
        for (int i = _damageLabels.Count - 1; i >= 0; i--)
        {
            DamageLabel damage = _damageLabels[i];
            damage.Remaining -= Time.unscaledDeltaTime;
            if (damage.Remaining <= 0f)
            {
                ReturnDamageLabel(damage.View);
                _damageLabels.RemoveAt(i);
                continue;
            }
            Vector3 world = damage.Position + Vector3.up * (.95f - damage.Remaining);
            PositionDamageLabel(damage.View, world);
            damage.View.canvasRenderer.SetAlpha(damage.Remaining / .7f);
            _damageLabels[i] = damage;
        }
    }

    private void PositionDamageLabel(TextMeshProUGUI label, Vector3 world)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_damageLayer, _camera.WorldToScreenPoint(world),
            null, out Vector2 point);
        label.rectTransform.anchoredPosition = point;
    }

    private void ReturnDamageLabel(TextMeshProUGUI label)
    {
        label.gameObject.SetActive(false);
        _availableDamageLabels.Push(label);
    }

    private void AddDamageLabelToPool()
    {
        TextMeshProUGUI label = Instantiate(_damageTemplate, _damageLayer);
        label.name = $"Damage_Label_{++_createdDamageLabels:00}";
        label.gameObject.SetActive(false);
        _availableDamageLabels.Push(label);
    }
}
