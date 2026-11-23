using System.Collections.Generic;
using UnityEngine;

// 진기폭발과 유성낙화를 Particle System으로 비교 재생한다.
public sealed class BattleAwakeningParticleEffect : MonoBehaviour
{
    private const int ENERGY_COUNT = 76;
    private const int MAP_GRID = 13;
    private const float GATHER_END = 1f;
    private const float ORBIT_END = 1.5f;
    private const float SPREAD_END = 2.4f;
    private const float ENERGY_IMPACT = 2.9f;
    private const float SCROLL_END = .75f;
    private const float METEOR_START = .9f;
    private const float METEOR_IMPACT = 1.8f;
    private const float FIRE_SPREAD = 1.45f;

    private static readonly Color _purple = new(.68f, .27f, 1f);
    private static readonly Color _amber = new(1f, .53f, .12f);
    private static readonly Color _hot = new(1f, .91f, .63f);
    private static readonly Color[] _energyColors = { new(.67f, .64f, .73f), new(.045f, .025f, .066f), _purple };

    // 발동 중 사용할 입자 시스템을 스프라이트와 렌더 순서별로 나눈다.
    private readonly List<Layer> _layers = new();
    private Layer _energy;
    private Layer _flash;
    private Layer _scroll;
    private Layer _smoke;
    private Layer _meteor;
    private Layer _debris;
    private Layer _shockwave;
    private Layer[] _groundFire;
    private readonly GroundPatch[] _patches = new GroundPatch[MAP_GRID * MAP_GRID];

    private struct GroundPatch
    {
        public Vector2 Position;
        public float IgnitionDelay;
        public int ImageIndex;
    }

    // 기 조각과 섬광용 파티클 시스템을 준비한다.
    public void ConfigureEnergy(Sprite pixel, Material material, int order)
    {
        _energy = AddLayer("Energy Qi", pixel, material, order, 1500);
        _flash = AddLayer("Energy flash", pixel, material, order - 1, 1);
    }

    // 유성낙화에 쓰는 기존 스프라이트와 맵 전체 불길의 점화 위치를 준비한다.
    public void ConfigureMeteor(Sprite scroll, Sprite smoke, Sprite meteorBody, Sprite debris, Sprite[] groundFire,
        Sprite shockwave, Sprite pixel, Material material, int order, BattleMap map, Vector2 impact)
    {
        _scroll = AddLayer("Scroll", scroll, material, order, 1);
        _smoke = AddLayer("Smoke", smoke, material, order + 2, 1);
        _meteor = AddLayer("Single meteor", meteorBody, material, order + 2, 1);
        _debris = AddLayer("Debris", debris, material, order + 1, 9);
        _shockwave = AddLayer("Shockwave", shockwave, material, order + 2, 1);
        _flash = AddLayer("Meteor flash", pixel, material, order + 3, 1);
        _groundFire = new Layer[groundFire.Length];
        for (int i = 0; i < groundFire.Length; i++)
        {
            _groundFire[i] = AddLayer($"Ground fire {i}", groundFire[i], material, -10,
                (MAP_GRID * MAP_GRID + groundFire.Length - 1) / groundFire.Length + MAP_GRID);
        }

        // 점화 시각은 충돌 지점과 각 바닥 위치 사이의 실제 거리로 구한다.
        Vector2 center = _mapCenter = map.transform.position;
        float half = map.HalfExtent;
        float cell = half * 2f / MAP_GRID;
        Vector2 localImpact = impact - center;
        float farthest = new Vector2(half + Mathf.Abs(localImpact.x), half + Mathf.Abs(localImpact.y)).magnitude;
        for (int y = 0; y < MAP_GRID; y++)
        {
            for (int x = 0; x < MAP_GRID; x++)
            {
                int i = y * MAP_GRID + x;
                Vector2 jitter = new(Mathf.Sin(i * 5.37f), Mathf.Cos(i * 3.19f));
                Vector2 position = center + (new Vector2(x + .5f, y + .5f) + jitter * .14f) * cell - Vector2.one * half;
                _patches[i] = new GroundPatch
                {
                    Position = position,
                    IgnitionDelay = Vector2.Distance(impact, position) / farthest * FIRE_SPREAD,
                    ImageIndex = (x + y * 3) % groundFire.Length
                };
            }
        }
        _fireCell = cell;
    }

    private float _fireCell;
    private Vector2 _mapCenter;

    // 파티클 시스템을 유지한 채 이번 충돌 위치에 맞춰 점화 시각을 초기화한다.
    internal void SetImpactPoint(Vector2 impact)
    {
        if (_groundFire == null) return;
        Vector2 origin = impact - _mapCenter;
        float half = _fireCell * MAP_GRID * .5f;
        float farthest = new Vector2(half + Mathf.Abs(origin.x), half + Mathf.Abs(origin.y)).magnitude;
        for (int i = 0; i < _patches.Length; i++)
            _patches[i].IgnitionDelay = Vector2.Distance(impact, _patches[i].Position) / farthest * FIRE_SPREAD;
    }

    // 파티클을 전투 시계에 맞춰 갱신한다. 두 연출 모두 같은 프레임에서 정지와 타격 시점을 맞춘다.
    public Vector2 Render(float age, Vector2 caster, Vector2 cameraCenter, float width, float height)
    {
        foreach (Layer layer in _layers)
        {
            layer.Clear();
        }

        Vector2 shake = _energy != null
            ? RenderEnergy(age, caster, cameraCenter, width, height)
            : RenderMeteor(age, cameraCenter, width, height);

        foreach (Layer layer in _layers)
        {
            layer.Apply();
        }
        return shake;
    }

    // 회색·검은색·보라색 기를 모으고, 공전과 확산 이후 같은 시각에 터뜨린다.
    private Vector2 RenderEnergy(float age, Vector2 caster, Vector2 center, float width, float height)
    {
        float impact = age - ENERGY_IMPACT;
        float middle = age - ORBIT_END;
        if (middle is >= 0f and < .65f)
        {
            float t = middle / .65f;
            Ring(caster, Mathf.Lerp(.12f, height * .45f, Ease(t)), .14f, Fade(_purple, .9f * (1f - t)), 48);
        }
        if (impact is >= 0f and < .28f)
        {
            _flash.Add(center, width * 1.12f, height * 1.12f, 0f, Fade(_purple, .43f * Decay(impact, .28f)));
        }

        for (int i = 0; i < ENERGY_COUNT; i++)
        {
            float angle = (i + Seed(i)) * 2f * Mathf.PI / ENERGY_COUNT;
            float spin = Seed(i + 273) * 1.15f;
            float spread = Mathf.Lerp(.42f, .97f, Seed(i + 180));
            float size = Mathf.Lerp(.075f, .16f, Seed(i + 341));
            float orbit = .62f + spread * .62f;
            float endAngle = angle + spin * .72f + 2.25f;
            Vector2 endRay = Direction(endAngle);
            Vector2 stop = center + new Vector2(endRay.x * width, endRay.y * height) * (.48f * spread);
            Vector2 launch = caster + endRay * orbit * .64f;
            Vector2 position = stop;
            Vector2 tail = Vector2.zero;
            Color color = _energyColors[i % _energyColors.Length];
            Color edge = i % 3 == 1 ? _energyColors[0] : color;

            if (age < GATHER_END)
            {
                float t = age / GATHER_END;
                Vector2 ray = Direction(angle + spin * t * .72f);
                Vector2 start = center + new Vector2(ray.x * width, ray.y * height) * Mathf.Lerp(.54f, .74f, Seed(i + 92));
                position = Vector2.Lerp(start, caster + ray * orbit, Ease(t));
                tail = ray * (.12f + .44f * Mathf.Sin(t * Mathf.PI));
            }
            else if (age < ORBIT_END)
            {
                float t = (age - GATHER_END) / (ORBIT_END - GATHER_END);
                Vector2 ray = Direction(angle + spin * .72f + t * 2.25f);
                position = caster + ray * Mathf.Lerp(orbit, orbit * .64f, t);
                tail = new Vector2(ray.y, -ray.x) * Mathf.Lerp(.12f, .4f, t);
            }
            else if (age < SPREAD_END)
            {
                float t = (age - ORBIT_END) / (SPREAD_END - ORBIT_END);
                position = Vector2.Lerp(launch, stop, Ease(t));
                tail = (launch - stop).normalized * (.58f * (1f - t));
            }

            if (impact < 0f)
            {
                Trail(position, tail, size * .50f, Fade(edge, .55f));
                _energy.Add(position, size * 1.35f, size * 1.35f, 45f, Fade(edge, .60f));
                _energy.Add(position, size, size, 45f, color);
            }
            else if (impact < 1.1f)
            {
                float fade = Decay(impact, 1.1f);
                float radius = .06f + Ease(impact / 1.1f) * (.52f + size * 2f);
                Ring(stop, radius, .06f, Fade(edge, .85f * fade), 12);
                if (impact < .28f)
                {
                    float flare = size + .16f + Ease(impact / .28f) * .25f;
                    _energy.Add(stop, flare, flare, 45f, Fade(Color.Lerp(edge, Color.white, .72f), Decay(impact, .28f)));
                }
                for (int ray = 0; ray < 5; ray++)
                {
                    Vector2 direction = Direction(endAngle + ray * Mathf.PI * .4f);
                    Trail(stop + direction * radius * .55f, direction * radius * .62f, .025f, Fade(edge, fade));
                }
            }
        }
        return Shake(impact, .28f, .15f);
    }

    // 두루마리 하나, 유성 한 개와 충돌 이후의 맵 전체 불길을 별개 파티클로 표시한다.
    private Vector2 RenderMeteor(float age, Vector2 center, float width, float height)
    {
        float smokeAge = age - SCROLL_END;
        float impact = age - METEOR_IMPACT;
        if (age < SCROLL_END)
        {
            _scroll.Add(center, width * .71f * Mathf.Lerp(.1f, 1f, Ease(age / .42f)), height * .23f, 0f, Color.white);
        }
        if (smokeAge is >= 0f and < .60f)
        {
            float puff = Ease(smokeAge / .60f);
            _smoke.Add(center + Vector2.up * puff * .55f, width * Mathf.Lerp(.48f, .93f, puff),
                height * Mathf.Lerp(.18f, .34f, puff), 0f, Fade(Color.white, 1f - puff));
        }
        if (age is >= METEOR_START and < METEOR_IMPACT)
        {
            float t = (age - METEOR_START) / (METEOR_IMPACT - METEOR_START);
            Vector2 from = new(width * .78f, height * .61f);
            float size = Mathf.Lerp(width * .23f, width * .38f, t);
            float rotation = Mathf.Atan2(from.y, from.x) * Mathf.Rad2Deg - 53f;
            _meteor.Add(center + from * (1f - t * t), size * 1.75f, size * 2.31f, rotation, Color.white);
        }
        float flash = Mathf.Max(Decay(smokeAge, .14f) * .18f, Decay(impact, .26f) * .57f);
        if (flash > 0f)
        {
            _flash.Add(center, width * 1.12f, height * 1.12f, 0f, Fade(_hot, flash));
        }
        if (impact is >= 0f and < .82f)
        {
            float t = impact / .82f;
            float radius = Mathf.Lerp(.15f, new Vector2(width, height).magnitude * .53f, Ease(t));
            _shockwave.Add(center, radius * 2.55f, radius * 3.06f, 0f,
                Fade(Color.Lerp(_hot, _amber, t), Decay(impact, .82f)));
        }
        if (impact is >= 0f and < .95f)
        {
            float t = impact / .95f;
            for (int i = 0; i < 9; i++)
            {
                float size = width * (.055f + i % 3 * .018f) * Mathf.Lerp(1f, .38f, t);
                Vector2 position = center + Direction(i * 2.4f) * (Ease(t) * (1.25f + i % 4 * .6f));
                _debris.Add(position, size * 1.3f, size, i * 41f + t * 150f, Fade(_amber, 1f - t));
            }
        }
        if (_groundFire.Length > 0 && impact is > 0f and < 3.2f)
        {
            float fade = Mathf.Clamp01((3.2f - impact) / .55f);
            for (int i = 0; i < _patches.Length; i++)
            {
                GroundPatch patch = _patches[i];
                float elapsed = impact - patch.IgnitionDelay;
                if (elapsed <= 0f)
                {
                    continue;
                }
                float grow = Mathf.SmoothStep(0f, 1f, elapsed / .42f);
                float pulse = 1f + .045f * Mathf.Sin(impact * 13f + i * 1.37f);
                float size = _fireCell * Mathf.Lerp(.26f, 1.44f, grow) * pulse;
                float opacity = Mathf.Lerp(1f, .58f, Mathf.Clamp01(elapsed / 1.8f)) * grow * fade;
                _groundFire[patch.ImageIndex].Add(patch.Position, size, size, i * 137.508f, Fade(Color.white, opacity));
            }
        }
        return Shake(impact, .32f, .2f);
    }

    // 특정 스프라이트를 쓰는 Particle System과 프레임 버퍼를 등록한다.
    private Layer AddLayer(string layerName, Sprite sprite, Material material, int sortingOrder, int capacity)
    {
        var host = new GameObject(layerName);
        host.transform.SetParent(transform, false);
        var layer = new Layer(host, sprite, material, sortingOrder, capacity);
        _layers.Add(layer);
        return layer;
    }

    // 고리의 둘레를 짧은 파티클로 그려 충격파가 퍼지는 형태를 만든다.
    private void Ring(Vector2 center, float radius, float width, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            _energy.Add(center + Direction(angle) * radius, radius * Mathf.PI * 2f / count * 1.12f,
                width, angle * Mathf.Rad2Deg + 90f, color);
        }
    }

    // 움직이는 기와 폭발 파편의 길쭉한 입자를 방향에 맞춘다.
    private void Trail(Vector2 position, Vector2 tail, float width, Color color)
    {
        if (tail.sqrMagnitude > .001f)
        {
            _energy.Add(position + tail * .5f, tail.magnitude, width, Mathf.Atan2(tail.y, tail.x) * Mathf.Rad2Deg, color);
        }
    }

    private static Vector2 Shake(float age, float duration, float strength) =>
        new Vector2(Mathf.Sin(age * 101f), Mathf.Cos(age * 119f)) * (strength * Decay(age, duration));

    private static float Seed(int index) => Mathf.Repeat(Mathf.Sin(index * 71.971f + 13.75f) * 49731.776f, 1f);
    private static float Ease(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
    private static float Decay(float age, float duration) => age < 0f ? 0f : Mathf.Pow(Mathf.Clamp01(1f - age / duration), 2f);
    private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    private static Color Fade(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

    // 레이어 전용 머티리얼을 반환한다.
    private void OnDestroy()
    {
        foreach (Layer layer in _layers)
        {
            Destroy(layer.Material);
        }
    }

    // 입자 시스템에 현재 프레임의 월드 좌표와 스프라이트별 크기를 전달한다.
    private sealed class Layer
    {
        private readonly ParticleSystem _system;
        private readonly ParticleSystem.Particle[] _particles;
        private readonly Vector2 _pivotOffset;
        private int _count;
        public Material Material { get; }

        public Layer(GameObject host, Sprite sprite, Material material, int sortingOrder, int capacity)
        {
            _particles = new ParticleSystem.Particle[capacity];
            host.SetActive(false);
            _system = host.AddComponent<ParticleSystem>();
            var main = _system.main;
            main.playOnAwake = false;
            main.startSize3D = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            var emission = _system.emission;
            emission.enabled = false;
            var shape = _system.shape;
            shape.enabled = false;

            Material = new Material(material) { mainTexture = sprite.texture };
            var sheet = _system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(sprite);
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material;
            renderer.sortingOrder = sortingOrder;
            renderer.maxParticleSize = 2f;
            _pivotOffset = new Vector2(.5f - sprite.pivot.x / sprite.rect.width, .5f - sprite.pivot.y / sprite.rect.height);
            host.SetActive(true);
            _system.Play(false);
            _system.Pause(false);
        }

        public void Clear() => _count = 0;

        public void Add(Vector2 position, float width, float height, float angle, Color color)
        {
            if (_count >= _particles.Length || color.a <= 0f)
            {
                return;
            }
            Vector2 offset = Vector2.Scale(_pivotOffset, new Vector2(width, height));
            Vector3 rotated = Quaternion.Euler(0f, 0f, angle) * offset;
            _particles[_count++] = new ParticleSystem.Particle
            {
                position = (Vector3)position + rotated,
                startSize3D = new Vector3(width, height, 1f),
                rotation = angle,
                startColor = color,
                startLifetime = 1f,
                remainingLifetime = 1f,
                randomSeed = (uint)(_count + 1)
            };
        }

        public void Apply() => _system.SetParticles(_particles, _count);
    }
}
