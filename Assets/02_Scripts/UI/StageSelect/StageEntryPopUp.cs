using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageEntryPopUp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _message;
    private int _stageId;

    public void Show(int stageId, string stageName)
    {
        _stageId = stageId;
        _message.text = $"{stageId}. {stageName}\n입장하시겠습니까?";
        UIManager.Instance.PopUp.Open(PopUpType.StageEntry);
    }

    public void ConfirmEntry()
    {
        if (GameManager.Instance.TryEnterStage(_stageId))
        {
            SoundManager.Instance?.PlayConfirm();
            ClosePopUp();
        }
    }

    public void ClosePopUp() => UIManager.Instance.PopUp.Close();
}
