using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattleSession : MonoBehaviour
{
    private const int CHEST_COUNT = 10;
    private static readonly int _isMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int _attackTrigger = Animator.StringToHash("Attack");
    private static readonly int _deathTrigger = Animator.StringToHash("Death");
    [SerializeField, Min(.5f)] private float _waveInterval = 20f;
    [SerializeField, Min(1)] private int _waveCount = 12;
    [SerializeField, Min(1)] private int _enemiesPerWave = 5;

    [SerializeField] private BattleMap _map;
    [SerializeField] private BattleInput _input;
    [SerializeField] private BattleHud _hud;
    [SerializeField] private Camera _camera;
    [SerializeField] private SpriteRenderer _player;
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Transform _actors;
    [SerializeField] private Sprite[] _characters;
    [SerializeField] private Sprite[] _monsters;
    [SerializeField] private RuntimeAnimatorController[] _characterAnimations;
    [SerializeField] private RuntimeAnimatorController[] _monsterAnimations;
    [SerializeField] private Sprite[] _attackIcons;
    [SerializeField] private Sprite _chestSprite;
    [SerializeField] private Sprite _experienceSprite;
    [SerializeField] private Sprite _silverSprite;
    [SerializeField] private Sprite _goldSprite;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [System.Serializable]
    private struct TestSkill
    {
        public bool IsActive;
        [Min(0)] public int Id;
    }

    [Header("레벨업 선택지 테스트")]
    [SerializeField] private bool _useTestChoices;
    [SerializeField] private TestSkill _testChoice1;
    [SerializeField] private TestSkill _testChoice2;
    [SerializeField] private TestSkill _testChoice3;

    [ContextMenu("테스트 레벨업")]
    private void TriggerTestLevelUp()
    {
        if (!Application.isPlaying || !_useTestChoices || Skills == null || _isFinished || _hud.HasModal)
        {
            return;
        }
        _experience = 0f;
        _level++;
        DrawChoices();
        RefreshHud();
    }
#endif

    private sealed class Enemy
    {
        public SpriteRenderer View;
        public Animator Animator;
        public int Id;
        public float Health;
        public float AttackDelay;
        public float SlowTime;
        public float BurnTime;
        public float BurnDamage;
        public int MarkedBy;
        public bool IsChest;
    }

    private sealed class Drop
    {
        public SpriteRenderer View;
        public int Type;
        public int Amount;
        public bool IsAttracted;
    }

    private sealed class Attack
    {
        public int Id;
        public Vector2 Center;
        public bool IsGrounded;
        public float Damage;
        public float Radius;
        public int Count;
        public float Remaining;
        public float Interval;
        public float Delay;
        public bool IsArea;
        public bool IsPassive;
    }

    private readonly List<Enemy> _enemies = new();
    private readonly List<Drop> _drops = new();
    private readonly List<Attack> _attacks = new();
    private readonly Dictionary<int, float> _cooldowns = new();
    private readonly float[] _characterPassives = new float[19];
    private List<BattleSkills.Choice> _choices = new();
    private GameManager _game;
    private CsvTable _stageData;
    private CsvTable _monsterData;
    private int _stageId;
    private int _level;
    private float _experience;
    private int _kills;
    private int _wave;
    private int _awakeningId;
    private int[] _stats;
    private int _critical;
    private int _criticalDamage;
    private int _moveSpeed;
    private int _dodge;
    private int _cooldownReduction;
    private float _health;
    private float _shield;
    private int _waterCasts;
    private float _spawnDelay;
    private bool _hasUsedAwakening;
    private long _silver;
    private long _gold;
    private int _goldRemainder;
    private bool _hasAwakening;
    private bool _isPausedByApp;
    private bool _isFinished;
    private bool _isVictory;
    private bool _isSettled;
    private bool _hasRefreshedChoices;

    public BattleSkills Skills { get; private set; }
    public bool IsEffectPaused => _isPausedByApp || _hud.HasModal;
    public SaveLoadData Settings => _game.Save.CurData;
    private int TargetExperience => 5 + (_level - 1) * 3;
    private float MaxHealth => _stats[2] * (1f + Skills.GetPassiveBonus(3) / 1000f);

    private void Start()
    {
        _game = GameManager.Instance;
        DataManager data = DataManager.Instance;
        if (_game == null || _game.ActiveStageId == 0 || data == null)
        {
            _map.SetTerrain(1);
            SetPlayerSprite(_characters[0]);
            FollowPlayer();
            _hud.ShowUnavailable();
            return;
        }

        _stageData = data.GetTable("StageData");
        _monsterData = data.GetTable("MonsterData");
        BeginStage();
    }

    private void BeginStage()
    {
        foreach (Transform actor in _actors)
        {
            actor.gameObject.SetActive(false);
            Destroy(actor.gameObject);
        }
        _enemies.Clear();
        _drops.Clear();
        _attacks.Clear();
        _cooldowns.Clear();
        _choices.Clear();
        _stageId = _game.ActiveStageId;
        _wave = _kills = _goldRemainder = 0;
        _experience = 0f;
        _shield = 0f;
        _waterCasts = 0;
        _silver = _gold = 0;
        _level = 1;
        _spawnDelay = .5f;
        _hasUsedAwakening = false;
        _isFinished = _isVictory = _isSettled = false;
        _hasRefreshedChoices = false;
        _player.transform.localPosition = Vector3.zero;
        _player.flipX = false;
        _map.SetTerrain(_game.Stages.GetMapType(_stageId));
        int character = Settings.EquippedCharacterId;
        CharacterProgression characters = _game.Characters;
        CsvTable table = characters.Characters;
        CsvTable passiveTable = DataManager.Instance.GetTable("CharacterPassiveSkillData");
        System.Array.Clear(_characterPassives, 0, _characterPassives.Length);
        int stars = characters.GetCharacter(character).Stars;
        for (int field = 16; field <= 18; field += 2)
        {
            int passive = table.GetInt(character, field);
            if (passiveTable.Contains(passive) && stars >= table.GetInt(character, field + 1))
            {
                _characterPassives[passive] = passiveTable.GetInt(passive, 3) / 1000f;
            }
        }
        _stats = characters.GetStats(character);
        _critical = table.GetInt(character, 11);
        _criticalDamage = table.GetInt(character, 12);
        _moveSpeed = table.GetInt(character, 13);
        _dodge = table.GetInt(character, 14);
        _cooldownReduction = table.GetInt(character, 15);
        _hasAwakening = table.GetInt(character, 20) > 0
            && stars >= table.GetInt(character, 21);
        _awakeningId = 19 + table.GetInt(character, 22);
        Skills = new BattleSkills(DataManager.Instance, 1 + table.GetInt(character, 2));
        _health = MaxHealth;
        SetPlayerSprite(_characters[character - 1]);
        _playerAnimator.runtimeAnimatorController = _characterAnimations[character - 1];
        _playerAnimator.Rebind();
        _playerAnimator.Update(0f);

        for (int i = 0; i < CHEST_COUNT; i++)
        {
            Vector2 position = Random.insideUnitCircle * (_map.HalfExtent - 1f);
            position = position.normalized * Mathf.Max(2f, position.magnitude);
            _enemies.Add(new Enemy { View = CreateActor("Chest", _chestSprite, position, .7f, 1), Health = 12f, IsChest = true });
        }

        _input.ResetStick();
        _hud.CloseModal();
        FollowPlayer();
        RefreshHud();
    }

    private void Update()
    {
        if (Skills == null || _isFinished || _isPausedByApp || _hud.HasModal)
        {
            return;
        }

        float delta = Time.deltaTime;
        Vector2 input = _input.Direction;
        Vector2 position = _map.ClampInside((Vector2)_player.transform.position
            + input * (_moveSpeed * .32f * (1f + Skills.GetPassiveBonus(6) / 1000f) * delta), .4f);
        _player.transform.position = position;
        _playerAnimator.SetBool(_isMovingParameter, input.sqrMagnitude > .01f);
        if (Mathf.Abs(input.x) > .01f)
        {
            _player.flipX = input.x < 0;
        }

        SpawnWave(delta, position);
        MoveEnemies(delta, position);
        if (_health <= 0f)
        {
            Finish(false);
            return;
        }

        FireSkills(delta);
        CollectDrops(delta, position);
        RefreshHud();
        if (_wave > _waveCount && !_enemies.Any(enemy => !enemy.IsChest) && !_hud.HasModal
            && !_drops.Any(drop => drop.IsAttracted))
        {
            Finish(true);
        }
    }

    private void LateUpdate()
    {
        FollowPlayer();
    }

    private void SetPlayerSprite(Sprite sprite)
    {
        _player.sprite = sprite;
        _player.transform.localScale = Vector3.one * (.95f / sprite.bounds.size.y);
    }

    private void FollowPlayer()
    {
        Vector3 screen = _camera.transform.position;
        Vector2 center = _map.FollowPosition(_player.transform.position, _camera);
        _camera.transform.position = new Vector3(center.x, center.y, screen.z);
        _hud.MoveHealthBar(_camera.WorldToScreenPoint(_player.transform.position + Vector3.down * .55f));
    }

    private void OnApplicationPause(bool isPaused)
    {
        _isPausedByApp = isPaused;
        if (isPaused)
        {
            _input.ResetStick();
        }
    }

    private void SpawnWave(float delta, Vector2 player)
    {
        _spawnDelay -= delta;
        if (_wave > _waveCount || _spawnDelay > 0f)
        {
            return;
        }

        _spawnDelay = _waveInterval;
        if (_wave == _waveCount)
        {
            SpawnEnemy(6, player);
        }
        else
        {
            for (int i = 0; i < _enemiesPerWave; i++)
            {
                SpawnEnemy(2 + (_wave + i) % 3, player);
            }
            if (_wave % 4 == 3)
            {
                SpawnEnemy(5, player);
            }
        }
        _wave++;
    }

    private void SpawnEnemy(int field, Vector2 player)
    {
        int id = _stageData.GetInt(_stageId, field);
        Vector2 direction = Random.insideUnitCircle.normalized;
        Vector2 spawn = _map.ClampInside(player + direction * 6f, .5f);
        if ((spawn - player).sqrMagnitude < 9f)
        {
            spawn = _map.ClampInside(player - direction * 6f, .5f);
        }
        Sprite sprite = _monsters[id - 1];
        SpriteRenderer view = CreateActor(_monsterData.GetString(id, 0), sprite, spawn, .72f + (field - 2) * .13f, 2);
        Animator animator = view.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = _monsterAnimations[id - 1];
        _enemies.Add(new Enemy { Id = id, Health = _monsterData.GetInt(id, 4), View = view, Animator = animator });
    }

    private void MoveEnemies(float delta, Vector2 player)
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _enemies[i];
            if (enemy.IsChest)
            {
                continue;
            }
            Vector2 current = enemy.View.transform.position;
            if (enemy.BurnTime > 0f)
            {
                enemy.Health -= enemy.BurnDamage * Mathf.Min(delta, enemy.BurnTime);
                if (enemy.Health <= 0f)
                {
                    RemoveEnemy(enemy);
                    continue;
                }
                enemy.BurnTime -= delta;
            }
            float speed = enemy.SlowTime > 0f ? 1f - _characterPassives[7] : 1f;
            enemy.SlowTime -= delta;
            enemy.AttackDelay -= delta;
            if ((player - current).sqrMagnitude > .48f * .48f)
            {
                enemy.View.flipX = player.x < current.x;
                enemy.View.transform.position = Vector2.MoveTowards(current, player,
                    _monsterData.GetInt(enemy.Id, 5) * .11f * speed * delta);
                continue;
            }
            if (enemy.AttackDelay > 0f)
            {
                continue;
            }
            enemy.AttackDelay = Mathf.Max(.3f, _monsterData.GetInt(enemy.Id, 8) / 10f);
            enemy.Animator.SetTrigger(_attackTrigger);
            if (Random.value * 1000f >= _dodge)
            {
                int pattern = _monsterData.GetInt(enemy.Id, 1) == 3 ? Random.Range(9, 12) : 9;
                float damage = _monsterData.GetInt(enemy.Id, 2) * _monsterData.GetInt(enemy.Id, pattern)
                    / 1000f * (1f + _stageId * .035f);
                if (Random.value * 1000f < _monsterData.GetInt(enemy.Id, 6))
                {
                    damage *= 1f + _monsterData.GetInt(enemy.Id, 7) / 1000f;
                }
                float defense = _stats[1] * (1f + Skills.GetPassiveBonus(2) / 1000f);
                damage = Mathf.Max(1f, damage * 100f / (100f + defense));
                float absorbed = Mathf.Min(_shield, damage);
                _shield -= absorbed;
                damage -= absorbed;
                _health = Mathf.Max(0f, _health - damage);
                if (damage > 0f)
                {
                    _hud.ShowDamage(_player.transform.position, damage, true);
                }
            }
        }
    }

    private void FireSkills(float delta)
    {
        foreach (int id in Skills.Active.Keys)
        {
            float remaining = _cooldowns.GetValueOrDefault(id) - delta;
            _cooldowns[id] = remaining;
            if (remaining <= 0f && QueueAttack(id, false, out float cooldown))
            {
                _cooldowns[id] = cooldown;
            }
        }

        for (int i = _attacks.Count - 1; i >= 0; i--)
        {
            Attack attack = _attacks[i];
            attack.Delay -= delta;
            if (attack.Delay <= 0f)
            {
                HitTargets(attack);
                attack.Delay = attack.Interval;
            }
            attack.Remaining -= delta;
            if (attack.Remaining <= 0f)
            {
                _attacks.RemoveAt(i);
            }
        }
    }

    private bool QueueAttack(int id, bool isAwakening, out float cooldown)
    {
        var attack = Skills.GetAttack(id);
        cooldown = Mathf.Max(.25f, attack.Cooldown * (1f - _cooldownReduction / 1000f - _characterPassives[17]));
        int element = BattleSkills.GetActiveElement(id);
        float radius = attack.Radius * (isAwakening ? 2.3f : 1f) * (element == 0 ? 1f + _characterPassives[1] : 1f);
        Vector2 player = _player.transform.position;
        Enemy nearest = _enemies.Where(enemy => ((Vector2)enemy.View.transform.position - player).sqrMagnitude <= radius * radius)
            .OrderBy(enemy => ((Vector2)enemy.View.transform.position - player).sqrMagnitude).FirstOrDefault();
        if (!isAwakening && (nearest == null || (id == 11 && _attacks.Any(pending => pending.Id == id))))
        {
            return false;
        }

        Vector2 target = nearest == null ? player + Vector2.right * radius : nearest.View.transform.position;
        bool isGrounded = id is 11 or 12 or 13 or 14 or 15 or 16 or 20 or 22;
        float startup = id is 2 or 3 or 6 or 7 or 9 or 13 or 15 or 20 ? .25f : .10f;
        float duration = id == 11 ? Mathf.Max(1f, attack.Duration) : attack.Duration;
        _attacks.Add(new Attack
        {
            Id = id,
            Center = target,
            IsGrounded = isGrounded,
            Radius = radius,
            Damage = _stats[0] * attack.Damage * (1f + Skills.GetPassiveBonus(1) / 1000f
                + Mathf.Min(_kills / 10, 20) * _characterPassives[13]) * 5f,
            Count = attack.Count,
            IsArea = isAwakening || id is 8 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18,
            Delay = startup,
            Remaining = startup + Mathf.Max(.05f, duration),
            Interval = id is 8 or 10 or 11 or 14 or 17 ? .65f : Mathf.Max(.35f, cooldown)
        });
        BattleSkillEffect.Play(this, _actors, _player.transform, id, _attackIcons[id - 1], target, radius, duration, attack.Count);
        if (element == 2 && _characterPassives[8] > 0f && ++_waterCasts % 5 == 0)
        {
            QueuePassiveHit(21, target, radius, _stats[0] * _characterPassives[8] * 5f);
        }
        return true;
    }

    private void HitTargets(Attack attack)
    {
        Vector2 player = attack.IsGrounded ? attack.Center : _player.transform.position;
        float radiusSquared = attack.Radius * attack.Radius;
        var targets = _enemies
            .Where(enemy => ((Vector2)enemy.View.transform.position - player).sqrMagnitude <= radiusSquared)
            .OrderBy(enemy => ((Vector2)enemy.View.transform.position - player).sqrMagnitude)
            .Take(attack.IsArea ? _enemies.Count : attack.Count)
            .ToArray();

        foreach (Enemy enemy in targets)
        {
            float damage = attack.Damage;
            if (Random.value * 1000f < _critical + Skills.GetPassiveBonus(4))
            {
                damage *= 1f + (_criticalDamage + Skills.GetPassiveBonus(5)) / 1000f
                    + _characterPassives[14] * _health / MaxHealth;
            }
            if (!enemy.IsChest)
            {
                damage *= 100f / (100f + _monsterData.GetInt(enemy.Id, 3));
                if (!attack.IsPassive)
                {
                    damage = ApplyElementPassives(enemy, attack.Id, damage);
                }
            }
            if (attack.Id == 18 && !enemy.IsChest)
            {
                Vector2 position = enemy.View.transform.position;
                enemy.View.transform.position = _map.ClampInside(position + (position - player).normalized * .8f, .4f);
            }
            enemy.Health -= Mathf.Max(1f, damage);
            _hud.ShowDamage(enemy.View.transform.position, damage, false);
            if (enemy.Health <= 0f)
            {
                RemoveEnemy(enemy);
            }
        }
    }

    private float ApplyElementPassives(Enemy enemy, int skillId, float damage)
    {
        int element = BattleSkills.GetActiveElement(skillId);
        Vector2 position = enemy.View.transform.position;
        if (_characterPassives[2] > 0f)
        {
            if (enemy.MarkedBy != 0 && enemy.MarkedBy != skillId)
            {
                enemy.MarkedBy = 0;
                damage *= 1f + _characterPassives[2];
                PlayPassiveFeedback(19, position, .65f);
            }
            else if (element == 0)
            {
                enemy.MarkedBy = skillId;
                PlayPassiveFeedback(8, position, .6f);
            }
        }
        if (element == 1 && _characterPassives[4] > 0f)
        {
            if (enemy.BurnTime <= 0f)
            {
                PlayPassiveFeedback(20, position, .55f);
            }
            enemy.BurnTime = 2f;
            enemy.BurnDamage = damage * _characterPassives[4] / 2f;
        }
        if (element == 2 && _characterPassives[7] > 0f)
        {
            if (enemy.SlowTime <= 0f)
            {
                PlayPassiveFeedback(11, position, .6f);
            }
            enemy.SlowTime = 2f;
        }
        return damage;
    }

    private void QueuePassiveHit(int skillId, Vector2 position, float radius, float damage, int visualId = 0)
    {
        _attacks.Add(new Attack
        {
            Id = skillId,
            Center = position,
            IsGrounded = true,
            IsArea = true,
            IsPassive = true,
            Radius = radius,
            Damage = damage,
            Delay = .15f,
            Remaining = .25f,
            Interval = 1f
        });
        PlayPassiveFeedback(visualId == 0 ? skillId : visualId, position, radius);
    }

    private void PlayPassiveFeedback(int visualId, Vector2 position, float radius)
    {
        BattleSkillEffect.Play(this, _actors, _player.transform, visualId, _attackIcons[visualId - 1], position, radius, .65f, 1);
    }

    private void RemoveEnemy(Enemy enemy)
    {
        Vector2 position = enemy.View.transform.position;
        int type = enemy.IsChest ? Random.Range(1, 3) : 0;
        if (!enemy.IsChest)
        {
            _kills++;
            if (enemy.BurnTime > 0f && _characterPassives[5] > 0f)
            {
                QueuePassiveHit(19, position, 1.5f, _stats[0] * _characterPassives[5] * 5f, 20);
            }
            if (_characterPassives[10] > 0f)
            {
                float healed = _health + MaxHealth * _characterPassives[10];
                _shield = Mathf.Min(_shield + Mathf.Max(0f, healed - MaxHealth), MaxHealth * _characterPassives[11]);
                _health = Mathf.Min(MaxHealth, healed);
                PlayPassiveFeedback(22, _player.transform.position, .8f);
            }
        }
        Sprite sprite = type == 0 ? _experienceSprite : type == 1 ? _silverSprite : _goldSprite;
        int amount = type == 0 ? _monsterData.GetInt(enemy.Id, 1) : type == 1 ? 100 : 1;
        _drops.Add(new Drop { Type = type, Amount = amount, View = CreateActor("Pickup", sprite, position, .25f, 1) });
        _enemies.Remove(enemy);
        if (enemy.IsChest)
        {
            Despawn(enemy.View);
        }
        else
        {
            enemy.Animator.SetTrigger(_deathTrigger);
            enemy.View.sortingOrder = 0;
            Destroy(enemy.View.gameObject, .65f);
        }
    }

    private void CollectDrops(float delta, Vector2 player)
    {
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            Drop drop = _drops[i];
            Vector2 position = drop.View.transform.position;
            float pickupRadius = 1.75f * (1f + _characterPassives[16]);
            drop.IsAttracted |= (position - player).sqrMagnitude < pickupRadius * pickupRadius;
            if (!drop.IsAttracted)
            {
                continue;
            }
            drop.View.transform.position = Vector2.MoveTowards(position, player, 8f * delta);
            if (((Vector2)drop.View.transform.position - player).sqrMagnitude > .09f)
            {
                continue;
            }

            _drops.RemoveAt(i);
            Despawn(drop.View);
            switch (drop.Type)
            {
                case 0:
                    _experience += drop.Amount * (1f + Skills.GetPassiveBonus(11) / 1000f);
                    if (_experience >= TargetExperience)
                    {
                        _experience = 0;
                        _level++;
                        if (DrawChoices())
                        {
                            _input.ResetStick();
                            return;
                        }
                    }
                    break;
                case 1:
                    _silver += Mathf.RoundToInt(drop.Amount * (1f + Skills.GetPassiveBonus(10) / 1000f));
                    break;
                case 2:
                    int goldUnits = drop.Amount * (1000 + Skills.GetPassiveBonus(10)) + _goldRemainder;
                    _gold += goldUnits / 1000;
                    _goldRemainder = goldUnits % 1000;
                    break;
            }
        }
    }

    private SpriteRenderer CreateActor(string actorName, Sprite sprite, Vector2 position, float height, int sortingOrder)
    {
        GameObject actor = new(actorName);
        actor.transform.SetParent(_actors, false);
        actor.transform.position = position;
        actor.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
        SpriteRenderer view = actor.AddComponent<SpriteRenderer>();
        view.sprite = sprite;
        view.sortingOrder = sortingOrder;
        return view;
    }

    private static void Despawn(SpriteRenderer view)
    {
        view.gameObject.SetActive(false);
        Destroy(view.gameObject);
    }

    private bool DrawChoices()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _choices = _useTestChoices ? Skills.DrawChoices(new[]
        {
            new BattleSkills.Choice(_testChoice1.Id, _testChoice1.IsActive),
            new BattleSkills.Choice(_testChoice2.Id, _testChoice2.IsActive),
            new BattleSkills.Choice(_testChoice3.Id, _testChoice3.IsActive)
        }) : Skills.DrawChoices();
#else
        _choices = Skills.DrawChoices();
#endif
        if (_choices.Count == 0)
        {
            return false;
        }
        _hud.ShowChoices(_choices, !_hasRefreshedChoices);
        return true;
    }

    public void RefreshChoices()
    {
        if (_choices.Count > 0 && !_hasRefreshedChoices)
        {
            _hasRefreshedChoices = true;
            DrawChoices();
        }
    }

    public void ChooseSkill(int index)
    {
        if (index < 0 || index >= _choices.Count)
        {
            return;
        }
        float previous = MaxHealth;
        BattleSkills.Choice choice = _choices[index];
        Skills.Learn(choice);
        _health += MaxHealth - previous;
        _choices.Clear();
        _hud.CloseModal();
        if (!choice.IsActive)
        {
            BattleSkillEffect.Play(this, _actors, _player.transform, 19, _hud.GetSkillIcon(choice.Id, false),
                _player.transform.position, .75f, .65f, 1);
        }
        RefreshHud();
    }

    public void ActivateAwakening()
    {
        if (!_hasAwakening || _hasUsedAwakening || _isFinished || _hud.HasModal || _isPausedByApp)
        {
            return;
        }
        _hasUsedAwakening = QueueAttack(_awakeningId, true, out _);
        RefreshHud();
    }

    private void RefreshHud()
    {
        _hud.Refresh(_level, _experience / TargetExperience, _health / MaxHealth, _silver, _gold,
            _hasAwakening, _hasUsedAwakening);
    }

    private void Finish(bool isVictory)
    {
        _isFinished = true;
        _isVictory = isVictory;
        _playerAnimator.SetBool(_isMovingParameter, false);
        if (!isVictory)
        {
            _playerAnimator.SetTrigger(_deathTrigger);
        }
        _input.ResetStick();
        RetrySettlement();
    }

    public void RetrySettlement()
    {
        if (!_isFinished || _isSettled)
        {
            return;
        }
        _isSettled = _game.TrySettleBattle(_isVictory, _silver, _gold, out long silver, out long gold);
        int next = _game.Stages.GetNextStage(_stageId);
        _hud.ShowResult(_isVictory, _kills, _isSettled ? silver : _silver, _isSettled ? gold : _gold,
            _isSettled, _isVictory && next > 0 && _game.Stages.CanEnter(next));
    }

    public void Replay(bool isNext)
    {
        if (!_isSettled || (isNext && !_isVictory))
        {
            return;
        }
        int target = isNext ? _game.Stages.GetNextStage(_stageId) : _stageId;
        if (_game.TryRestartBattle(target))
        {
            BeginStage();
        }
    }

    public void LeaveBattle()
    {
        if (_isSettled)
        {
            _game.StartAsyncLoad(SceneType.Lobby);
        }
    }
}
