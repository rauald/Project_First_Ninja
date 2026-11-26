using UnityEngine;

public sealed class BattleSkillEffect : MonoBehaviour, IPoolable
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
    private static Material _strokeMaterial;
    private LineRenderer[] _strokes;
    private int _basicId;
    private float _characterSize;
    private float _travelTime;
    private bool _isImpact;
    private bool _isFlightControlled;
    public const float PUDDLE_SQUASH = .47f;
    private static readonly Color[] _elementalColors =
    {
        new(.22f, .13f, .34f), new(.66f, .40f, 1f), new(1f, .29f, .13f), new(1f, .39f, .23f),
        new(.18f, .65f, .97f), new(.22f, .74f, 1f), new(.64f, .82f, .24f), new(.51f, .78f, .27f),
        new(.94f, .46f, .23f), new(.66f, .43f, .27f), new(.43f, .86f, .79f), new(.42f, .93f, .80f)
    };

    internal enum Channel { Basic, Elemental, Impact, Feedback }

    // 자신을 대여한 풀에만 반환한다.
    private ObjectPool<BattleSkillEffect> _pool;

    internal static BattleSkillEffect CreatePooled(Transform root, int id, Channel channel, BattleSession battle)
    {
        var effect = new GameObject($"Skill_{id}_{channel}").AddComponent<BattleSkillEffect>();
        effect.transform.SetParent(root, false);
        effect._basicId = channel == Channel.Feedback ? 0 : id;
        if (channel == Channel.Feedback)
            effect._view = effect.gameObject.AddComponent<SpriteRenderer>();
        else if (channel == Channel.Basic)
            effect.EnsureStrokes(id is 1 or 4 ? 8 : 9);
        else if (channel == Channel.Elemental && battle.GetElementalSprite(id - 7) != null)
            effect.CreateElementalLayers(id == 8 ? 4 : id == 10 ? 1 : id == 14 ? 13 : id is 15 or 17 ? 7 : 1);
        else if (channel == Channel.Impact && id == 13 && battle.GetElementalSprite(6) != null)
            effect.CreateElementalLayers(7);
        else
        {
            effect.EnsureStrokes(channel == Channel.Impact ? 7 : id == 10 ? 5 : 12);
            if (channel == Channel.Elemental)
                effect.CreateInkMesh();
        }
        return effect;
    }

    // 이전 타격 시간과 표시 상태는 반납 때 초기화한다.
    public void OnRent() { }

    public void OnReturn()
    {
        _pool = null;
        _elapsed = 0f;
        _life = 0f;
        _isImpact = _isFlightControlled = false;
        _battle = null;
        _caster = null;
        if (_view != null) _view.sprite = null;
        if (_ink != null) _ink.Clear();
        if (_strokes != null)
            foreach (var stroke in _strokes) stroke.positionCount = 0;
        if (_elementalLayers != null)
            foreach (var layer in _elementalLayers) layer.sprite = null;
    }

    private static BattleSkillEffect Rent(BattleSession battle, int id, Channel channel)
    {
        var pool = battle.GetSkillPool(id, channel);
        var effect = pool.Rent(Vector3.zero, Quaternion.identity);
        if (effect == null)
            return null;

        effect._pool = pool;
        effect._battle = battle;
        effect.enabled = true;
        return effect;
    }

    public static float GetSweepHalfAngle(int skillId) => skillId == 4 ? 68f : 60f;

    public static BattleSkillEffect PlayBasic(BattleSession battle, int id, Vector2 start,
        Vector2 end, Vector2 direction, float characterSize, float radius, float travelTime)
    {
        var effect = CreateStrokes(battle, id, start, direction, characterSize, id is 1 or 4 ? 8 : 9);
        if (effect == null) return null;
        effect._end = end;
        effect._radius = radius;
        effect._travelTime = travelTime;
        effect._life = travelTime + .15f;
        effect._isFlightControlled = id is 2 or 3 or 6;
        if (effect._isFlightControlled)
        {
            effect._end = start;
        }
        effect.RenderBasic();
        return effect;
    }

    public void SetFlightPose(Vector2 position, Vector2 direction)
    {
        _end = position;
        _direction = direction;
        RenderBasic();
    }

    public void EndFlight()
    {
        _pool?.Return(this);
    }

    public static BattleSkillEffect PlayElemental(BattleSession battle, int id,
        Vector2 start, Vector2 direction, int count)
    {
        if (battle != null && battle.GetElementalSprite(id - 7) != null)
        {
            var spriteEffect = Rent(battle, id, Channel.Elemental);
            if (spriteEffect == null) return null;
            spriteEffect._battle = battle;
            spriteEffect._basicId = id;
            spriteEffect.CreateElementalLayers(id == 8 ? 4 : id == 10 ? Mathf.Max(1, count) :
                id == 14 ? 13 : id is 15 or 17 ? 7 : 1);
            spriteEffect.enabled = false;
            return spriteEffect;
        }
        var effect = CreateStrokes(battle, id, start, direction, 1f, id == 10 ? count * 3 + 2 : 12, Channel.Elemental);
        if (effect == null) return null;
        if (effect._ink == null) effect.CreateInkMesh();
        effect.enabled = false;
        return effect;
    }

    private const float FIRE_SHURIKEN_ORBIT_RADIUS_SCALE = .65f * 3f;

    public static Vector2 GetOrbitOffset(float age, float radius, int index, int count)
    {
        return Unit(age * 4.8f + index * Mathf.PI * 2f / count) * (radius * FIRE_SHURIKEN_ORBIT_RADIUS_SCALE);
    }

    public static Vector2 GetPuddleCenter(Vector2 center) => center + Vector2.down * .36f;

    public void SetElementalPose(Vector2 start, Vector2 center, Vector2 direction, float radius,
        float age, float life, int count)
    {
        if (_elementalLayers != null)
        {
            RenderElementalSprites(start, center, direction, radius, age, life, count);
            return;
        }
        float t = Mathf.Clamp01(age / life);
        float fade = Mathf.Clamp01((1f - t) * 5f);
        Color bright = Color.Lerp(_tint, Color.white, .68f);
        switch (_basicId)
        {
            case 7:
            case 9:
                RenderElementalProjectile(center, direction, radius, age, bright);
                break;
            case 8:
                Stroke(0, radius * 1.15f, radius * 1.15f, new Color(.075f, .045f, .12f), fade,
                    center - Vector2.right * radius * .16f, center + Vector2.right * radius * .16f);
                for (int i = 1; i <= 7; i++)
                {
                    DrawArc(i, center, radius * (.24f + i * .095f), .78f, age * 5f + i * .8f,
                        4.3f, .11f, i % 3 == 0 ? bright : _tint, fade * (1f - i * .055f));
                }
                DrawPulse(8, center, radius, age, .2f, fade);
                break;
            case 10:
                DrawArc(0, center, radius * FIRE_SHURIKEN_ORBIT_RADIUS_SCALE, 1f, 0f, Mathf.PI * 2f, .042f, _tint, fade * .55f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 blade = center + GetOrbitOffset(age, radius, i, count);
                    int layer = 1 + i * 3;
                    DrawTeeth(layer, blade, radius * .25f, 4, .37f, age * 24f + i, .13f, bright, fade);
                    DrawTeeth(layer + 1, blade, radius * .19f, 4, .47f, age * 24f + i, .17f, _tint, fade);
                    DrawArc(layer + 2, blade, radius * .32f, 1f, age * 8f, 4.5f, .06f, _tint, fade * .8f);
                }
                DrawPulse(count * 3 + 1, center, radius, age, .4f, fade * .45f);
                break;
            case 11:
                center = GetPuddleCenter(center);
                Stroke(0, radius * .49f, radius * .49f, new Color(.10f, .36f, .68f), .49f,
                    center - Vector2.right * radius * .56f, center + Vector2.right * radius * .56f);
                for (int i = 1; i <= 5; i++)
                {
                    DrawArc(i, center, radius * (.23f + i * .11f), .43f, age * .85f + i,
                        4.4f, .075f, i % 2 == 0 ? bright : _tint, .48f);
                }
                DrawPulse(6, center, radius, age, .4f, .55f, PUDDLE_SQUASH);
                break;
            case 12:
            case 16:
                RenderColumn(center, radius, t, _basicId == 16, bright);
                break;
            case 13:
                Vector2 fruit = Vector2.Lerp(start, center, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 1.55f);
                Stroke(0, .43f, .40f, new Color(.63f, .24f, .13f), 1f, fruit - Vector2.right * .06f, fruit);
                Stroke(1, .24f, .17f, _tint, 1f, fruit, fruit + Unit(age * 18f) * .14f);
                Stroke(2, .08f, .025f, bright, 1f, fruit + Unit(age * 18f) * .15f, fruit + Unit(age * 18f) * .32f);
                DrawArc(3, fruit, .27f, 1f, -age * 13f, 4.7f, .04f, bright, .8f);
                break;
            case 14:
                for (int i = 0; i < 9; i++)
                {
                    Vector2 mote = center + Unit(age * .9f + i * 2.4f) * (radius * (.16f + i * .087f));
                    Stroke(i, radius * .39f, radius * .27f, i % 3 == 0 ? bright : _tint,
                        fade * .26f, mote - Vector2.right * .13f, mote + Vector2.right * .13f);
                }
                DrawPulse(9, center, radius, age, .4f, fade);
                break;
            case 15:
                RenderMeteor(center, radius, age, bright);
                break;
            case 17:
                DrawArc(0, center, radius * .94f, .92f, 0f, Mathf.PI * 2f, .05f, _tint, fade * .7f);
                DrawTeeth(1, center, radius * .8f, 13, .81f, age * 16f, .15f, bright, fade);
                DrawTeeth(2, center, radius * .69f, 13, .83f, age * 16f, .32f, _tint, fade * .8f);
                DrawArc(3, center, radius * .24f, 1f, age * 16f, 5.1f, .17f, bright, fade);
                DrawPulse(4, center, radius, age, .4f, fade);
                break;
            case 18:
                for (int i = 0; i < 8; i++)
                {
                    DrawArc(i, center, radius * (.18f + .81f * t), .93f,
                        i * Mathf.PI * .25f + age * 5.4f, .66f + .15f * t, .13f,
                        i % 2 == 0 ? bright : _tint, 1f - t);
                }
                break;
        }
        RenderBrushwork(start, center, direction, radius, age, t, count);
    }

    private SpriteRenderer[] _elementalLayers;

    private void CreateElementalLayers(int count)
    {
        int existing = _elementalLayers?.Length ?? 0;
        if (count <= existing) return;
        System.Array.Resize(ref _elementalLayers, count);
        for (int i = existing; i < count; i++)
        {
            var layer = new GameObject("Sprite");
            layer.transform.SetParent(transform, false);
            _elementalLayers[i] = layer.AddComponent<SpriteRenderer>();
            _elementalLayers[i].sortingOrder = _basicId == 11 ? -2 : 8 + i;
        }
    }

    private void PlaceSprite(int layer, int slot, Vector2 position, float size,
        float angle = 0f, float alpha = 1f, float squash = 1f, int frame = 0)
    {
        SpriteRenderer view = _elementalLayers[layer];
        Sprite sprite = _battle.GetElementalSprite(slot, frame);
        view.sprite = sprite;
        if (sprite == null) return;
        view.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        float scale = size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        view.transform.localScale = new Vector3(scale, scale * squash, 1f);
        view.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private void RenderElementalSprites(Vector2 start, Vector2 center, Vector2 direction, float radius,
        float age, float life, int count)
    {
        int slot = _basicId - 7;
        float t = Mathf.Clamp01(age / life);
        float fade = Mathf.Clamp01((1f - t) * 6f);
        float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        switch (_basicId)
        {
            case 7:
                PlaceSprite(0, slot, center, radius * 1.3f, facing);
                break;
            case 8:
                for (int i = 0; i < 3; i++)
                {
                    float phase = Mathf.Repeat(age * 1.55f + i / 3f, 1f);
                    PlaceSprite(i, slot, center, radius * Mathf.Lerp(3.1f, 1f, phase), 0f,
                        .42f * fade * Mathf.Sin(phase * Mathf.PI));
                }
                PlaceSprite(3, slot, center, radius * 2f, 0f, fade);
                break;
            case 9:
                PlaceSprite(0, slot, center, radius * (1.54f + .05f * Mathf.Sin(age * 29f)), facing + 151f);
                break;
            case 10:
                for (int i = 0; i < _elementalLayers.Length; i++)
                    PlaceSprite(i, slot, center + GetOrbitOffset(age, radius, i, count),
                        radius * .57f, -age * 900f, fade);
                break;
            case 11:
                PlaceSprite(0, slot, GetPuddleCenter(center), radius * 2f, 0f, .9f, PUDDLE_SQUASH);
                break;
            case 12:
                int frame = Mathf.Min(7, Mathf.FloorToInt(t * 8f));
                PlaceSprite(0, slot, center + Vector2.up * radius * .68f, radius * 2.35f,
                    0f, fade, 1f, frame);
                break;
            case 13:
                PlaceSprite(0, slot, Vector2.Lerp(start, center, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 1.1f),
                    radius * .66f, -t * 165f);
                break;
            case 14:
                for (int i = 0; i < 12; i++)
                {
                    float phase = Mathf.Repeat(age * .63f + i / 12f, 1f);
                    Vector2 offset = Unit(i * 2.39996f + phase * .4f) * (radius * .77f * phase);
                    PlaceSprite(i, 12, center + offset, radius * (.45f + phase * 1.25f), i * 29f,
                        fade * Mathf.Sin(phase * Mathf.PI), 1f, i % 7);
                }
                PlaceSprite(12, slot, center, radius * .8f, 0f, fade);
                break;
            case 15:
                float fall = Mathf.Clamp01(age / .43f);
                if (fall < 1f)
                {
                    Vector2 head = center + Vector2.up * (4.2f * (1f - fall * fall));
                    for (int i = 0; i < 6; i++)
                    {
                        float phase = Mathf.Repeat(age * 5f + i / 6f, 1f);
                        PlaceSprite(i, 12, head + new Vector2((i - 2.5f) * radius * .08f, phase * radius * 1.35f),
                            radius * (.18f + phase * .42f), 0f, fall * (1f - phase), 2.2f, i);
                        _elementalLayers[i].color = new Color(.92f, .72f, .56f, fall * (1f - phase));
                    }
                    PlaceSprite(6, slot, head + Vector2.up * radius * .38f, radius * .9f);
                }
                else
                {
                    RenderFragments(slot, center, radius, (age - .43f) / .22f);
                }
                break;
            case 16:
                float rise = Mathf.Clamp01(t * 4f);
                int step = Mathf.Clamp(Mathf.CeilToInt(rise * 12f), 1, 12);
                Sprite visible = _battle.GetElementalSprite(slot, step);
                Sprite full = _battle.GetElementalSprite(slot);
                float pillarScale = radius * 1.25f / Mathf.Max(full.bounds.size.x, full.bounds.size.y);
                float height = visible.bounds.size.y * pillarScale;
                PlaceSprite(0, slot, center + Vector2.up * (height * .5f),
                    Mathf.Max(visible.bounds.size.x, visible.bounds.size.y) * pillarScale, 0f, fade * rise, 1f, step);
                break;
            case 17:
                for (int i = 0; i < 6; i++)
                {
                    float disperse = 1f - fade;
                    PlaceSprite(i, 11, center + Unit(i * Mathf.PI / 3f + age * 6f) * (radius * disperse),
                        radius * .6f, -age * 480f + i * 60f, Mathf.Sin(disperse * Mathf.PI) * .55f);
                }
                PlaceSprite(6, slot, center, radius * (1.55f + Mathf.Clamp01(age * 8f) * .45f),
                    -age * 720f, fade);
                break;
            case 18:
                PlaceSprite(0, slot, center, radius * Mathf.Lerp(.28f, 2f, t), -t * 265f, 1f - t);
                break;
        }
    }

    private void RenderFragments(int slot, Vector2 center, float radius, float t)
    {
        for (int i = 0; i < _elementalLayers.Length; i++)
        {
            float travel = Mathf.Sqrt(Mathf.Clamp01(t));
            Vector2 offset = Unit(i * 2.39996f) * (radius * travel * (.38f + i * .05f));
            PlaceSprite(i, slot, center + offset + Vector2.up * (radius * Mathf.Sin(travel * Mathf.PI) * .3f),
                radius * (.23f + i % 3 * .055f), (i - 3) * t * 110f, 1f - t, 1f, i + 1);
        }
    }

    // Filled brush strokes share one mesh so that even a many-bladed orbit uses one accent renderer.
    private Mesh _ink;
    private readonly System.Collections.Generic.List<Vector3> _vertices = new();
    private readonly System.Collections.Generic.List<Color32> _pigment = new();
    private readonly System.Collections.Generic.List<int> _indices = new();

    private void CreateInkMesh()
    {
        var layer = new GameObject("Brushwork");
        layer.transform.SetParent(transform, false);
        _ink = new Mesh { name = "Elemental brushwork" };
        _ink.MarkDynamic();
        layer.AddComponent<MeshFilter>().sharedMesh = _ink;
        var renderer = layer.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _strokeMaterial;
        renderer.sortingOrder = _basicId == 11 ? -1 : 12 + _strokes.Length;
    }

    private void OnDestroy()
    {
        if (_ink != null) Destroy(_ink);
    }

    private void Brush(Vector2 a, Vector2 b, Vector2 c, float width, Color body, Color tip)
    {
        const int steps = 12;
        for (int j = 0; j <= steps; j++)
        {
            float u = j / (float)steps;
            Vector2 tangent = Vector2.Lerp(b - a, c - b, u).normalized;
            Vector2 p = Vector2.Lerp(Vector2.Lerp(a, b, u), Vector2.Lerp(b, c, u), u);
            Vector2 n = new Vector2(-tangent.y, tangent.x) * (width * Mathf.Sin(Mathf.PI * u) * (1.35f - u));
            int k = _vertices.Count;
            Vector2 local = transform.InverseTransformPoint(p);
            Vector2 offset = transform.InverseTransformVector(n);
            _vertices.Add(local + offset);
            _vertices.Add(local - offset);
            Color32 color = Color.Lerp(body, tip, u);
            _pigment.Add(color);
            _pigment.Add(color);
            if (j == 0) continue;
            _indices.Add(k - 2); _indices.Add(k); _indices.Add(k - 1);
            _indices.Add(k - 1); _indices.Add(k); _indices.Add(k + 1);
        }
    }

    private void RenderBrushwork(Vector2 start, Vector2 center, Vector2 dir, float r, float age, float t, int count)
    {
        _vertices.Clear();
        _indices.Clear();
        _pigment.Clear();
        Vector2 side = new(-dir.y, dir.x);
        Color ink = Color.Lerp(_tint, new Color(.035f,.018f,.09f), .67f);
        Color light = Color.Lerp(_tint, Color.white, .82f);
        Color flare = _basicId is 9 or 10 or 15 ? new Color(1f,.76f,.19f) : _tint;
        float fade = _basicId == 11 ? 1f : Mathf.Clamp01((1f-t)*7f);
        ink.a = light.a = flare.a = fade;
        int marks = _basicId == 10 ? count * 4 : 8;
        for (int i = 0; i < marks; i++)
        {
            float angle = i * Mathf.PI * .25f;
            float phase = Mathf.Repeat(age * 1.9f + i * .381966f, 1f);
            Vector2 ray = Unit(angle);
            Vector2 cross = new(-ray.y, ray.x);
            Vector2 a, b, c;
            float width;
            switch (_basicId)
            {
                case 7:
                case 9:
                    float lane = (i - 3.5f) / 4f;
                    a = center + side * (lane * r * .48f) - dir * r * (.8f + .4f * phase);
                    b = center + side * r * (lane * .34f + .08f * Mathf.Sin(age*22f+i));
                    c = center + dir*r*(.38f-.34f*lane*lane) + side*r*lane*.15f;
                    width = r * (_basicId == 7 ? .085f : .105f);
                    break;
                case 10:
                    Vector2 hub = center + GetOrbitOffset(age,r,i/4,count);
                    ray = Unit(age*24f + i*Mathf.PI*.5f + i/4);
                    cross = new(-ray.y,ray.x);
                    a=hub+cross*r*.025f; b=hub+ray*r*.27f; c=b+cross*r*.18f;
                    width=r*.066f;
                    break;
                case 12:
                case 16:
                    float rise = Mathf.Clamp01(t*3.2f);
                    float x = (i-3.5f)*.105f;
                    a=center+Vector2.right*x*r*.7f;
                    b=a+new Vector2(x*.8f,.46f)*r*rise;
                    c=center+new Vector2(x*(_basicId==12?1.7f:1f),(.65f+.43f*Mathf.Cos(x*4f)))*r*rise;
                    width=r*(_basicId==12?.085f:.13f)*rise;
                    break;
                case 13:
                    Vector2 fruit=Vector2.Lerp(start,center,t)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*1.55f);
                    ray=Unit(angle+age*11f);
                    a=fruit; b=fruit+ray*.28f; c=b+new Vector2(-ray.y,ray.x)*(.15f+t*.14f);
                    width=.064f;
                    break;
                case 15:
                    float fall=Mathf.Clamp01(age/.43f);
                    Vector2 head=center+new Vector2(.45f,4.2f)*(1f-fall*fall);
                    a=head+ray*r*.08f;
                    b=head+ray*r*.36f;
                    c=fall<1f ? b+Vector2.up*r*(.55f+phase*.47f) : head+ray*r*(.55f+(age-.43f)*2f);
                    width=r*.11f;
                    break;
                default:
                    float spin = _basicId == 8 ? -age*3f : _basicId == 17 ? age*16f : age*1.6f;
                    float scale = _basicId == 18 ? .18f+.82f*t : 1f;
                    float squash = _basicId == 11 ? PUDDLE_SQUASH : _basicId==14?.72f:1f;
                    Vector2 shape=new(r*scale,r*scale*squash);
                    float turn=angle+spin;
                    float inner=_basicId==17?.32f:_basicId==8?.18f:.38f;
                    a=center+Vector2.Scale(Unit(turn)*inner,shape);
                    b=center+Vector2.Scale(Unit(turn+.86f)*1.12f,shape);
                    c=center+Vector2.Scale(Unit(turn+(_basicId==17?.72f:1.85f))*(.88f+.09f*phase),shape);
                    width=r*scale*(_basicId==14?.14f:_basicId==11?.035f:.078f);
                    break;
            }
            Brush(a,b,c,width,ink,flare);
            Brush(Vector2.Lerp(a,b,.22f),b,c,width*.28f,flare,light);
        }
        _ink.Clear();
        _ink.SetVertices(_vertices);
        _ink.SetColors(_pigment);
        _ink.SetTriangles(_indices,0);
    }
    public static void PlayImpact(BattleSession battle, int id, Vector2 center,
        Vector2 direction, float characterSize)
    {
        if (id == 13 && battle.GetElementalSprite(6) != null)
        {
            var burst = Rent(battle, id, Channel.Impact);
            if (burst == null) return;
            burst._battle = battle;
            burst._basicId = id;
            burst._start = center;
            burst._characterSize = characterSize;
            burst._isImpact = true;
            burst._life = .28f;
            burst.CreateElementalLayers(7);
            burst.RenderFragments(6, center, characterSize, 0f);
            return;
        }
        var effect = CreateStrokes(battle, id, center, direction, characterSize, 7, Channel.Impact);
        if (effect == null) return;
        effect._isImpact = true;
        effect._life = .24f;
        effect.RenderImpact();
    }

    private static BattleSkillEffect CreateStrokes(BattleSession battle, int id,
        Vector2 origin, Vector2 direction, float characterSize, int count, Channel channel = Channel.Basic)
    {
        var view = Rent(battle, id, channel);
        if (view == null)
            return null;

        view._start = origin;
        view._direction = direction;
        view._characterSize = characterSize;
        view._tint = id <= 6 ? _colors[id - 1] : _elementalColors[id - 7];
        view.EnsureStrokes(count);
        return view;
    }

    // 처음 준비한 선을 재사용하고 스킬 단계가 요구하는 수량만 추가한다.
    private void EnsureStrokes(int count)
    {
        int existing = _strokes?.Length ?? 0;
        if (count <= existing) return;
        if (_strokeMaterial == null)
            _strokeMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };

        System.Array.Resize(ref _strokes, count);
        for (int i = existing; i < count; i++)
        {
            var layer = new GameObject("Stroke");
            layer.transform.SetParent(transform, false);
            var stroke = layer.AddComponent<LineRenderer>();
            stroke.sharedMaterial = _strokeMaterial;
            stroke.sortingOrder = _basicId == 11 ? -2 : 8 + i;
            stroke.positionCount = 0;
            stroke.numCapVertices = 3;
            stroke.numCornerVertices = 2;
            _strokes[i] = stroke;
        }
    }

    private void RenderElementalProjectile(Vector2 center, Vector2 direction, float radius, float age, Color bright)
    {
        Vector2 normal = new(-direction.y, direction.x);
        if (_basicId == 7)
        {
            for (int i = 0; i < 4; i++)
            {
                int layer = i;
                float width = radius * (.46f - i * .04f);
                Vector2 basePoint = center - direction * (.14f * i);
                BeginStroke(layer, 23, .23f - i * .04f, .15f - i * .02f,
                    i == 0 ? new Color(.055f, .035f, .075f) : _tint, 1f - i * .19f);
                for (int j = 0; j <= 22; j++)
                {
                    float angle = Mathf.Lerp(-Mathf.PI * .5f, Mathf.PI * .5f, j / 22f);
                    _strokes[layer].SetPosition(j, basePoint + width * (normal * Mathf.Sin(angle)
                        + direction * (.55f * Mathf.Cos(angle))));
                }
            }
            Stroke(4, .12f, .02f, bright, .65f, center + direction * radius * .28f, center + direction * radius * .45f);
            return;
        }
        float size = radius * .25f;
        Stroke(0, size, size * .94f, _tint, 1f, center - direction * size * .38f, center);
        Stroke(1, size * .69f, size * .58f, new Color(1f, .68f, .22f), 1f,
            center - direction * size * .20f, center + direction * size * .12f);
        Stroke(2, size * .36f, size * .28f, new Color(1f, .92f, .58f), 1f, center, center + direction * size * .21f);
        for (int i = 3; i < 11; i++)
        {
            float phase = Mathf.Repeat(age * 5.5f + i * .138f, 1f);
            Vector2 ember = center - direction * size * (1f + phase * 2.8f)
                + normal * Mathf.Sin(i * 3f + age * 20f) * (size * .53f * phase);
            Stroke(i, size * .34f, .012f, i % 2 == 0 ? bright : _tint,
                1f - phase, ember, ember - direction * size * (.22f + phase));
        }
    }

    private void RenderColumn(Vector2 center, float radius, float t, bool stone, Color bright)
    {
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 3.2f));
        float fade = Mathf.Clamp01((1f - t) * 3f);
        float height = Mathf.Max(.3f, radius) * (stone ? .86f : 1.28f) * rise;
        DrawArc(0, center, radius * (.28f + .25f * t), .44f, 0f, Mathf.PI * 2f, .11f, _tint, fade);
        Color core = stone ? new Color(.36f, .22f, .17f) : new Color(.15f, .47f, .92f);
        Stroke(1, radius * .55f, radius * .28f, core, fade, center, center + Vector2.up * height);
        for (int i = 2; i < 8; i++)
        {
            float x = (i - 4.5f) * radius * .085f;
            Vector2 basePoint = center + Vector2.right * x;
            Vector2 top = basePoint + new Vector2(-x * .35f, height * (.65f + i * .046f));
            Stroke(i, .12f, .036f, i % 2 == 0 ? bright : _tint, fade,
                basePoint, Vector2.Lerp(basePoint, top, .45f) + Vector2.right * (stone ? .08f : .14f), top);
        }
    }

    private void RenderMeteor(Vector2 center, float radius, float age, Color bright)
    {
        float fall = Mathf.Clamp01(age / .43f);
        if (fall < 1f)
        {
            Vector2 head = center + new Vector2(.45f, 4.2f) * (1f - fall * fall);
            Stroke(0, radius * .48f, radius * .38f, new Color(.39f, .21f, .17f), 1f,
                head, head + Vector2.up * .31f);
            for (int i = 1; i <= 6; i++)
            {
                Vector2 spark = head + new Vector2(Mathf.Sin(i * 3.2f) * .39f, .27f + i * .24f);
                Stroke(i, .17f, .03f, i % 2 == 0 ? bright : _tint, 1f - i * .105f,
                    spark, spark + Vector2.up * .36f);
            }
        }
        else
        {
            float burst = (age - .43f) / .22f;
            for (int i = 0; i <= 6; i++)
            {
                Vector2 ray = Unit(i * Mathf.PI * 2f / 7f);
                Vector2 spark = center + ray * radius * (.16f + burst * .55f);
                Stroke(i, .19f, .035f, i % 2 == 0 ? bright : _tint, 1f - burst,
                    spark, spark + ray * .29f);
            }
        }
        DrawArc(7, center, radius * (.20f + fall * .29f), .46f, 0f,
            Mathf.PI * 2f, .074f, _tint, .8f);
    }

    private void DrawPulse(int index, Vector2 center, float radius, float age, float interval, float alpha,
        float squash = 1f)
    {
        float phase = Mathf.Repeat(age, interval) / interval;
        DrawArc(index, center, radius * (.58f + phase * .42f), squash, 0f, Mathf.PI * 2f,
            .08f, Color.Lerp(_tint, Color.white, .45f), alpha * (1f - phase));
    }

    private void DrawArc(int index, Vector2 center, float radius, float squash, float start, float span,
        float width, Color color, float alpha)
    {
        BeginStroke(index, 37, width, width, color, alpha);
        for (int i = 0; i <= 36; i++)
        {
            Vector2 offset = Unit(start + span * i / 36f) * radius;
            offset.y *= squash;
            _strokes[index].SetPosition(i, center + offset);
        }
    }

    private void DrawTeeth(int index, Vector2 center, float radius, int teeth, float inner, float rotation,
        float width, Color color, float alpha)
    {
        int segments = teeth * 4;
        BeginStroke(index, segments + 1, width, width, color, alpha);
        for (int i = 0; i <= segments; i++)
        {
            float scale = i % 4 is 1 or 2 ? 1f : inner;
            _strokes[index].SetPosition(i, center + Unit(rotation + i * Mathf.PI * 2f / segments) * (radius * scale));
        }
    }

    public static void Play(BattleSession battle, Transform caster, int skillId, Sprite sprite,
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
            var view = Rent(battle, skillId, Channel.Feedback);
            if (view == null) return;
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
        if (!_isFlightControlled && _elapsed >= _life)
        {
            EndFlight();
            return;
        }
        if (_isImpact)
        {
            if (_elementalLayers != null)
                RenderFragments(6, _start, _characterSize, _elapsed / _life);
            else
                RenderImpact();
        }
        else if (_basicId != 0)
        {
            RenderBasic();
        }
        else
        {
            Render(_elapsed / _life);
        }
    }

    private void RenderBasic()
    {
        float progress = Mathf.Clamp01(_elapsed / _travelTime);
        float fade = _isFlightControlled ? 1f : Mathf.Clamp01((_life - _elapsed) / (_life - _travelTime));
        Vector2 normal = new(-_direction.y, _direction.x);
        Vector2 head = _isFlightControlled ? _end : Vector2.Lerp(_start, _end, progress);
        Color bright = Color.Lerp(_tint, Color.white, .72f);
        if (_basicId is 1 or 4)
        {
            RenderSweep(progress, fade, bright);
            return;
        }
        if (_basicId == 6)
        {
            for (int i = 0; i < 6; i++)
            {
                float spin = _elapsed * 29f + i * Mathf.PI / 3f;
                BeginStroke(i, 13, .055f, .01f, i % 2 == 0 ? bright : _tint, fade);
                for (int j = 0; j <= 12; j++)
                {
                    float t = j / 12f;
                    _strokes[i].SetPosition(j, head + Unit(spin - t * 1.8f) * _characterSize * (.06f + t * .28f));
                }
            }
            Stroke(6, .13f, .04f, bright, fade, head, head - _direction * _characterSize * .08f);
        }
        else if (_basicId == 5)
        {
            float scale = _characterSize;
            Vector2 Point(float x, float y) => head + scale * (_direction * x + normal * y);
            Stroke(0, .7f, .7f, new Color(.44f, .24f, .13f), fade, Point(-.30f, 0f), Point(.10f, 0f));
            Stroke(1, .86f, .86f, _tint, fade, Point(-.19f, 0f), Point(.09f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float y = (i - 1.5f) * .22f;
                Stroke(2 + i, .21f, .16f, Color.Lerp(_tint, bright, .48f), fade,
                    Point(.02f, y), Point(.32f - Mathf.Abs(y) * .2f, y));
            }
            Stroke(6, .24f, .15f, bright, fade, Point(-.24f, -.36f), Point(.05f, -.46f), Point(.20f, -.26f));
        }
        else
        {
            float scale = _characterSize;
            Vector2 Point(float x, float y) => head + scale * (_direction * x + normal * y);
            if (_basicId == 2)
            {
                Stroke(0, .065f, .065f, _tint, fade, Point(-.48f, 0f), Point(-.19f, 0f));
                Stroke(1, .13f, 0f, bright, fade, Point(-.22f, 0f), head);
                Stroke(2, .045f, .045f, bright, fade, Point(-.44f, 0f), Point(-.49f, .07f),
                    Point(-.56f, 0f), Point(-.49f, -.07f), Point(-.44f, 0f));
            }
            else
            {
                Stroke(0, .09f, .035f, _tint, fade, Point(-.65f, 0f), head);
                Stroke(1, .04f, .015f, bright, fade, Point(-.49f, 0f), head);
                Stroke(2, .045f, .025f, bright, fade, Point(-.22f, .12f), head, Point(-.22f, -.12f));
            }
        }
        int firstTrail = _basicId is 5 or 6 ? 7 : 3;
        for (int i = firstTrail; i < _strokes.Length; i++)
        {
            float phase = Mathf.Repeat(_elapsed * 8f + i * .283f, 1f);
            float offset = Mathf.Sin(i * 2.4f) * (.11f + phase * .24f);
            float back = _basicId == 5 ? .35f : _basicId == 6 ? .25f : .36f;
            Vector2 spark = head + _characterSize * (normal * offset - _direction * (back + phase * .57f));
            Stroke(i, .035f, .012f, i % 2 == 0 ? _tint : bright, fade * (1f - phase),
                spark, spark - _direction * _characterSize * (.07f + phase * .13f));
        }
    }

    private void RenderSweep(float progress, float fade, Color bright)
    {
        bool whip = _basicId == 4;
        float angle = Mathf.Atan2(_direction.y, _direction.x);
        float sweep = _direction.x > 0f ? -1f : 1f;
        float extent = GetSweepHalfAngle(_basicId) * Mathf.Deg2Rad;
        float length = _radius;
        float headAngle = angle + sweep * extent * (2f * progress - 1f);
        for (int i = 0; i < 4; i++)
        {
            float span = Mathf.Min(progress * 2f * extent, .95f + .13f * i);
            float radius = length * (1f - .052f * i);
            BeginStroke(i, 19, .012f, .12f - .02f * i, i == 0 ? bright : _tint, fade * (1f - .19f * i));
            for (int j = 0; j <= 18; j++)
            {
                float t = j / 18f;
                _strokes[i].SetPosition(j, _start + Unit(headAngle - sweep * span * (1f - t)) * radius);
            }
        }
        Vector2 radial = Unit(headAngle);
        Vector2 hand = _start + radial * (_characterSize * .31f);
        if (whip)
        {
            for (int layer = 4; layer <= 5; layer++)
            {
                BeginStroke(layer, 19, layer == 4 ? .12f : .065f, .018f,
                    layer == 4 ? new Color(.34f, .20f, .11f) : _tint, fade);
                for (int j = 0; j <= 18; j++)
                {
                    float t = j / 18f;
                    float bend = Mathf.Sin(t * Mathf.PI) * (.56f - .29f * progress);
                    Vector2 point = _start + Unit(headAngle - sweep * bend)
                        * Mathf.Lerp(_characterSize * .31f, length, t);
                    _strokes[layer].SetPosition(j, point);
                }
            }
        }
        else
        {
            Vector2 guard = hand + radial * (_characterSize * .18f);
            Vector2 cross = new(-radial.y, radial.x);
            Stroke(4, .09f, .075f, new Color(.35f, .23f, .26f), fade, hand, guard);
            Stroke(5, .055f, .055f, new Color(.94f, .72f, .35f), fade,
                guard - cross * .13f * _characterSize, guard + cross * .13f * _characterSize);
            Stroke(6, .16f, 0f, bright, fade, guard, _start + radial * length);
        }
        Vector2 tip = _start + radial * length;
        Stroke(7, .055f, .012f, bright, fade, tip, tip + radial * _characterSize * .14f);
    }

    private void RenderImpact()
    {
        float t = _elapsed / _life;
        float alpha = 1f - t;
        float direction = Mathf.Atan2(_direction.y, _direction.x);
        float scale = _characterSize * (_basicId == 5 ? 1.25f : 1f);
        BeginStroke(0, 23, .05f, .04f, _tint, alpha);
        for (int j = 0; j <= 22; j++)
        {
            _strokes[0].SetPosition(j, _start + Unit(j * Mathf.PI / 11f) * scale * (.12f + t * .55f));
        }
        for (int i = 1; i < 7; i++)
        {
            Vector2 ray = Unit(direction + i * Mathf.PI / 3f + (_basicId == 6 ? t * 1.7f : 0f));
            Vector2 point = _start + ray * scale * (.09f + t * (.35f + i * .045f));
            Stroke(i, .055f, 0f, Color.Lerp(_tint, Color.white, i % 2 * .65f), alpha,
                point, point + ray * scale * (.05f + alpha * .13f));
        }
    }

    private static Vector2 Unit(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));

    private void BeginStroke(int index, int count, float startWidth, float endWidth, Color tint, float alpha)
    {
        LineRenderer stroke = _strokes[index];
        stroke.positionCount = count;
        stroke.startWidth = _characterSize * startWidth;
        stroke.endWidth = _characterSize * endWidth;
        tint.a = alpha;
        stroke.startColor = tint;
        stroke.endColor = tint;
    }

    private void Stroke(int index, float startWidth, float endWidth, Color tint, float alpha,
        Vector2 a, Vector2 b, Vector2? c = null, Vector2? d = null, Vector2? e = null)
    {
        BeginStroke(index, e.HasValue ? 5 : d.HasValue ? 4 : c.HasValue ? 3 : 2, startWidth, endWidth, tint, alpha);
        LineRenderer stroke = _strokes[index];
        stroke.SetPosition(0, a);
        stroke.SetPosition(1, b);
        if (c.HasValue) stroke.SetPosition(2, c.Value);
        if (d.HasValue) stroke.SetPosition(3, d.Value);
        if (e.HasValue) stroke.SetPosition(4, e.Value);
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
