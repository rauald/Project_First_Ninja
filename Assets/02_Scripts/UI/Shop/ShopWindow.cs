using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopWindow : MonoBehaviour, IWindowable
{
    private static readonly int[] EXCHANGE_COUNTS = { 1, 10, 100 };

    [SerializeField] private TextMeshProUGUI _silver;
    [SerializeField] private TextMeshProUGUI _gold;
    [SerializeField] private Button[] _drawButtons;
    [SerializeField] private Button[] _oddsButtons;
    [SerializeField] private Button[] _exchangeButtons;
    [SerializeField] private GameObject _modal;
    [SerializeField] private TextMeshProUGUI _modalTitle;
    [SerializeField] private ScrollRect _oddsScroll;
    [SerializeField] private RectTransform[] _oddsHeadings;
    [SerializeField] private Button _close;
    [SerializeField] private ShopDrawPresentation _drawPresentation;
    [SerializeField] private EquipmentIconCatalog _equipmentArt;
    [Tooltip("이미지로 제목을 표시하는 버튼의 가격을 뽑기, 교환 순서로 연결합니다.")]
    [SerializeField] private TextMeshProUGUI[] _offerPrices;

    private readonly List<(RectTransform Row, Image Icon, TextMeshProUGUI Name, TextMeshProUGUI Probability)> _oddsRows = new();
    private ShopService Shop => GameManager.Instance.Shop;

    private void Awake()
    {
        for (int i = 0; i < _drawButtons.Length; i++)
        {
            bool isPremium = i < 2;
            int count = i % 2 == 0 ? 1 : 10;
            _drawButtons[i].onClick.AddListener(() => RequestDraw(isPremium, count));
        }

        _oddsButtons[0].onClick.AddListener(() => ShowOdds(true));
        _oddsButtons[1].onClick.AddListener(() => ShowOdds(false));
        for (int i = 0; i < _exchangeButtons.Length; i++)
        {
            int count = EXCHANGE_COUNTS[i];
            _exchangeButtons[i].onClick.AddListener(() => ConfirmExchange(count));
        }

        _close.onClick.AddListener(CloseModal);
        _drawPresentation.SetRetryAction(RequestDraw);
    }

    private void OnDisable()
    {
        _drawPresentation.Close();
    }

    public void ResetUI()
    {
        UIManager.Instance.PopUp.Close();
        CloseModal();
        RefreshShop();
    }

    private void RefreshShop()
    {
        LobbyWindow.BindCurrencyLabels(transform, ref _silver, ref _gold);
        CharacterProgression characters = GameManager.Instance.Characters;
        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
        for (int i = 0; i < _drawButtons.Length; i++)
        {
            bool isPremium = i < 2;
            int count = i % 2 == 0 ? 1 : 10;
            SetOffer(_drawButtons[i], _offerPrices[i], Shop.GetDrawCost(isPremium, count),
                isPremium ? "금자" : "은자");
        }

        for (int i = 0; i < _exchangeButtons.Length; i++)
        {
            int count = EXCHANGE_COUNTS[i];
            SetOffer(_exchangeButtons[i], _offerPrices[_drawButtons.Length + i],
                Shop.GetExchangeCost(count), "은자");
        }
    }

    private static void SetOffer(Button button, TextMeshProUGUI priceLabel, long cost, string currency)
    {
        button.interactable = cost > 0;
        priceLabel.text = cost > 0 ? $"{cost:N0} {currency}" : "가격 미설정";
    }

    private void RequestDraw(bool isPremium, int count)
    {
        if (_drawPresentation.IsPlaying)
        {
            return;
        }

        string currency = isPremium ? "금자" : "은자";
        string mode = isPremium ? "고급 뽑기" : "일반 뽑기";
        UIManager.Instance.PopUp.ShowChoice(
            $"{mode} {count}회\n{Shop.GetDrawCost(isPremium, count):N0} {currency}를 사용하시겠습니까?",
            () => DrawEquipment(isPremium, count));
    }

    private void DrawEquipment(bool isPremium, int count)
    {
        if (_drawPresentation.IsPlaying)
        {
            return;
        }

        if (!Shop.TryDraw(isPremium, count, out int[] results, out string message))
        {
            UIManager.Instance.PopUp.ShowNotice(message);
            return;
        }

        CloseModal();
        RefreshShop();
        _drawPresentation.Play(isPremium, results, GameManager.Instance.Equipment.Definitions);
    }

    private void ConfirmExchange(int count)
    {
        UIManager.Instance.PopUp.ShowChoice($"{Shop.GetExchangeCost(count):N0} 은자를 금자 {count}개로 교환할까요?",
            () =>
            {
                Shop.TryExchange(count, out string message);
                RefreshShop();
                UIManager.Instance.PopUp.ShowNotice(message);
            });
    }

    private void ShowOdds(bool isPremium)
    {
        if (_oddsRows.Count == 0)
        {
            foreach (RectTransform row in _oddsScroll.content)
            {
                Transform probability = row.Find("Probability");
                if (probability != null)
                {
                    _oddsRows.Add((row, row.Find("Icon").GetComponent<Image>(),
                        row.Find("Name").GetComponent<TextMeshProUGUI>(), probability.GetComponent<TextMeshProUGUI>()));
                }
            }
        }

        var odds = Shop.GetOdds(isPremium).ToDictionary(entry => entry.Id, entry => entry.Probability);
        CsvTable equipment = GameManager.Instance.Equipment.Definitions;
        int[] ids = equipment.GetIds().OrderBy(id => id).ToArray();
        int index = 0;
        float top = 0f;
        for (int group = 0; group < _oddsHeadings.Length; group++)
        {
            RectTransform heading = _oddsHeadings[group];
            heading.anchoredPosition = Vector2.down * top;
            top += heading.sizeDelta.y;
            foreach (int id in ids)
            {
                if (equipment.GetInt(id, 2) != group)
                {
                    continue;
                }

                var (row, icon, name, chance) = _oddsRows[index++];
                row.anchoredPosition = Vector2.down * top;
                top += row.sizeDelta.y;
                icon.sprite = _equipmentArt.GetIcon(id);
                name.text = equipment.GetString(id, 0);
                chance.text = $"{odds.GetValueOrDefault(id):0.###%}";
                row.gameObject.SetActive(true);
            }
        }

        for (int i = index; i < _oddsRows.Count; i++)
        {
            _oddsRows[i].Row.gameObject.SetActive(false);
        }

        _oddsScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top);
        _modalTitle.text = isPremium ? "고급 뽑기 확률" : "일반 뽑기 확률";
        _oddsScroll.gameObject.SetActive(true);
        _modal.SetActive(true);
        _oddsScroll.StopMovement();
        _oddsScroll.verticalNormalizedPosition = 1f;
    }
    public void OpenSettings()
    {
        UIManager.Instance.PopUp.Open(PopUpType.Settings);
    }

    private void CloseModal()
    {
        _modal.SetActive(false);
    }
}
