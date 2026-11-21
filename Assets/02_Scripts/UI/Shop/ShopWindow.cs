using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopWindow : MonoBehaviour, IWindowable
{
    private static readonly int[] EXCHANGE_COUNTS = { 1, 10, 100 };

    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;
    [SerializeField] private Button[] _drawButtons;
    [SerializeField] private Button[] _oddsButtons;
    [SerializeField] private Button[] _exchangeButtons;
    [SerializeField] private GameObject _modal;
    [SerializeField] private Text _modalTitle;
    [SerializeField] private Text _modalBody;
    [SerializeField] private ScrollRect _oddsScroll;
    [SerializeField] private RectTransform[] _oddsHeadings;
    [SerializeField] private RectTransform _oddsRowTemplate;
    [SerializeField] private Button _confirm;
    [SerializeField] private Button _close;
    [SerializeField] private ShopDrawPresentation _drawPresentation;

    private readonly List<(RectTransform Row, Image Icon, Text Name, Text Probability)> _oddsRows = new();
    private Action _confirmAction;
    private ShopService Shop => GameManager.Instance.Shop;

    private void Awake()
    {
        for (int i = 0; i < _drawButtons.Length; i++)
        {
            bool isPremium = i < 2;
            int count = i % 2 == 0 ? 1 : 10;
            _drawButtons[i].onClick.AddListener(() => DrawEquipment(isPremium, count));
        }

        _oddsButtons[0].onClick.AddListener(() => ShowOdds(true));
        _oddsButtons[1].onClick.AddListener(() => ShowOdds(false));
        for (int i = 0; i < _exchangeButtons.Length; i++)
        {
            int count = EXCHANGE_COUNTS[i];
            _exchangeButtons[i].onClick.AddListener(() => ConfirmExchange(count));
        }

        _confirm.onClick.AddListener(ConfirmTransaction);
        _close.onClick.AddListener(CloseModal);
        _drawPresentation.SetRetryAction(DrawEquipment);
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
        CharacterProgression characters = GameManager.Instance.Characters;
        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
        for (int i = 0; i < _drawButtons.Length; i++)
        {
            bool isPremium = i < 2;
            int count = i % 2 == 0 ? 1 : 10;
            SetOffer(_drawButtons[i], $"{count}회 뽑기", Shop.GetDrawCost(isPremium, count), isPremium ? "금자" : "은자");
        }

        for (int i = 0; i < _exchangeButtons.Length; i++)
        {
            int count = EXCHANGE_COUNTS[i];
            SetOffer(_exchangeButtons[i], $"{count}개 교환", Shop.GetExchangeCost(count), "은자");
        }
    }

    private static void SetOffer(Button button, string title, long cost, string currency)
    {
        button.interactable = cost > 0;
        string price = cost > 0 ? $"{cost:N0} {currency}" : "가격 미설정";
        button.GetComponentInChildren<Text>().text = $"{title}\n{price}";
    }

    private void DrawEquipment(bool isPremium, int count)
    {
        if (_drawPresentation.IsPlaying)
        {
            return;
        }

        if (!Shop.TryDraw(isPremium, count, out int[] results, out string message))
        {
            if (_drawPresentation.gameObject.activeSelf)
            {
                _drawPresentation.ShowError(message);
            }
            else
            {
                ShowModal(isPremium ? "고급 뽑기" : "일반 뽑기", message);
            }

            return;
        }

        CloseModal();
        RefreshShop();
        _drawPresentation.Play(isPremium, results, GameManager.Instance.Equipment.Definitions);
    }

    private void ConfirmExchange(int count)
    {
        ShowModal("금자 교환", $"{Shop.GetExchangeCost(count):N0} 은자를 금자 {count}개로 교환할까요?");
        _confirmAction = () =>
        {
            Shop.TryExchange(count, out string message);
            RefreshShop();
            ShowModal("금자 교환", message);
        };
        _confirm.gameObject.SetActive(true);
    }

    private void ConfirmTransaction()
    {
        Action action = _confirmAction;
        _confirmAction = null;
        action?.Invoke();
    }

    private void ShowModal(string title, string message)
    {
        OpenModal(title, false);
        _modalBody.text = message;
    }

    private void OpenModal(string title, bool showOdds)
    {
        _confirmAction = null;
        _confirm.gameObject.SetActive(false);
        _modalTitle.text = title;
        _modalBody.gameObject.SetActive(!showOdds);
        _oddsScroll.gameObject.SetActive(showOdds);
        _modal.SetActive(true);
    }

    private void ShowOdds(bool isPremium)
    {
        var odds = Shop.GetOdds(isPremium);
        CsvTable equipment = GameManager.Instance.Equipment.Definitions;
        int index = 0;
        float top = 0f;
        for (int group = 0; group < _oddsHeadings.Length; group++)
        {
            RectTransform heading = _oddsHeadings[group];
            heading.anchoredPosition = Vector2.down * top;
            top += heading.sizeDelta.y;
            foreach (var (id, probability) in odds)
            {
                if (equipment.GetInt(id, 2) != group)
                {
                    continue;
                }

                if (index == _oddsRows.Count)
                {
                    RectTransform newRow = Instantiate(_oddsRowTemplate, _oddsScroll.content);
                    _oddsRows.Add((newRow, newRow.Find("Icon").GetComponent<Image>(),
                        newRow.Find("Name").GetComponent<Text>(), newRow.Find("Probability").GetComponent<Text>()));
                }

                var (row, icon, name, chance) = _oddsRows[index++];
                row.anchoredPosition = Vector2.down * top;
                top += row.sizeDelta.y;
                icon.sprite = _drawPresentation.GetEquipmentIcon(id);
                name.text = equipment.GetString(id, 0);
                chance.text = $"{probability:0.###%}";
                row.gameObject.SetActive(true);
            }
        }

        for (int i = index; i < _oddsRows.Count; i++)
        {
            _oddsRows[i].Row.gameObject.SetActive(false);
        }

        _oddsScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top);
        OpenModal(isPremium ? "고급 뽑기 확률" : "일반 뽑기 확률", true);
        _oddsScroll.StopMovement();
        _oddsScroll.verticalNormalizedPosition = 1f;
    }

    private void CloseModal()
    {
        _confirmAction = null;
        _modal.SetActive(false);
    }
}
