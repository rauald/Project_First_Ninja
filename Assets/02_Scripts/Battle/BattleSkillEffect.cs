using UnityEngine;

public sealed class BattleSkillEffect : MonoBehaviour
{
    private enum MotionType { None = -1, Slash, Projectile, Orbit, Ground, Rise, Fall, Wave, Burst, Max }

    private static readonly MotionType[] _motions =
    {
        MotionType.Slash, MotionType.Projectile, MotionType.Projectile, MotionType.Slash, MotionType.Slash, MotionType.Projectile,
        MotionType.Projectile, MotionType.Orbit, MotionType.Projectile, MotionType.Orbit, MotionType.Ground, MotionType.Rise,
        MotionType.Fall, MotionType.Ground, MotionType.Fall, MotionType.Rise, MotionType.Orbit, MotionType.Wave,
        MotionType.Burst, MotionType.Fall, MotionType.Wave, MotionType.Rise, MotionType.Burst, MotionType.Orbit
    };

    private static readonly Color[] _colors =
    {
        new(.82f, .77f, 1f), new(1f, .36f, .09f), new(.23f, .72f, 1f),
        new(.48f, .95f, .22f), new(.89f, .57f, .25f), new(.49f, 1f, .87f)
    };

    private SpriteRenderer _view;
    private BattleSession _battle;
    private Transform _caster;
    private MotionType _motion;
    private Vector2 _start;
    private Vector2 _end;
    private Vector2 _direction;
    private Color _tint;
    private float _life;
    private float _elapsed;
    private float _radius;
    private float _height;
    private float _angle;
    private float _phase;
    private bool _isAccent;

    public static void Play(BattleSession battle, Transform parent, Transform caster, int skillId, Sprite sprite,
        Vector2 target, float radius, float duration, int count)
    {
        if (sprite == null)
        {
            return;
        }

        MotionType motion = _motions[skillId - 1];
        int element = skillId >= 19 ? skillId - 19 : skillId <= 6 ? skillId - 1 : (skillId - 7) / 2;
        float life = motion is MotionType.Ground or MotionType.Orbit
            ? Mathf.Max(.65f, duration)
            : skillId >= 19 ? 1.15f : .48f;
        int layers = motion is MotionType.Orbit or MotionType.Burst || skillId >= 19 ? 5 : 3;
        if (motion == MotionType.Projectile)
        {
            layers = Mathf.Clamp(count + 2, 3, 8);
        }
        for (int i = 0; i < layers; i++)
        {
            GameObject effect = new($"Skill effect {skillId}");
            effect.transform.SetParent(parent, false);
            var view = effect.AddComponent<BattleSkillEffect>();
            view.Initialize(battle, caster, sprite, target, motion, _colors[element], radius, life, i, layers);
        }
    }

    private void Initialize(BattleSession battle, Transform caster, Sprite sprite, Vector2 target,
        MotionType motion, Color tint, float radius, float life, int layer, int layers)
    {
        _battle = battle;
        _caster = caster;
        _start = caster.position;
        _end = target;
        _direction = (_end - _start).normalized;
        if (_direction == Vector2.zero)
        {
            _direction = Vector2.right;
        }
        _motion = motion;
        _tint = tint;
        _radius = radius;
        _life = life;
        _phase = (float)layer / layers;
        _isAccent = layer > 0;
        _height = (_isAccent ? .40f : 1f) / sprite.bounds.size.y;
        _angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        _view = gameObject.AddComponent<SpriteRenderer>();
        _view.sprite = sprite;
        _view.sortingOrder = _isAccent ? 8 : 9;
        Render(0f);
    }

    private void Update()
    {
        if (_battle.IsEffectPaused)
        {
            return;
        }
        _elapsed += Time.deltaTime;
        if (_elapsed >= _life)
        {
            Destroy(gameObject);
            return;
        }
        Render(_elapsed / _life);
    }

    private void Render(float progress)
    {
        float travel = Mathf.Clamp01((progress - _phase * .24f) * 1.5f);
        float swing = Mathf.SmoothStep(0f, 1f, travel);
        float spread = _phase * Mathf.PI * 2f;
        Vector2 normal = new(-_direction.y, _direction.x);
        Vector2 ring = new(Mathf.Cos(spread + _elapsed * 7f), Mathf.Sin(spread + _elapsed * 7f));
        Vector2 caster = _caster.position;
        Vector2 position;
        float angle = _angle;
        float scale = _height;
        switch (_motion)
        {
            case MotionType.Slash:
                angle += Mathf.Lerp(-68f, 68f, swing);
                Vector2 arc = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                position = _start + arc * Mathf.Min(_radius, 1.6f) * (.6f + _phase * .45f);
                scale *= .7f + swing;
                break;
            case MotionType.Projectile:
                position = Vector2.Lerp(_start, _end, travel) + normal * Mathf.Sin(spread) * .22f;
                break;
            case MotionType.Orbit:
                position = caster + ring * _radius * (.38f + _phase * .30f);
                angle = spread * Mathf.Rad2Deg + progress * 780f;
                break;
            case MotionType.Ground:
                position = _end + ring * _radius * _phase * .42f;
                scale *= Mathf.SmoothStep(.15f, _radius * 1.25f, Mathf.Clamp01(_elapsed * 3f - _phase * .2f));
                angle = spread * 15f;
                break;
            case MotionType.Rise:
                position = _end + normal * Mathf.Sin(spread) * _radius * .55f + Vector2.up * swing * .68f;
                scale *= Mathf.SmoothStep(.15f, 1.65f, travel);
                angle = 0f;
                break;
            case MotionType.Fall:
                position = _end + normal * Mathf.Sin(spread) * _radius * .65f + Vector2.up * (1f - swing) * 3f;
                angle = spread * 18f;
                break;
            case MotionType.Wave:
                position = _start + _direction * (travel * _radius) + normal * Mathf.Sin(spread) * _radius * .64f;
                scale *= .55f + swing * 1.55f;
                break;
            default:
                position = _start + ring * _radius * travel * _phase;
                scale *= .35f + swing * _radius;
                angle = 0f;
                break;
        }

        transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.Euler(0f, 0f, angle));
        transform.localScale = Vector3.one * scale;
        float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.62f, 1f, progress));
        Color color = _isAccent ? _tint : Color.white;
        color.a = alpha * (_isAccent ? .40f : 1f);
        _view.color = color;
    }
}
