using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ShopDrawPresentation : MonoBehaviour, IPointerDownHandler
{
    private const float SECONDS_PER_ITEM = 1f;
    private const float LAND_TIME = 0.3f;
    private const float REVEAL_TIME = 0.6f;

    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _safeArea;
    [SerializeField] private GameObject _animation;
    [SerializeField] private Image _scroll;
    [SerializeField] private Image _smoke;
    [SerializeField] private Image _revealedItem;
    [SerializeField] private Image _revealSpecialBadge;
    [Tooltip("일반 결과 연출에 쓰는 닫힌 회색 두루마리입니다.")]
    [SerializeField] private Sprite _grayClosed;
    [Tooltip("일반 결과 연출에 쓰는 펼친 회색 두루마리입니다.")]
    [SerializeField] private Sprite _grayOpen;
    [Tooltip("특별 결과 연출에 쓰는 닫힌 금색 두루마리입니다.")]
    [SerializeField] private Sprite _goldClosed;
    [Tooltip("특별 결과 연출에 쓰는 펼친 금색 두루마리입니다.")]
    [SerializeField] private Sprite _goldOpen;
    [Tooltip("뽑기 연출과 결과에 표시하는 장비 아이콘입니다. 장비 ID 1은 Element 0입니다.")]
    [SerializeField] private Sprite[] _equipmentIcons;
    [SerializeField] private GameObject _results;
    [SerializeField] private Image _singleIcon;
    [SerializeField] private Image _singleSpecialBadge;
    [SerializeField] private Image[] _gridIcons;
    [SerializeField] private Image[] _gridSpecialBadges;
    [SerializeField] private Button _return;
    [SerializeField] private Button _retry;
    [SerializeField] private Text _error;

    private int[] _equipmentIds;
    private CsvTable _definitions;
    private Action<bool, int> _retryAction;
    private bool _isPremium;
    private float _elapsed;
    private int _itemIndex;
    private bool _isSpecial;

    public bool IsPlaying { get; private set; }

    public Sprite GetEquipmentIcon(int id)
    {
        return _equipmentIcons[id - 1];
    }

    private void Awake()
    {
        _return.onClick.AddListener(Close);
        _retry.onClick.AddListener(() => _retryAction(_isPremium, _equipmentIds.Length));
    }

    public void SetRetryAction(Action<bool, int> retryAction)
    {
        _retryAction = retryAction;
    }

    public void Play(bool isPremium, int[] equipmentIds, CsvTable definitions)
    {
        _isPremium = isPremium;
        _equipmentIds = equipmentIds;
        _definitions = definitions;
        _elapsed = 0f;
        _itemIndex = -1;
        IsPlaying = true;
        gameObject.SetActive(true);
        FitContent();
        _results.SetActive(false);
        _error.text = string.Empty;
        _animation.SetActive(true);
        UpdateReveal(0, 0f);
    }

    public void Close()
    {
        IsPlaying = false;
        gameObject.SetActive(false);
    }

    public void ShowError(string message)
    {
        _error.text = message;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsPlaying && eventData.button == PointerEventData.InputButton.Left)
        {
            ShowResults();
        }
    }

    private void Update()
    {
        FitContent();
        if (!IsPlaying)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;
        int index = (int)(_elapsed / SECONDS_PER_ITEM);
        if (index >= _equipmentIds.Length)
        {
            ShowResults();
            return;
        }

        UpdateReveal(index, _elapsed - index * SECONDS_PER_ITEM);
    }

    private void FitContent()
    {
        _content.anchoredPosition = _safeArea.anchoredPosition;
        _content.sizeDelta = _safeArea.sizeDelta;
    }

    private void UpdateReveal(int index, float time)
    {
        if (index != _itemIndex)
        {
            _itemIndex = index;
            int id = _equipmentIds[index];
            _isSpecial = _definitions.GetInt(id, 2) == 1;
            _revealedItem.sprite = GetEquipmentIcon(id);
            _revealSpecialBadge.gameObject.SetActive(_isSpecial);
        }

        float landing = Mathf.Clamp01(time / LAND_TIME);
        float opening = Mathf.Clamp01((time - LAND_TIME) / (REVEAL_TIME - LAND_TIME));
        float reveal = Mathf.Clamp01((time - REVEAL_TIME) / (SECONDS_PER_ITEM - REVEAL_TIME));
        bool hasLanded = time >= LAND_TIME;
        _scroll.sprite = hasLanded ? (_isSpecial ? _goldOpen : _grayOpen) : (_isSpecial ? _goldClosed : _grayClosed);
        RectTransform scroll = _scroll.rectTransform;
        scroll.anchoredPosition = Vector2.up * (_content.rect.height * 0.65f * (1f - landing * landing));
        scroll.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-26f, 0f, landing));
        scroll.localScale = new Vector3(hasLanded ? Mathf.Lerp(0.38f, 1f, Mathf.SmoothStep(0f, 1f, opening)) : 0.78f, 1f, 1f);

        SetOpacity(_scroll, 1f - Mathf.Clamp01(reveal * 4f));
        SetOpacity(_smoke, opening * (1f - reveal));
        _smoke.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.55f, Mathf.Max(opening * 0.62f, reveal));
        float itemOpacity = Mathf.Clamp01(reveal * 3f);
        SetOpacity(_revealedItem, itemOpacity);
        SetOpacity(_revealSpecialBadge, itemOpacity);
        _revealedItem.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(reveal * 2f));
    }

    private void ShowResults()
    {
        IsPlaying = false;
        _animation.SetActive(false);
        bool isSingle = _equipmentIds.Length == 1;
        _singleIcon.transform.parent.gameObject.SetActive(isSingle);
        _gridIcons[0].transform.parent.parent.gameObject.SetActive(!isSingle);
        if (isSingle)
        {
            SetResultIcon(_singleIcon, _singleSpecialBadge, _equipmentIds[0]);
        }
        else
        {
            for (int i = 0; i < _gridIcons.Length; i++)
            {
                SetResultIcon(_gridIcons[i], _gridSpecialBadges[i], _equipmentIds[i]);
            }
        }

        _results.SetActive(true);
    }

    private void SetResultIcon(Image icon, Image specialBadge, int id)
    {
        icon.sprite = GetEquipmentIcon(id);
        bool isSpecial = _definitions.GetInt(id, 2) == 1;
        specialBadge.gameObject.SetActive(isSpecial);
        icon.transform.parent.GetComponent<Image>().color = isSpecial
            ? new Color(0.84f, 0.60f, 0.20f) : new Color(0.35f, 0.36f, 0.40f);
    }

    private static void SetOpacity(Graphic graphic, float opacity)
    {
        Color color = graphic.color;
        color.a = opacity;
        graphic.color = color;
    }
}
