using UnityEngine;

public sealed class LobbyWindow : MonoBehaviour, IWindowable
{
    public void ResetUI()
    {
        UIManager.Instance.PopUp.Close();
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
