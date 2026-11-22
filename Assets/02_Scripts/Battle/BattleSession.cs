using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattleSession : MonoBehaviour
{
    private const int CHEST_COUNT = 10;
    private const int FINAL_WAVE = 5;
    private const int FIRST_WAVE_EXPERIENCE_ROW = 8;
    private const int NAMED_EXPERIENCE_ROW = 12;
    private const float BASIC_SHOT_INTERVAL = .09f;
    private static readonly int _isMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int _attackTrigger = Animator.StringToHash("Attack");
    private static readonly int _deathTrigger = Animator.StringToHash("Death");
    private const float SPAWN_WINDOW = 10f;
    [SerializeField, Min(SPAWN_WINDOW)] private float _waveInterval = 60f;

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
    [System.Serializable]
    private struct ElementalRange
    {
        public string Name;
        [Min(.01f)] public float Radius;
    }

    [SerializeField] private ElementalRange[] _elementalRanges;
    [SerializeField] private Texture2D[] _elementalSprites;
    private readonly Dictionary<(int, int), Sprite> _elementalSpriteCache = new();

    private void OnDestroy()
    {
        foreach (Sprite sprite in _elementalSpriteCache.Values)
            Destroy(sprite);
    }

    public Sprite GetElementalSprite(int slot, int frame = 0)
    {
        if (_elementalSprites == null || slot >= _elementalSprites.Length || !_elementalSprites[slot])
            return null;
        if (_elementalSpriteCache.TryGetValue((slot, frame), out Sprite sprite))
            return sprite;
        Texture2D texture = _elementalSprites[slot];
        bool sheet = slot is 5 or 12;
        int width = texture.width / (sheet ? 4 : 1);
        int height = texture.height / (sheet ? 2 : 1);
        var rect = new Rect(frame % 4 * width, sheet ? (1 - frame / 4) * height : 0, width, height);
        if (frame > 0 && (slot is 6 or 8))
        {
            int piece = frame - 1;
            width /= 3;
            height /= 3;
            rect = new Rect(piece % 3 * width, piece / 3 * height, width, height);
        }
        else if (frame > 0 && slot == 9)
        {
            int visibleHeight = Mathf.Max(1, height * frame / 12);
            rect = new Rect(0, height - visibleHeight, width, visibleHeight);
        }
        sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        _elementalSpriteCache.Add((slot, frame), sprite);
        return sprite;
    }
    [SerializeField] private Sprite _chestSprite;
    [SerializeField] private Sprite[] _experienceSprites;
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
        public int ExperienceReward;
    }

    private sealed class Drop
    {
        public SpriteRenderer View;
        public int Type;
        public int Amount;
        public bool IsAttracted;
        public float CanCollectAt;
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
        public Enemy Target;
        public Vector2 Start;
        public Vector2 Direction;
        public float Travel;
        public float TravelTime;
        public float Progress;
        public float CharacterSize;
        public HashSet<Enemy> Pierced;
        public bool HasLaunched;
        public BattleSkillEffect Flight;
        public float Elapsed;
        public float NextTick;
    }

    private readonly List<Enemy> _enemies = new();
    private readonly List<Drop> _drops = new();
    private readonly List<Attack> _attacks = new();
    private Vector2 _facing = Vector2.right;
    private readonly Dictionary<int, float> _cooldowns = new();
    private readonly float[] _characterPassives = new float[19];
    private List<BattleSkills.Choice> _choices = new();
    private GameManager _game;
    private CsvTable _stageData;
    private CsvTable _monsterData;
    private CsvTable _battleLevelData;
    private CsvTable _defaultData;
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
    private int _regularSpawned;
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
    private int TargetExperience => _battleLevelData.Contains(_level) ? _battleLevelData.GetInt(_level, 0) : 0;
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
        _battleLevelData = data.GetTable("BattleLevelData");
        _defaultData = data.GetTable("DefaultData");
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
        _regularSpawned = 0;
        _hasUsedAwakening = false;
        _isFinished = _isVictory = _isSettled = false;
        _hasRefreshedChoices = false;
        _player.transform.localPosition = Vector3.zero;
        _player.flipX = false;
        _facing = Vector2.right;
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
        if (input.sqrMagnitude > .01f)
        {
            _facing = input.normalized;
        }
        if (Mathf.Abs(input.x) > .01f)
        {
            _player.flipX = input.x < 0;
        }

        SpawnWave(delta);
        MoveEnemies(delta, position);
        if (_health <= 0f)
        {
            Finish(false);
            return;
        }

        FireSkills(delta);
        if (!ProcessLevelUps())
        {
            CollectDrops(delta, position);
        }
        RefreshHud();
        if (_wave == FINAL_WAVE && !_enemies.Any(enemy => !enemy.IsChest) && !_hud.HasModal
            && !_drops.Any(drop => drop.IsAttracted || Time.time < drop.CanCollectAt))
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

    private void SpawnWave(float delta)
    {
        _spawnDelay -= delta;
        if (_wave >= FINAL_WAVE)
        {
            return;
        }

        if (_spawnDelay <= 0f)
        {
            _spawnDelay += _waveInterval;
            _regularSpawned = 0;
            _wave++;
            if (_wave == FINAL_WAVE)
            {
                for (int i = _enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = _enemies[i];
                    if (!enemy.IsChest)
                    {
                        _enemies.RemoveAt(i);
                        PlayEnemyDeath(enemy);
                    }
                }
                SpawnEnemy(6, NAMED_EXPERIENCE_ROW);
                _hud.ShowSpawnWarning(true);
                return;
            }
            if (_wave == 3)
            {
                SpawnEnemy(5, NAMED_EXPERIENCE_ROW);
                _hud.ShowSpawnWarning(false);
            }
        }
        if (_wave == 0)
        {
            return;
        }

        int regularCount = (50 + (_wave - 1) * 10) * (1 - _wave / FINAL_WAVE) * 2;
        float progress = Mathf.Clamp01((_waveInterval - _spawnDelay) / SPAWN_WINDOW);
        int due = progress >= 1f ? regularCount : Mathf.FloorToInt(progress * regularCount);
        while (_regularSpawned < due)
        {
            int field = _wave == 4 ? 2 + _regularSpawned % 3 : 1 + _wave;
            SpawnEnemy(field, FIRST_WAVE_EXPERIENCE_ROW + _wave - 1);
            _regularSpawned++;
        }
    }

    private void SpawnEnemy(int field, int experienceRow)
    {
        int id = _stageData.GetInt(_stageId, field);
        float edge = Mathf.Max(0f, _map.HalfExtent - 1f);
        float alongEdge = Random.Range(-edge, edge);
        Vector2 spawn = Random.Range(0, 4) switch
        {
            0 => new Vector2(-edge, alongEdge),
            1 => new Vector2(edge, alongEdge),
            2 => new Vector2(alongEdge, -edge),
            _ => new Vector2(alongEdge, edge)
        };
        Sprite sprite = _monsters[id - 1];
        SpriteRenderer view = CreateActor(_monsterData.GetString(id, 0), sprite, spawn, .72f + (field - 2) * .13f, 2);
        Animator animator = view.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = _monsterAnimations[id - 1];
        _enemies.Add(new Enemy
        {
            Id = id, Health = _monsterData.GetInt(id, 4), View = view, Animator = animator,
            ExperienceReward = _defaultData.GetInt(experienceRow, 0)
        });
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
            if (attack.Id is >= 7 and <= 18 && !attack.IsPassive)
            {
                if (AdvanceElementalAttack(attack, delta))
                {
                    _attacks.RemoveAt(i);
                }
                continue;
            }
            if (attack.TravelTime > 0f)
            {
                float flightDelta = delta;
                if (!attack.HasLaunched)
                {
                    attack.Delay -= delta;
                    if (attack.Delay > 0f)
                    {
                        continue;
                    }
                    flightDelta = -attack.Delay;
                    LaunchBasicAttack(attack);
                }
                AdvanceBasicAttack(attack, flightDelta);
                if (attack.Progress >= 1f)
                {
                    _attacks.RemoveAt(i);
                }
                continue;
            }
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
        float? baseRadius = id is >= 7 and <= 18 && _elementalRanges != null && id - 7 < _elementalRanges.Length
            ? Mathf.Max(.01f, _elementalRanges[id - 7].Radius) : null;
        var attack = Skills.GetAttack(id, baseRadius);
        cooldown = Mathf.Max(.25f, attack.Cooldown * (1f - _cooldownReduction / 1000f - _characterPassives[17]));
        int element = BattleSkills.GetActiveElement(id);
        float radius = attack.Radius * (isAwakening ? 2.3f : 1f) * (element == 0 ? 1f + _characterPassives[1] : 1f);
        Vector2 player = _player.transform.position;
        float damage = _stats[0] * attack.Damage * (1f + Skills.GetPassiveBonus(1) / 1000f
            + Mathf.Min(_kills / 10, 20) * _characterPassives[13]) * 5f;
        if (id is >= 1 and <= 6)
        {
            return QueueBasicAttack(id, radius, damage, attack.Count, attack.Duration);
        }
        if (id is >= 7 and <= 18)
        {
            QueueElementalAttack(id, player, radius, damage, attack.Count, attack.Duration);
            return true;
        }
        Enemy nearest = _enemies.Where(enemy => ((Vector2)enemy.View.transform.position - player).sqrMagnitude <= radius * radius)
            .OrderBy(enemy => ((Vector2)enemy.View.transform.position - player).sqrMagnitude).FirstOrDefault();
        bool isGrounded = id is 20 or 22;
        Vector2 target = nearest == null
            ? (isGrounded ? player : player + _facing * radius)
            : nearest.View.transform.position;
        float startup = id == 20 ? .25f : .10f;
        float duration = attack.Duration;
        _attacks.Add(new Attack
        {
            Id = id,
            Center = target,
            IsGrounded = isGrounded,
            Radius = radius,
            Damage = damage,
            Count = attack.Count,
            IsArea = isAwakening,
            Delay = startup,
            Remaining = startup + Mathf.Max(.05f, duration),
            Interval = Mathf.Max(.35f, cooldown)
        });
        BattleSkillEffect.Play(this, _actors, _player.transform, id, _attackIcons[id - 1], target, radius, duration, attack.Count);
        if (element == 2 && _characterPassives[8] > 0f && ++_waterCasts % 5 == 0)
        {
            QueuePassiveHit(21, target, radius, _stats[0] * _characterPassives[8] * 5f);
        }
        return true;
    }

    private bool QueueBasicAttack(int id, float radius, float damage, int count, float duration)
    {
        Vector2 origin = _player.transform.position;
        int shots = id is 2 or 3 or 6 ? Mathf.Max(1, count) : 1;
        Enemy[] targets = _enemies
            .OrderBy(enemy => ((Vector2)enemy.View.transform.position - origin).sqrMagnitude)
            .Take(shots).ToArray();

        float time = id switch { 1 => .25f, 4 => .33f, 5 => duration > 0f ? duration : .28f,
            6 => Mathf.Max(.01f, duration), _ => 1f };
        // 전투 목록은 뒤에서부터 갱신하므로, 같은 프레임에 발사할 때도 가까운 적부터 처리한다.
        for (int shot = shots - 1; shot >= 0; shot--)
        {
            Enemy target = targets.Length == 0 ? null : targets[shot % targets.Length];
            _attacks.Add(new Attack
            {
                Id = id, Target = target, TravelTime = time, Radius = radius,
                Delay = shot * BASIC_SHOT_INTERVAL, Damage = damage
            });
        }
        if (id == 3 && _characterPassives[8] > 0f && ++_waterCasts % 5 == 0)
        {
            Vector2 center = targets.Length == 0 ? origin : targets[0].View.transform.position;
            QueuePassiveHit(21, center, radius, _stats[0] * _characterPassives[8] * 5f);
        }
        return true;
    }

    private void LaunchBasicAttack(Attack attack)
    {
        Vector2 origin = _player.transform.position;
        float size = Mathf.Max(.1f, _player.bounds.size.y);
        if (attack.Target != null && !_enemies.Contains(attack.Target))
        {
            attack.Target = null;
        }
        Vector2 direction = attack.Target == null
            ? _facing : ((Vector2)attack.Target.View.transform.position - origin).normalized;
        if (direction == Vector2.zero)
        {
            direction = _facing;
        }
        if (attack.Id is 1 or 4)
        {
            direction = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2(Mathf.Sign(direction.x), 0f) : new Vector2(0f, Mathf.Sign(direction.y));
        }
        bool piercing = attack.Id is 5 or 6;
        bool tracksTarget = attack.Id is 2 or 3;
        attack.HasLaunched = true;
        attack.Direction = direction;
        attack.CharacterSize = size;
        attack.Travel = attack.Id switch { 5 => size, 6 => 5f * attack.TravelTime, _ => 0f };
        float launchOffset = piercing ? size * .5f : tracksTarget ? size * .35f : 0f;
        if (tracksTarget && attack.Target != null)
        {
            launchOffset = Mathf.Min(launchOffset, Vector2.Distance(origin, attack.Target.View.transform.position));
        }
        attack.Start = origin + direction * launchOffset;
        attack.Center = attack.Start + direction * attack.Travel;
        attack.Pierced = piercing ? new HashSet<Enemy>() : null;
        attack.Flight = BattleSkillEffect.PlayBasic(this, _actors, attack.Id, attack.Start, attack.Center,
            direction, size, attack.Radius, attack.TravelTime);
    }

    private void AdvanceBasicAttack(Attack attack, float delta)
    {
        if (attack.Id is 2 or 3)
        {
            AdvanceTrackedProjectile(attack, delta);
            return;
        }
        float previous = attack.Progress;
        attack.Progress = Mathf.Min(1f, previous + delta / attack.TravelTime);
        if (attack.Id is 5 or 6)
        {
            float width = attack.CharacterSize * (attack.Id == 5 ? .48f : .26f);
            HitPiercingTargets(attack, previous * attack.Travel, attack.Progress * attack.Travel, width);
            if (attack.Id == 6)
            {
                if (attack.Progress >= 1f)
                {
                    attack.Flight.EndFlight();
                }
                else
                {
                    attack.Flight.SetFlightPose(attack.Start + attack.Direction * (attack.Progress * attack.Travel),
                        attack.Direction);
                }
            }
        }
        else if (previous < .5f && attack.Progress >= .5f)
        {
            float minimumDot = Mathf.Cos(BattleSkillEffect.GetSweepHalfAngle(attack.Id) * Mathf.Deg2Rad);
            var targets = _enemies.Where(enemy =>
            {
                Vector2 offset = (Vector2)enemy.View.transform.position - attack.Start;
                return offset.sqrMagnitude <= attack.Radius * attack.Radius
                    && (offset == Vector2.zero || Vector2.Dot(offset.normalized, attack.Direction) >= minimumDot);
            }).OrderBy(enemy => ((Vector2)enemy.View.transform.position - attack.Start).sqrMagnitude).ToArray();
            foreach (Enemy enemy in targets)
            {
                HitBasicEnemy(attack, enemy);
            }
        }
    }

    private void HitPiercingTargets(Attack attack, float from, float to, float width)
    {
        var targets = new List<(Enemy Enemy, float Entry)>();
        foreach (Enemy enemy in _enemies)
        {
            if (!attack.Pierced.Contains(enemy) && TouchesPath(enemy, attack.Start, attack.Direction,
                from, to, width, out float entry))
            {
                targets.Add((enemy, entry));
            }
        }
        foreach (var hit in targets.OrderBy(hit => hit.Entry))
        {
            attack.Pierced.Add(hit.Enemy);
            HitBasicEnemy(attack, hit.Enemy);
        }
    }

    private void QueueElementalAttack(int id, Vector2 origin, float radius, float damage, int count, float duration)
    {
        if (id == 11)
        {
            Attack puddle = _attacks.Find(pending => pending.Id == 11);
            if (puddle != null)
            {
                puddle.Radius = radius;
                puddle.Damage = damage;
                return;
            }
        }

        float interval = id == 8 ? .2f : id is 10 or 11 or 14 or 17 ? .4f : 0f;
        float life = id switch
        {
            11 => float.PositiveInfinity,
            12 => .58f,
            13 => duration > 0f ? duration : .5f,
            15 => .65f,
            16 => .44f,
            18 => .55f,
            _ => Mathf.Max(.01f, duration)
        };
        float firstHit = id switch
        {
            12 => .19f, 13 => life, 15 => .43f, 16 => .14f, 18 => .13f, _ => interval
        };
        Vector2 direction = _facing;
        bool piercing = id is 7 or 9;
        float travel = id switch { 7 => 8f * life, 9 => 7f * life, _ => 0f };
        int instances = id switch
        {
            16 => 3,
            7 or 9 or 12 or 13 or 14 or 15 or 17 => Mathf.Max(1, count),
            _ => 1
        };
        Vector2[] fruitTargets = id == 13
            ? _enemies.OrderBy(enemy => ((Vector2)enemy.View.transform.position - origin).sqrMagnitude)
                .Take(instances).Select(enemy => (Vector2)enemy.View.transform.position).ToArray()
            : System.Array.Empty<Vector2>();
        for (int i = instances - 1; i >= 0; i--)
        {
            Vector2 center = id switch
            {
                8 => _map.ClampInside(origin + direction * radius, .15f),
                12 or 14 or 15 or 17 => RandomVisiblePoint(),
                13 => fruitTargets.Length == 0 ? _map.ClampInside(origin + direction * radius, .15f)
                    : fruitTargets[i % fruitTargets.Length],
                16 => _map.ClampInside(origin + direction * radius * (.45f + i * .52f), .15f),
                _ => origin
            };
            Vector2 start = piercing ? origin + direction * (_player.bounds.size.y * .35f) : origin;
            _attacks.Add(new Attack
            {
                Id = id, Start = start, Center = center, Direction = direction, Radius = radius,
                Damage = damage, Count = id == 10 ? Mathf.Max(1, count) : 1,
                Remaining = life, Interval = interval, NextTick = firstHit, Travel = travel,
                Delay = id == 16 ? i * .15f : id is 7 or 9 or 13 ? i * BASIC_SHOT_INTERVAL : 0f,
                Pierced = piercing ? new HashSet<Enemy>() : null,
                CharacterSize = Mathf.Max(.1f, _player.bounds.size.y)
            });
        }
        if (id is 11 or 12 && _characterPassives[8] > 0f && ++_waterCasts % 5 == 0)
        {
            QueuePassiveHit(21, origin, radius, _stats[0] * _characterPassives[8] * 5f);
        }
    }

    private Vector2 RandomVisiblePoint()
    {
        Vector3 point = _camera.ViewportToWorldPoint(new Vector3(Random.Range(.14f, .86f),
            Random.Range(.16f, .84f), Mathf.Abs(_camera.transform.position.z)));
        return _map.ClampInside(point, .25f);
    }

    private bool AdvanceElementalAttack(Attack attack, float delta)
    {
        if (!attack.HasLaunched)
        {
            attack.Delay -= delta;
            if (attack.Delay > 0f)
            {
                return false;
            }
            delta = -attack.Delay;
            attack.HasLaunched = true;
            Vector2 position = attack.Id is 7 or 9 or 13 ? attack.Start : attack.Center;
            attack.Flight = BattleSkillEffect.PlayElemental(this, _actors, attack.Id, position, attack.Direction, attack.Count);
        }
        float previous = attack.Elapsed;
        attack.Elapsed = Mathf.Min(attack.Remaining, previous + delta);
        if (attack.Id is 7 or 9)
        {
            float speed = attack.Travel / attack.Remaining;
            HitPiercingTargets(attack, previous * speed, attack.Elapsed * speed,
                attack.Radius * (attack.Id == 7 ? .48f : .26f));
            attack.Center = attack.Start + attack.Direction * (attack.Elapsed * speed);
        }
        else
        {
            if (attack.Id is 10 or 11)
            {
                attack.Center = _player.transform.position;
            }
            while (attack.NextTick <= attack.Elapsed)
            {
                HitElementalArea(attack, attack.NextTick);
                attack.NextTick = attack.Interval > 0f ? attack.NextTick + attack.Interval : float.PositiveInfinity;
            }
        }

        if (attack.Elapsed >= attack.Remaining)
        {
            if (attack.Id == 13)
            {
                BattleSkillEffect.PlayImpact(this, _actors, 13, attack.Center, attack.Direction, attack.Radius * .8f);
            }
            attack.Flight.EndFlight();
            return true;
        }
        attack.Flight.SetElementalPose(attack.Start, attack.Center, attack.Direction, attack.Radius,
            attack.Elapsed, attack.Remaining, attack.Count);
        return false;
    }

    private void HitElementalArea(Attack attack, float tick)
    {
        float radiusSquared = attack.Radius * attack.Radius;
        var targets = _enemies.Where(enemy =>
        {
            Vector2 position = enemy.View.transform.position;
            if (attack.Id == 11)
            {
                Vector2 offset = position - BattleSkillEffect.GetPuddleCenter(attack.Center);
                offset.y /= BattleSkillEffect.PUDDLE_SQUASH;
                return offset.sqrMagnitude <= radiusSquared;
            }
            if (attack.Id == 10)
            {
                for (int i = 0; i < attack.Count; i++)
                {
                    Vector2 blade = BattleSkillEffect.GetOrbitOffset(tick, attack.Radius, i, attack.Count);
                    float width = attack.Radius * .32f + EnemyRadius(enemy);
                    if ((position - attack.Center - blade).sqrMagnitude <= width * width)
                    {
                        return true;
                    }
                }
                return false;
            }
            return (position - attack.Center).sqrMagnitude <= radiusSquared;
        }).ToArray();
        foreach (Enemy enemy in targets)
        {
            DamageEnemy(attack, enemy, attack.Center);
        }
    }

    private void AdvanceTrackedProjectile(Attack attack, float delta)
    {
        if (attack.Target != null && !_enemies.Contains(attack.Target))
        {
            attack.Target = null;
        }
        if (attack.Target != null)
        {
            Vector2 offset = (Vector2)attack.Target.View.transform.position - attack.Center;
            if (offset != Vector2.zero)
            {
                attack.Direction = offset.normalized;
            }
        }

        float step = (attack.Id == 2 ? 10f : 9f) * delta;
        float width = attack.CharacterSize * .18f;
        Enemy hit = attack.Target;
        if (hit == null)
        {
            float nearestEntry = float.PositiveInfinity;
            foreach (Enemy enemy in _enemies)
            {
                if (TouchesPath(enemy, attack.Center, attack.Direction, 0f, step, width, out float entry)
                    && entry < nearestEntry)
                {
                    hit = enemy;
                    nearestEntry = entry;
                }
            }
        }
        if (hit != null && TouchesPath(hit, attack.Center, attack.Direction, 0f, step, width, out _))
        {
            HitBasicEnemy(attack, hit);
            attack.Flight.EndFlight();
            attack.Progress = 1f;
            return;
        }

        attack.Center += attack.Direction * step;
        attack.Flight.SetFlightPose(attack.Center, attack.Direction);
        float limit = _map.HalfExtent + attack.CharacterSize;
        if (Mathf.Abs(attack.Center.x) > limit || Mathf.Abs(attack.Center.y) > limit)
        {
            attack.Flight.EndFlight();
            attack.Progress = 1f;
        }
    }

    private static float EnemyRadius(Enemy enemy) => Mathf.Min(enemy.View.bounds.extents.x, enemy.View.bounds.extents.y);

    private static bool TouchesPath(Enemy enemy, Vector2 origin, Vector2 direction, float from, float to,
        float width, out float entry)
    {
        Vector2 offset = (Vector2)enemy.View.transform.position - origin;
        float along = Vector2.Dot(offset, direction);
        float across = offset.x * direction.y - offset.y * direction.x;
        float radius = width + EnemyRadius(enemy);
        float depthSquared = radius * radius - across * across;
        float depth = Mathf.Sqrt(Mathf.Max(0f, depthSquared));
        entry = along - depth;
        return depthSquared >= 0f && entry <= to && along + depth >= from;
    }

    private void HitBasicEnemy(Attack attack, Enemy enemy)
    {
        BattleSkillEffect.PlayImpact(this, _actors, attack.Id, enemy.View.transform.position,
            attack.Direction, attack.CharacterSize);
        DamageEnemy(attack, enemy, attack.Start);
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
            DamageEnemy(attack, enemy, player);
        }
    }

    private void DamageEnemy(Attack attack, Enemy enemy, Vector2 player)
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
        int amount = type == 0 ? enemy.ExperienceReward : type == 1 ? 100 : 1;
        Sprite sprite = type == 0 ? GetExperienceSprite(amount) : type == 1 ? _silverSprite : _goldSprite;
        _drops.Add(new Drop
        {
            Type = type, Amount = amount,
            View = CreateActor("Pickup", sprite, position, .25f, 1),
            CanCollectAt = Time.time + (type == 0 ? .2f : 0f)
        });
        _enemies.Remove(enemy);
        if (enemy.IsChest)
        {
            Despawn(enemy.View);
        }
        else
        {
            PlayEnemyDeath(enemy);
        }
    }

    private Sprite GetExperienceSprite(int amount)
    {
        int index = amount switch
        {
            2 => 1,
            3 => 2,
            5 => 3,
            200 => 4,
            _ => 0
        };
        return _experienceSprites[index];
    }

    private static void PlayEnemyDeath(Enemy enemy)
    {
        enemy.Animator.SetTrigger(_deathTrigger);
        enemy.View.sortingOrder = 0;
        Destroy(enemy.View.gameObject, .65f);
    }

    private void CollectDrops(float delta, Vector2 player)
    {
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            Drop drop = _drops[i];
            Vector2 position = drop.View.transform.position;
            float pickupRadius = 1.75f * (1f + _characterPassives[16]);
            drop.IsAttracted |= (position - player).sqrMagnitude < pickupRadius * pickupRadius;
            if (!drop.IsAttracted || Time.time < drop.CanCollectAt)
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
                    if (TargetExperience > 0)
                    {
                        _experience += drop.Amount * (1f + Skills.GetPassiveBonus(11) / 1000f);
                        if (ProcessLevelUps())
                        {
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

    private bool ProcessLevelUps()
    {
        int target = TargetExperience;
        while (target > 0 && _experience >= target)
        {
            _experience -= target;
            _level++;
            target = TargetExperience;
            if (target <= 0)
            {
                _experience = 0f;
            }
            if (DrawChoices())
            {
                _input.ResetStick();
                return true;
            }
        }
        return false;
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
        int target = TargetExperience;
        _hud.Refresh(_level, target > 0 ? Mathf.Clamp01(_experience / target) : 1f, _health / MaxHealth, _silver, _gold,
            _hasAwakening, _hasUsedAwakening);
    }

    private void Finish(bool isVictory)
    {
        _isFinished = true;
        foreach (Attack attack in _attacks)
        {
            if (attack.Id is >= 7 and <= 18 && attack.Flight != null)
            {
                attack.Flight.EndFlight();
            }
        }
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
