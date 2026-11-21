using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class TalentWindow : MonoBehaviour, IWindowable
{
    private const int ROOT_ID = 1;

    [SerializeField] private TalentPanViewport _viewport;
    [SerializeField] private TalentNodeView[] _nodes;
    [SerializeField] private Color _unlearnedColor = new(0.72f, 0.72f, 0.72f, 1f);
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;
    [SerializeField] private GameObject _details;
    [SerializeField] private Image _detailIcon;
    [SerializeField] private Text _detailName;
    [SerializeField] private Text _detailEffect;
    [SerializeField] private Text _detailRequirements;
    [SerializeField] private Text _detailCost;
    [SerializeField] private Text _detailStatus;
    [SerializeField] private Button _learn;
    [SerializeField] private Button _close;

    private Dictionary<int, TalentNodeView> _nodesById;
    private TalentService.Talent _selected;
    private TalentService Talents => GameManager.Instance.Talents;

    private void Awake()
    {
        _nodesById = _nodes.ToDictionary(node => node.Id);
        foreach (TalentNodeView node in _nodes)
        {
            node.Button.onClick.AddListener(() => OpenDetails(node));
        }

        _learn.onClick.AddListener(LearnSelected);
        _close.onClick.AddListener(CloseDetails);
    }

    public void ResetUI()
    {
        UIManager.Instance.PopUp.Close();
        CloseDetails();
        foreach (TalentNodeView node in _nodes)
        {
            IReadOnlyList<int> prerequisites = Talents.GetTalent(node.Id).Prerequisites;
            for (int index = 0; index < prerequisites.Count; index++)
            {
                node.PlaceLink(index, _nodesById[prerequisites[index]].Position);
            }
        }

        _viewport.ResetPosition(_nodesById[ROOT_ID].Position, _nodes);
        RefreshTree();
    }

    private void RefreshTree()
    {
        _silver.text = CurrencyFormatter.Format(Talents.GetBalance(0));
        _gold.text = CurrencyFormatter.Format(Talents.GetBalance(1));
        foreach (TalentNodeView node in _nodes)
        {
            node.SetLearned(Talents.IsLearned(node.Id), _unlearnedColor);
        }
    }

    private void OpenDetails(TalentNodeView node)
    {
        _selected = Talents.GetTalent(node.Id);
        _detailIcon.sprite = node.Icon;
        _detailName.text = _selected.Name;
        _detailEffect.text = _selected.Id == ROOT_ID ? "스킬 새로고침" : $"{_selected.Effect} +{_selected.Value / 10f:0.#}%";
        _detailCost.text = $"비용  {(_selected.Currency == 0 ? "은자" : "금자")} {_selected.Cost:N0}";
        _details.SetActive(true);
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        _detailIcon.color = Talents.IsLearned(_selected.Id) ? Color.white : _unlearnedColor;
        _detailRequirements.text = _selected.Prerequisites.Count == 0
            ? "선행 특성 없음"
            : "선행 특성\n" + string.Join("\n", _selected.Prerequisites.Select(id =>
                $"{Talents.GetTalent(id).Name}  {(Talents.IsLearned(id) ? "습득" : "미습득")}"));
        string reason = Talents.GetBlockReason(_selected);
        _learn.interactable = reason.Length == 0;
        _detailStatus.text = _learn.interactable ? "습득 가능" : reason;
    }

    private void LearnSelected()
    {
        if (Talents.TryLearn(_selected.Id, out string message))
        {
            RefreshTree();
            RefreshDetails();
            return;
        }

        RefreshDetails();
        _detailStatus.text = message;
    }

    private void CloseDetails()
    {
        _details.SetActive(false);
        _selected = null;
    }

    public void OpenSettingsPopUp() => UIManager.Instance.PopUp.Open(PopUpType.Settings);
    public void OpenShopWindow() => UIManager.Instance.Window.Open(WindowType.Shop);
    public void OpenEquipmentWindow() => UIManager.Instance.Window.Open(WindowType.Equipment);
    public void OpenStageSelectWindow() => UIManager.Instance.Window.Open(WindowType.StageSelect);
    public void OpenCharacterWindow() => UIManager.Instance.Window.Open(WindowType.Character);
    public void OpenTalentWindow() => UIManager.Instance.Window.Open(WindowType.Talent);
}
