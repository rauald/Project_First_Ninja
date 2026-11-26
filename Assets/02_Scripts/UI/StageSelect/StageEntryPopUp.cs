using UnityEngine;

public sealed class StageEntryPopUp : MonoBehaviour
{
    public void Show(int stageId, string stageName)
    {
        UIManager.Instance.PopUp.ShowChoice($"{stageId}. {stageName}\n입장하시겠습니까?", () =>
        {
            if (GameManager.Instance.TryEnterStage(stageId))
            {
                SoundManager.Instance?.PlayConfirm();
                return;
            }

            UIManager.Instance.PopUp.ShowNotice("입장할 수 없습니다.");
        });
    }
}
