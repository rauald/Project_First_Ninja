using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageSelectWindow : MonoBehaviour, IWindowable
{
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private StageCardView _cardTemplate;
    [SerializeField] private StageEntryPopUp _entryPopUp;
    [Tooltip("스테이지 목록 카드에 표시하는 지도 미리보기입니다. 맵 타입 1은 Element 0입니다.")]
    [SerializeField] private Sprite[] _mapPreviews;

    private readonly List<StageCardView> _cards = new();

    public void ResetUI()
    {
        LobbyWindow.BindCurrencyLabels(transform, ref _silver, ref _gold);
        UIManager.Instance.PopUp.Close();
        GameManager game = GameManager.Instance;
        _silver.text = CurrencyFormatter.Format(game.Characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(game.Characters.GetAmount(0, 1));
        if (_cardTemplate == null)
        {
            _cardTemplate = _scroll.GetComponentInChildren<StageCardView>(true);
        }
        _cardTemplate.gameObject.SetActive(false);

        StageProgression stages = game.Stages;
        RectTransform highestAvailableCard = null;
        for (int i = 0; i < stages.StageIds.Count; i++)
        {
            if (i == _cards.Count)
            {
                StageCardView card = Instantiate(_cardTemplate, _scroll.content);
                _cards.Add(card);
            }

            int id = stages.StageIds[i];
            int mapIndex = stages.GetMapType(id) - 1;
            Sprite preview = mapIndex >= 0 && mapIndex < _mapPreviews.Length ? _mapPreviews[mapIndex] : null;
            StageCardView stageCard = _cards[i];
            bool canEnter = stages.CanEnter(id);
            stageCard.Bind(id, stages.GetStageName(id), preview, canEnter, stages.HasCleared(id), OpenEntryPopUp);
            stageCard.gameObject.SetActive(true);
            if (canEnter)
            {
                highestAvailableCard = (RectTransform)stageCard.transform;
            }
        }

        _scroll.StopMovement();
        if (highestAvailableCard != null)
        {
            ScrollToStage(highestAvailableCard);
        }
    }

    private void ScrollToStage(RectTransform card)
    {
        RectTransform content = _scroll.content;
        RectTransform viewport = _scroll.viewport;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float scrollableHeight = content.rect.height - viewport.rect.height;
        if (scrollableHeight <= 0f)
        {
            _scroll.verticalNormalizedPosition = 1f;
            return;
        }

        float cardCenter = content.InverseTransformPoint(card.TransformPoint(card.rect.center)).y;
        float offsetFromTop = content.rect.yMax - cardCenter - viewport.rect.height * 0.5f;
        _scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(offsetFromTop / scrollableHeight);
    }

    private void OpenEntryPopUp(int id)
    {
        StageProgression stages = GameManager.Instance.Stages;
        if (stages.CanEnter(id))
        {
            _entryPopUp.Show(id, stages.GetStageName(id));
        }
    }

    public void OpenSettingsPopUp() => UIManager.Instance.PopUp.Open(PopUpType.Settings);
}
