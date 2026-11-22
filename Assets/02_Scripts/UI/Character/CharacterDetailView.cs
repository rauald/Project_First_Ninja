using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterDetailView : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Text _name;
    [SerializeField] private Text _rankLevel;
    [SerializeField] private Text _stars;
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
        _rankLevel.text = $"{GetRankName(rank)}   Lv. {level}   {GetElementName(table.GetInt(id, 7))}";
        _stars.text = new string('★', stars) + new string('☆', 5 - stars);
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

    public static string GetElementName(int element)
    {
        return element switch { 0 => "무", 1 => "화", 2 => "수", 3 => "목", 4 => "토", 5 => "풍", _ => "?" };
    }
}
