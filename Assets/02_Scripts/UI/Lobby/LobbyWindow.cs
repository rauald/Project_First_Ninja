using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyWindow : MonoBehaviour, IWindowable
{
    private const float ATTACK_INTERVAL = 5f;
    private static readonly int ATTACK_TRIGGER = Animator.StringToHash("Attack");

    [SerializeField] private Animator _characterAnimator;
    [SerializeField] private CharacterWindow _characterWindow;
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;

    private float _attackTimer;

    private void OnEnable()
    {
        _attackTimer = 0f;
        _characterAnimator.Rebind();
        _characterAnimator.speed = 1.5f;
        _characterAnimator.Update(0f);
    }

    private void Update()
    {
        _attackTimer += Time.unscaledDeltaTime;
        if (_attackTimer < ATTACK_INTERVAL)
        {
            return;
        }

        _attackTimer %= ATTACK_INTERVAL;
        _characterAnimator.SetTrigger(ATTACK_TRIGGER);
    }

    public void ResetUI()
    {
        UIManager.Instance.PopUp.Close();
        CharacterProgression characters = GameManager.Instance.Characters;
        CharacterDetailView.ApplyPresentation(_characterAnimator,
            _characterWindow.GetPresentation(characters.Data.EquippedCharacterId));
        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
    }

    public void OpenSettingsPopUp()
    {
        UIManager.Instance.PopUp.Open(PopUpType.Settings);
    }

    public void OpenShopWindow()
    {
        UIManager.Instance.Window.Open(WindowType.Shop);
    }

    public void OpenEquipmentWindow()
    {
        UIManager.Instance.Window.Open(WindowType.Equipment);
    }

    public void OpenStageSelectWindow()
    {
        UIManager.Instance.Window.Open(WindowType.StageSelect);
    }

    public void OpenCharacterWindow()
    {
        UIManager.Instance.Window.Open(WindowType.Character);
    }

    public void OpenTalentWindow()
    {
        UIManager.Instance.Window.Open(WindowType.Talent);
    }
}
