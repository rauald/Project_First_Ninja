using System.Collections.Generic;
using UnityEngine;

// 넝쿨과 가시의 끝점을 전투 시계에 맞춰 움직이고 폭발 파편까지 같은 풀 객체에서 그린다.
internal sealed class BattleThornBloomEffect : MonoBehaviour
{
    internal const float BURST_TIME = 4.34f;
    private const int RAYS = 18;
    private const int SEGMENTS = 42;
    private static readonly Color _bark = new(.10f, .24f, .055f);
    private static readonly Color _vine = new(.47f, .79f, .14f);
    private static readonly Color _tip = new(.77f, .94f, .29f);
    private static readonly Color _amber = new(1f, .65f, .08f);
    private static readonly Color _whiteGold = new(1f, .97f, .65f);

    // 두 메시를 사용해 바닥 넝쿨은 캐릭터 뒤에, 가시와 열매는 앞에 보이게 한다.
    private Geometry _ground;
    private Geometry _canopy;
    private Material _material;
    private ParticleSystem _splinters;
    private bool _spawnSplinters;
    private float _lastAge;
    private Vector2 _mapCenter;
    private float _half;

    internal void Initialize(BattleMap map, int order)
    {
        _mapCenter = map.transform.position;
        _half = map.HalfExtent;
        _material = new Material(Shader.Find("Sprites/Default")) { mainTexture = Texture2D.whiteTexture };
        _ground = new Geometry(transform, "Creeping_thorn_vines", _material, -15);
        _canopy = new Geometry(transform, "Thorn_columns_and_fruit", _material, order);

        // 작은 잎과 씨앗은 한 시스템이 월드 좌표에서 한꺼번에 방출한다.
        var emitter = new GameObject("Fruit_splinters");
        emitter.SetActive(false);
        emitter.transform.SetParent(transform, false);
        _splinters = emitter.AddComponent<ParticleSystem>();
        var main = _splinters.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 480;
        main.gravityModifier = .4f;
        var emission = _splinters.emission;
        emission.enabled = false;
        var renderer = emitter.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _material;
        renderer.sortingOrder = order + 1;
        emitter.SetActive(true);
    }

    internal void BeginCast()
    {
        _lastAge = 0f;
        _splinters.Clear(false);
    }

    internal void Render(float age, Vector2 caster)
    {
        _spawnSplinters = age >= BURST_TIME && _lastAge < BURST_TIME;
        _ground.Clear();
        _canopy.Clear();
        float dissolve = Ramp((age - BURST_TIME) / .59f);
        for (int ray = 0; ray < RAYS; ray++)
        {
            float angle = ray * (Mathf.PI * 2f / RAYS);
            Vector2 direction = Direction(angle);
            float range = MapReach(caster, direction);
            float growth = Ramp((age - .04f * (ray % 5)) / 1.23f);
            DrawVine(ray, caster, direction, range, growth * (1f - dissolve), 1f - dissolve);

            // 가시는 바닥 넝쿨의 실제 위치와 연결한다.
            for (int node = 0; node < 4; node++)
            {
                int seed = ray * 4 + node;
                float at = .21f + node * .218f + .036f * Noise(seed);
                Vector2 anchor = VinePoint(caster, direction, range, ray, at);
                DrawThorn(age, seed, anchor, dissolve);
            }
        }
        _ground.Upload();
        _canopy.Upload();
        if (age >= BURST_TIME)
            _splinters.Simulate(age - Mathf.Max(_lastAge, BURST_TIME), false, false, false);
        _lastAge = age;
    }

    // 맵 경계까지만 뻗고, 넝쿨 끝과 잎은 시간에 따라 그 위치에서 자란다.
    private void DrawVine(int ray, Vector2 caster, Vector2 direction, float range, float growth, float opacity)
    {
        if (growth <= 0f) return;
        int pieces = Mathf.CeilToInt(growth * SEGMENTS);
        Vector2 previous = caster;
        for (int i = 1; i <= pieces; i++)
        {
            float t = Mathf.Min((float)i / SEGMENTS, growth);
            Vector2 point = VinePoint(caster, direction, range, ray, t);
            float thick = Mathf.Lerp(.24f, .075f, t) * Mathf.Lerp(.55f, 1f, Mathf.Clamp01((growth - t) * 12f));
            _ground.Stroke(previous, point, thick, Fade(_bark, opacity));
            Vector2 highlight = Vector2.up * thick * .13f;
            _ground.Stroke(previous + highlight, point + highlight, thick * .56f, Fade(_vine, opacity));

            if (i % 3 == 0)
            {
                Vector2 along = (point - previous).normalized;
                Vector2 side = new Vector2(-along.y, along.x) * (i % 2 == 0 ? 1f : -1f);
                float open = Ramp((growth - t) * 22f);
                Vector2 leaf = Vector2.Lerp(along, side, .8f).normalized * (.35f + .23f * Noise(i + ray * 11)) * open;
                _ground.Leaf(point, leaf, Fade(_vine, opacity), Fade(_tip, opacity));

                // 긴 줄기에서 뻗는 곁가지를 짧은 곡선으로 이어 그린다.
                if (i % 9 == 0)
                {
                    Vector2 elbow = ClampToMap(point + (along + side * .48f) * range * .061f * open);
                    Vector2 tip = ClampToMap(elbow + (along * .25f + side * .77f) * range * .064f * open);
                    _ground.Stroke(point, elbow, .12f, Fade(_bark, opacity));
                    _ground.Stroke(elbow, tip, .08f, Fade(_vine, opacity));
                    _ground.Leaf(tip, side * .24f * open, Fade(_vine, opacity), Fade(_tip, opacity));
                }
            }
            previous = point;
        }
    }

    private void DrawThorn(float age, int seed, Vector2 anchor, float dissolve)
    {
        float start = 1.33f + Noise(seed + 3) * .32f;
        float emerge = Ramp((age - start) / .49f);
        if (emerge <= 0f) return;
        float size = .85f + .95f * Noise(seed + 37);
        float height = size * emerge * (1f - dissolve);
        float bend = (Noise(seed + 77) - .5f) * size * .62f;
        float opacity = 1f - dissolve;
        Vector2 tip = anchor + new Vector2(bend * emerge, height);

        // 돌출된 어두운 가시에 밝은 능선과 얇은 끝을 더한다.
        if (height > .01f)
        {
            Vector2 left = anchor + Vector2.left * size * .23f * emerge;
            Vector2 right = anchor + Vector2.right * size * .23f * emerge;
            Vector2 ridge = anchor + Vector2.up * height * .12f;
            _canopy.Triangle(left, ridge, tip, Fade(_bark, opacity));
            _canopy.Triangle(ridge, right, tip, Fade(new Color(.25f, .43f, .08f), opacity));
            _canopy.Stroke(ridge, tip, .045f * emerge, Fade(_tip, opacity * .68f));
        }

        // 새 덩굴은 가시 아래에서부터 세 바퀴를 따라 기어오른다.
        float wrap = Ramp((age - start - .48f) / .96f);
        Vector2 previous = Coil(anchor, height, bend, size, 0f);
        int loops = Mathf.CeilToInt(wrap * 32f);
        for (int i = 1; i <= loops && height > .01f; i++)
        {
            float t = Mathf.Min(wrap, i / 32f);
            Vector2 point = Coil(anchor, height, bend, size, t);
            _canopy.Stroke(previous, point, .13f * size * (1f - t * .57f), Fade(_bark, opacity));
            _canopy.Stroke(previous, point + Vector2.up * .012f, .074f * size * (1f - t * .57f), Fade(_vine, opacity));
            previous = point;
        }

        for (int fruit = 0; fruit < 2; fruit++)
        {
            float at = fruit == 0 ? .34f : .68f;
            Vector2 point = Coil(anchor, size, bend, size, at) + Vector2.down * .12f;
            float ripen = Ramp((age - 3.05f - Noise(seed + fruit) * .23f) / .61f);
            if (age < BURST_TIME && ripen > 0f)
                DrawFruit(point, size * .18f * ripen, ripen);
            if (age >= BURST_TIME)
                DrawBurst(age - BURST_TIME, point, size, seed * 2 + fruit);
            if (_spawnSplinters)
                EmitSplinters(point, size, seed * 2 + fruit);
        }
    }

    private void EmitSplinters(Vector2 center, float size, int seed)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 direction = Direction((Noise(seed) + i / 3f) * 2f * Mathf.PI);
            var particle = new ParticleSystem.EmitParams
            {
                position = center,
                velocity = direction * (1.9f + size),
                startLifetime = .34f + .19f * Noise(seed + i),
                startColor = i == 1 ? _amber : _tip,
                startSize = size * (.045f + .027f * i)
            };
            _splinters.Emit(particle, 1);
        }
    }

    private void DrawFruit(Vector2 center, float radius, float ripe)
    {
        _canopy.Disc(center, new Vector2(radius * 1.45f, radius * 1.48f), Fade(_vine, ripe * .22f), 10);
        _canopy.Disc(center, new Vector2(radius, radius * 1.08f), Color.Lerp(_vine, _amber, ripe), 10);
        _canopy.Disc(center + new Vector2(-radius * .22f, radius * .24f),
            new Vector2(radius * .52f, radius * .50f), Fade(_whiteGold, .93f), 8);
    }

    // 모든 열매에 같은 시작 시간을 사용하고 빛과 잎은 서로 다른 속도로 흩어진다.
    private void DrawBurst(float elapsed, Vector2 center, float size, int seed)
    {
        float t = Mathf.Clamp01(elapsed / .58f);
        float alpha = (1f - t) * (1f - t);
        float reach = size * (.25f + 1.15f * Ramp(t));
        _canopy.Disc(center, Vector2.one * reach * .55f, Fade(_amber, alpha * .38f), 12);
        _canopy.Disc(center, Vector2.one * reach * .34f, Fade(_whiteGold, alpha), 10);
        for (int i = 0; i < 7; i++)
        {
            Vector2 direction = Direction((i + Noise(seed)) * (Mathf.PI * 2f / 7f));
            Vector2 offset = direction * reach;
            _canopy.Triangle(center + Vector2.Perpendicular(direction) * reach * .15f,
                center - Vector2.Perpendicular(direction) * reach * .15f, center + offset,
                Fade(i % 2 == 0 ? _amber : _whiteGold, alpha));
            _canopy.Leaf(center + offset * (1f + t * 1.4f), direction * size * .16f,
                Fade(_vine, 1f - t), Fade(_tip, 1f - t));
        }
    }

    private Vector2 VinePoint(Vector2 caster, Vector2 direction, float range, int ray, float t)
    {
        float sway = Mathf.Sin(t * 17f + ray * 1.2f) * Mathf.Sin(t * Mathf.PI) * Mathf.Min(range * .09f, 1.25f);
        return ClampToMap(caster + direction * (range * t) + Vector2.Perpendicular(direction) * sway);
    }

    private float MapReach(Vector2 caster, Vector2 direction)
    {
        Vector2 local = caster - _mapCenter;
        float horizontal = Mathf.Abs(direction.x) < .001f ? float.MaxValue :
            (_half - local.x * Mathf.Sign(direction.x)) / Mathf.Abs(direction.x);
        float vertical = Mathf.Abs(direction.y) < .001f ? float.MaxValue :
            (_half - local.y * Mathf.Sign(direction.y)) / Mathf.Abs(direction.y);
        return Mathf.Max(0f, Mathf.Min(horizontal, vertical));
    }

    private Vector2 ClampToMap(Vector2 point) => new(
        Mathf.Clamp(point.x, _mapCenter.x - _half, _mapCenter.x + _half),
        Mathf.Clamp(point.y, _mapCenter.y - _half, _mapCenter.y + _half));

    private static Vector2 Coil(Vector2 anchor, float height, float bend, float width, float t) =>
        anchor + new Vector2(bend * t + Mathf.Sin(t * Mathf.PI * 6f) * width * (.27f - .17f * t), height * t * .89f);

    private static float Ramp(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    private static float Noise(int seed) => Mathf.Repeat(Mathf.Sin(seed * 57.731f + 8.7f) * 42315.47f, 1f);
    private static Color Fade(Color color, float opacity) => new(color.r, color.g, color.b, opacity);

    private void OnDestroy()
    {
        _ground?.Dispose();
        _canopy?.Dispose();
        Destroy(_material);
    }

    // 정점 배열을 재사용해 개별 넝쿨, 가시, 파편 오브젝트 생성을 피한다.
    private sealed class Geometry
    {
        private readonly Mesh _mesh;
        private readonly Transform _root;
        private readonly List<Vector3> _vertices = new(32768);
        private readonly List<Color32> _colors = new(32768);
        private readonly List<int> _indices = new(32768);
        private Vector2 _origin;

        internal Geometry(Transform parent, string name, Material material, int order)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            _root = host.transform;
            _mesh = new Mesh { name = name };
            _mesh.MarkDynamic();
            host.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
        }

        internal void Clear()
        {
            _origin = _root.position;
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
        }

        internal void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int index = _vertices.Count;
            _vertices.Add(a - _origin);
            _vertices.Add(b - _origin);
            _vertices.Add(c - _origin);
            Color32 tint = color;
            _colors.Add(tint);
            _colors.Add(tint);
            _colors.Add(tint);
            _indices.Add(index);
            _indices.Add(index + 1);
            _indices.Add(index + 2);
        }

        internal void Stroke(Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 side = Vector2.Perpendicular(direction) * (width * .5f);
            // 이어지는 줄기 구간이 각도 때문에 벌어지지 않도록 끝을 조금 겹친다.
            Vector2 overlap = direction * (width * .24f);
            start -= overlap;
            end += overlap;
            Triangle(start - side, start + side, end + side, color);
            Triangle(start - side, end + side, end - side, color);
        }

        internal void Leaf(Vector2 start, Vector2 reach, Color shade, Color light)
        {
            Vector2 middle = start + reach * .49f;
            Vector2 side = Vector2.Perpendicular(reach) * .26f;
            Triangle(start, middle + side, start + reach, shade);
            Triangle(start, start + reach, middle - side, light);
        }

        internal void Disc(Vector2 center, Vector2 radius, Color color, int sides)
        {
            Vector2 previous = center + Vector2.right * radius.x;
            for (int i = 1; i <= sides; i++)
            {
                Vector2 point = center + Vector2.Scale(Direction(i * Mathf.PI * 2f / sides), radius);
                Triangle(center, previous, point, color);
                previous = point;
            }
        }

        internal void Upload()
        {
            _mesh.Clear(false);
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0);
        }

        internal void Dispose() => Object.Destroy(_mesh);
    }
}
