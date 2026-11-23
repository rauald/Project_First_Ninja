using UnityEngine;

public class BottomBar : MonoBehaviour
{
    public void OpenShopWindow()
    {
        UIManager.Instance.PopUp.Close();
        UIManager.Instance.Window.Open(WindowType.Shop);
    }

    public void OpenEquipmentWindow()
    {
        UIManager.Instance.PopUp.Close();
        UIManager.Instance.Window.Open(WindowType.Equipment);
    }

    public void OpenLobbyWindow()
    {
        UIManager.Instance.PopUp.Close();
        UIManager.Instance.Window.Open(WindowType.Lobby);
    }

    public void OpenCharacterWindow()
    {
        UIManager.Instance.PopUp.Close();
        UIManager.Instance.Window.Open(WindowType.Character);
    }

    public void OpenTalentWindow()
    {
        UIManager.Instance.PopUp.Close();
        UIManager.Instance.Window.Open(WindowType.Talent);
    }
}
