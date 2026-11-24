using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterDetailView : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Text _name;
    [SerializeField] private Image _element;
    [Tooltip("무·화·수·목·토·풍 순서로 연결합니다.")]
    [SerializeField] private Sprite[] _elementSprites;
    [Header("등급·레벨·성급 이미지")]
    [SerializeField] private Image _rank;
    [Tooltip("현재 등급 N, R, U, E 순서로 연결합니다.")]
    [SerializeField] private Sprite[] _rankSprites;
    [SerializeField] private CharacterLevelView _level;
    [Tooltip("왼쪽부터 성급을 채우는 별 5칸입니다.")]
    [SerializeField] private Image[] _starImages;
    [SerializeField] private Sprite _emptyStar;
    [SerializeField] private Sprite _filledStar;
    [SerializeField] private Text[] _stats;
    [SerializeField] private Button[] _skills;
    [SerializeField] private Image[] _skillIcons;
    [SerializeField] private GameObject[] _locks;

    private float _attackTimer;

    public void RefreshCharacter(CharacterPresentation presentation, Action<int> onSkill)
    {
        CharacterProgression characters = GameManager.Instance.Characters;
        CharacterProgressData state = characters.GetCharacter(presentation.CharacterId);
        CsvTable table = characters.Characters;
        int id = presentation.CharacterId;
        _name.text = table.GetString(id, 0);
        int rank = state.IsAvailable ? state.Rank : 0;
        int level = state.IsAvailable ? state.Level : 1;
        int stars = state.IsAvailable ? state.Stars : 0;
        int element = table.GetInt(id, 7);
        _element.enabled = (uint)element < (uint)_elementSprites.Length;
        _element.sprite = _element.enabled ? _elementSprites[element] : null;
        _rank.sprite = (uint)rank < (uint)_rankSprites.Length ? _rankSprites[rank] : null;
        _level.SetLevel(level);
        for (int i = 0; i < _starImages.Length; i++)
        {
            _starImages[i].sprite = i < stars ? _filledStar : _emptyStar;
        }
        int[] stats = characters.GetStats(id);
        string[] names = { "공격력", "방어력", "체력" };
        for (int i = 0; i < _stats.Length; i++)
        {
            _stats[i].text = $"{names[i]}   {stats[i]:N0}";
        }

        for (int i = 0; i < _skills.Length; i++)
        {
            int slot = i;
            _skillIcons[i].sprite = presentation.SkillIcons[i];
            _locks[i].SetActive(!IsSkillUnlocked(id, i));
            _skills[i].onClick.RemoveAllListeners();
            _skills[i].onClick.AddListener(() => onSkill(slot));
        }

        ApplyPresentation(_animator, presentation);
        _attackTimer = 0f;
    }

    private void Update()
    {
        _attackTimer += Time.unscaledDeltaTime;
        if (_attackTimer >= 5f)
        {
            _attackTimer %= 5f;
            _animator.SetTrigger("Attack");
        }
    }

    public static void ApplyPresentation(Animator animator, CharacterPresentation presentation)
    {
        animator.GetComponent<Image>().sprite = presentation.Portrait;
        animator.runtimeAnimatorController = presentation.Controller;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.Rebind();
        animator.speed = 1.5f;
        animator.Update(0f);
    }

    public static bool IsSkillUnlocked(int id, int slot)
    {
        CharacterProgression characters = GameManager.Instance.Characters;
        CharacterProgressData state = characters.GetCharacter(id);
        // 각성기는 각성기 개방 패시브(세 번째 패시브)의 성급 조건을 함께 사용한다.
        int conditionField = 17 + Mathf.Min(slot, 2) * 2;
        return state.IsAvailable && state.Stars >= characters.Characters.GetInt(id, conditionField);
    }

    public static string GetRankName(int rank)
    {
        return rank switch { 0 => "N", 1 => "R", 2 => "U", 3 => "E", _ => "?" };
    }

}
