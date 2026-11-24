using UnityEngine;

[AddComponentMenu("UI/Scene Loading Artwork")]
internal sealed class SceneLoadingArtwork : MonoBehaviour
{
    [Header("씬 전환 배경")]
    [Tooltip("타이틀, 로비, 전투 사이의 로딩 화면을 채웁니다. 비워두면 기존 검은 배경을 사용합니다.")]
    [SerializeField] private Sprite _backdrop;

    [Header("로딩 회전 이미지")]
    [Tooltip("가운데에서 회전하는 100×100 이미지입니다. 비워두면 기존 12칸 회전 표시를 사용합니다.")]
    [SerializeField] private Sprite _spinner;

    public Sprite Backdrop => _backdrop;
    public Sprite Spinner => _spinner;
}
