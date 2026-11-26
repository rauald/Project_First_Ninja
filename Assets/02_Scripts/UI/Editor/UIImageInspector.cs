using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class UIImageInspector
{
    private const int PAGE_SIZE = 24;
    private static GameObject _root;
    private static bool _isOpen;
    private static string _search = string.Empty;
    private static int _page;
    private static readonly HashSet<AnimationClip> _expandedClips = new();

    static UIImageInspector()
    {
        Editor.finishedDefaultHeaderGUI += DrawImages;
    }

    private static void DrawImages(Editor editor)
    {
        if (editor.targets.Length != 1)
        {
            return;
        }

        if (editor.target is AnimationClip clip)
        {
            DrawClipFrames(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .Where(IsImageBinding).ToArray());
            return;
        }

        if (editor.target is not GameObject root)
        {
            return;
        }

        var imageComponents = root.GetComponentsInChildren<Component>(true)
            .Where(component => component is Image or RawImage or SpriteRenderer)
            .ToArray();
        if (imageComponents.Length == 0)
        {
            return;
        }

        if (_root != root)
        {
            _root = root;
            _search = string.Empty;
            _page = 0;
        }

        _isOpen = EditorGUILayout.Foldout(_isOpen, "이미지 교체", true);
        if (!_isOpen)
        {
            return;
        }

        string search = EditorGUILayout.TextField("오브젝트 경로 검색", _search);
        if (search != _search)
        {
            _search = search;
            _page = 0;
        }

        var images = imageComponents
            .Select(component => (Component: component, Path: GetPath(root, component)))
            .Where(entry => entry.Path.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        if (images.Length == 0)
        {
            EditorGUILayout.HelpBox("검색 조건에 맞는 이미지가 없습니다.", MessageType.Info);
            return;
        }

        int lastPage = (images.Length - 1) / PAGE_SIZE;
        _page = Mathf.Clamp(_page, 0, lastPage);
        if (lastPage > 0)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_page == 0))
                {
                    if (GUILayout.Button("이전"))
                        _page--;
                }

                GUILayout.Label($"{_page + 1} / {lastPage + 1} 페이지 · {images.Length}개");

                using (new EditorGUI.DisabledScope(_page == lastPage))
                {
                    if (GUILayout.Button("다음"))
                        _page++;
                }
            }
        }

        foreach (var entry in images.Skip(_page * PAGE_SIZE).Take(PAGE_SIZE))
        {
            Component component = entry.Component;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(entry.Path, entry.Path), EditorStyles.boldLabel);
                if (component.gameObject != root && GUILayout.Button("선택", GUILayout.Width(44)))
                {
                    Selection.activeGameObject = component.gameObject;
                }
            }

            using var serialized = new SerializedObject(component);
            bool isSprite = component is not RawImage;
            EditorGUILayout.PropertyField(serialized.FindProperty(isSprite ? "m_Sprite" : "m_Texture"),
                new GUIContent(isSprite ? "스프라이트" : "텍스처"));
            serialized.ApplyModifiedProperties();

            if (component.gameObject == root)
            {
                DrawAnimatedSpriteSources(component);
            }
        }
    }

    private static string GetPath(GameObject root, Component component)
    {
        if (component.gameObject == root)
        {
            return root.name;
        }

        return root.name + "/" + AnimationUtility.CalculateTransformPath(component.transform, root.transform);
    }

    private static void DrawAnimatedSpriteSources(Component component)
    {
        foreach (Animator animator in component.GetComponentsInParent<Animator>(true))
        {
            if (animator.runtimeAnimatorController == null)
            {
                continue;
            }

            string path = AnimationUtility.CalculateTransformPath(component.transform, animator.transform);
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips.Distinct())
            {
                if (clip == null)
                {
                    continue;
                }

                DrawClipFrames(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)
                    .Where(binding => binding.path == path && binding.type == component.GetType()
                        && IsImageBinding(binding)).ToArray());
            }
        }
    }

    private static bool IsImageBinding(EditorCurveBinding binding)
    {
        return ((binding.type == typeof(Image) || binding.type == typeof(SpriteRenderer))
                && binding.propertyName == "m_Sprite")
            || (binding.type == typeof(RawImage) && binding.propertyName == "m_Texture");
    }

    private static void DrawClipFrames(AnimationClip clip, EditorCurveBinding[] bindings)
    {
        if (bindings.Length == 0)
        {
            return;
        }

        bool wasExpanded = _expandedClips.Contains(clip);
        bool isExpanded = EditorGUILayout.Foldout(wasExpanded, $"애니메이션 이미지 프레임 · {clip.name}", true);
        if (!isExpanded)
        {
            _expandedClips.Remove(clip);
            return;
        }

        _expandedClips.Add(clip);
        if (GUILayout.Button($"프로젝트에서 {clip.name} 찾기"))
        {
            EditorGUIUtility.PingObject(clip);
        }

        bool isEditable = AssetDatabase.IsMainAsset(clip) && AssetDatabase.IsOpenForEdit(clip);
        if (!isEditable)
        {
            EditorGUILayout.HelpBox("가져온 클립 또는 읽기 전용 클립입니다. 편집 가능한 Animation Clip에서 프레임을 변경할 수 있습니다.", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!isEditable))
        {
            foreach (EditorCurveBinding binding in bindings)
            {
                DrawBindingFrames(clip, binding);
            }
        }
    }

    private static void DrawBindingFrames(AnimationClip clip, EditorCurveBinding binding)
    {
        var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        if (frames.Length == 0)
        {
            return;
        }

        EditorGUILayout.LabelField(string.IsNullOrEmpty(binding.path) ? "애니메이터 오브젝트" : binding.path,
            EditorStyles.boldLabel);
        var assetType = binding.type == typeof(RawImage) ? typeof(Texture) : typeof(Sprite);

        EditorGUI.BeginChangeCheck();
        for (int index = 0; index < frames.Length; index++)
        {
            frames[index].value = EditorGUILayout.ObjectField($"{index + 1} · {frames[index].time:0.###}초",
                frames[index].value, assetType, false);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(clip, "UI 애니메이션 이미지 교체");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            EditorUtility.SetDirty(clip);
        }
    }
}
