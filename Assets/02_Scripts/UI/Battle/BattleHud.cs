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
    [SerializeField] private Sprite _namedWarningSprite;
    [SerializeField] private Sprite _bossWarningSprite;
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

    // 웨이브나 표시 초가 달라질 때만 HUD 문구를 갱신한다.
    private Text _waveLabel;
    private Text _waveTimer;
    private int _shownWave = -1;
    private int _shownWaveSeconds = -1;
    private ObjectPool<Text> _damagePool;
    private readonly List<DamageLabel> _damageLabels = new();
    private GameObject _modal;
    private Coroutine _unfoldChoices;
    private Coroutine _spawnWarning;
    private Image _warningImage;
    private RawImage _warningBorder;
    private Texture2D _warningBorderTexture;

    private struct DamageLabel
    {
        public Text View;
        public Vector3 Position;
        public float Remaining;
    }

    public bool HasModal => _modal != null;

    internal void SetWaveStatus(int wave, int finalWave, float delay)
    {
        if (_waveLabel == null)
            CreateWaveStatus();

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, delay));
        if (wave == _shownWave && seconds == _shownWaveSeconds)
            return;

        _shownWave = wave;
        _shownWaveSeconds = seconds;
        _waveLabel.text = wave == 0 ? "전투 준비" : $"웨이브 {wave} / {finalWave}";
        _waveTimer.text = wave == finalWave ? "최종 웨이브" : $"다음 웨이브까지 {seconds}초";
    }

    // 기존 전투 상태 표시 아래에 같은 글꼴로 두 줄을 배치한다.
    private void CreateWaveStatus()
    {
        var panel = new GameObject("Wave status", typeof(RectTransform), typeof(Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -108f);
        rect.sizeDelta = new Vector2(344f, 91f);
        var backdrop = panel.GetComponent<Image>();
        backdrop.color = new Color(.065f, .10f, .07f, .84f);
        backdrop.raycastTarget = false;

        _waveLabel = CreateWaveLine(rect, "Current wave", 28f, 34, new Color(1f, .91f, .64f));
        _waveTimer = CreateWaveLine(rect, "Next wave timer", 67f, 25, new Color(1f, .96f, .86f));
    }

    private Text CreateWaveLine(RectTransform parent, string name, float top, int size, Color color)
    {
        var line = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        line.rectTransform.SetParent(parent, false);
        line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(.5f, 1f);
        line.rectTransform.anchoredPosition = new Vector2(0f, -top);
        line.rectTransform.sizeDelta = new Vector2(328f, 38f);
        line.font = _level.font;
        line.fontSize = size;
        line.fontStyle = FontStyle.Bold;
        line.alignment = TextAnchor.MiddleCenter;
        line.color = color;
        line.raycastTarget = false;
        return line;
    }

    public void ShowSpawnWarning(bool boss)
    {
        Sprite sprite = boss ? _bossWarningSprite : _namedWarningSprite;
        if (sprite == null)
        {
            return;
        }
        if (_warningImage == null)
        {
            CreateSpawnWarning();
        }
        if (_spawnWarning != null)
        {
            StopCoroutine(_spawnWarning);
        }

        _warningImage.sprite = sprite;
        _spawnWarning = StartCoroutine(FlashSpawnWarning());
    }

    private void CreateSpawnWarning()
    {
        Canvas canvas = _level.GetComponentInParent<Canvas>().rootCanvas;
        var border = new GameObject("Monster warning border", typeof(RectTransform), typeof(RawImage));
        border.transform.SetParent(canvas.transform, false);
        _warningBorder = border.GetComponent<RawImage>();
        _warningBorder.raycastTarget = false;
        _warningBorder.rectTransform.anchorMax = Vector2.one;
        _warningBorder.rectTransform.anchorMin = Vector2.zero;
        _warningBorder.rectTransform.offsetMin = Vector2.zero;
        _warningBorder.rectTransform.offsetMax = Vector2.zero;

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
        _warningBorder.texture = _warningBorderTexture;

        var message = new GameObject("Monster warning image", typeof(RectTransform), typeof(Image));
        message.transform.SetParent(border.transform, false);
        _warningImage = message.GetComponent<Image>();
        _warningImage.raycastTarget = false;
        _warningImage.preserveAspect = true;
        _warningImage.rectTransform.anchorMin = new Vector2(.10f, .40f);
        _warningImage.rectTransform.anchorMax = new Vector2(.90f, .60f);
        _warningImage.rectTransform.offsetMin = Vector2.zero;
        _warningImage.rectTransform.offsetMax = Vector2.zero;
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

    // 전투 진입 시 라벨을 준비하고 재도전 때 기존 라벨을 회수한다.
    internal void ResetDamageLabels()
    {
        _damagePool ??= PoolManager.Instance.CreateScenePool(_damageLayer,
            root => Instantiate(_damageTemplate, root), 48);
        _damagePool.ReturnAll();
        _damageLabels.Clear();
    }

    public void ShowDamage(Vector3 position, float damage, bool isPlayer)
    {
        if (!_battle.Settings.IsDamageTextEnabled)
        {
            return;
        }
        Text label = _damagePool.Rent(Vector3.zero, Quaternion.identity);
        if (label == null)
            return;

        label.canvasRenderer.SetAlpha(1f);
        label.text = Mathf.Max(1, Mathf.RoundToInt(damage)).ToString();
        label.color = isPlayer ? new Color(1f, .38f, .3f) : new Color(1f, .9f, .6f);
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
                _damagePool.Return(damage.View);
                _damageLabels.RemoveAt(i);
                continue;
            }
            Vector3 world = damage.Position + Vector3.up * (.95f - damage.Remaining);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_damageLayer, _camera.WorldToScreenPoint(world),
                null, out Vector2 point);
            damage.View.rectTransform.anchoredPosition = point;
            damage.View.canvasRenderer.SetAlpha(damage.Remaining / .7f);
            _damageLabels[i] = damage;
        }
    }
}
