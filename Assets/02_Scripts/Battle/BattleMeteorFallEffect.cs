using UnityEngine;

// 유성낙화의 두루마리, 낙하, 충돌, 불길을 전투 화면에 순서대로 그린다.
public sealed class BattleMeteorFallEffect : MonoBehaviour
{
    private const float SCROLL_END = .75f;
    private const float FALL_START = .9f;
    private const float IMPACT = 1.8f;
    private const float FIRE_SPREAD = 1.45f;
    private const int GROUND_FIRE_GRID = 13;
    private const int FRAGMENT_COUNT = 9;

    private static readonly Color _amber = new(1f, .53f, .12f);
    private static readonly Color _hot = new(1f, .91f, .63f);

    // 유성낙화 전용 스프라이트를 컷신 이후 시간에 맞춰 이동하고 표시한다.
    private SpriteRenderer _scroll;
    private SpriteRenderer _smoke;
    private SpriteRenderer _rock;
    private SpriteRenderer _flash;
    private readonly SpriteRenderer[] _fragments = new SpriteRenderer[FRAGMENT_COUNT];
    // 바닥 불길은 카메라와 독립된 맵 위치를 사용한다.
    private Transform _groundFireRoot;
    private Vector3 _mapCenter;
    private float _fireCellSize;
    private readonly GroundFirePatch[] _groundFire = new GroundFirePatch[GROUND_FIRE_GRID * GROUND_FIRE_GRID];

    private struct GroundFirePatch
    {
        public SpriteRenderer View;
        public float IgnitionDelay;
    }
    private SpriteRenderer _shockwave;

    // 두루마리와 바위는 한 장씩 표시하며 충돌 뒤에만 파편과 불꽃을 사용한다.
    public void Initialize(Sprite scroll, Sprite smoke, Sprite meteorBody, Sprite debris, Sprite[] groundFire,
        Sprite shockwave, Sprite pixel, int order, Vector3 mapCenter, float mapHalfExtent, Vector3 impactPoint)
    {
        _scroll = CreateSprite("Summoning scroll", scroll, order);
        _smoke = CreateSprite("Scroll smoke", smoke, order + 2);
        _rock = CreateSprite("Meteor core", meteorBody, order + 2);
        _flash = CreateSprite("Impact flash", pixel, order + 3);

        for (int i = 0; i < _fragments.Length; i++)
        {
            _fragments[i] = CreateSprite("Meteor fragment", debris, order + 1);
        }
        PrepareGroundFire(groundFire, mapCenter, mapHalfExtent, impactPoint);

        _shockwave = CreateSprite("Meteor impact shockwave", shockwave, order + 2);
    }

    // 현재 카메라 위치를 기준으로 기존 불꽃의 점화 시각을 다시 계산한다.
    internal void SetImpactPoint(Vector2 impactPoint)
    {
        if (_groundFireRoot == null) return;
        Vector2 origin = impactPoint - (Vector2)_mapCenter;
        float half = _fireCellSize * GROUND_FIRE_GRID * .5f;
        float farthest = new Vector2(half + Mathf.Abs(origin.x), half + Mathf.Abs(origin.y)).magnitude;
        for (int i = 0; i < _groundFire.Length; i++)
        {
            var view = _groundFire[i].View;
            _groundFire[i].IgnitionDelay = Vector2.Distance(origin, view.transform.localPosition) / farthest * FIRE_SPREAD;
        }
    }

    // 화면 중앙을 낙하지점으로 삼아 각 단계의 표시 상태를 갱신한다.
    public void Render(float age, float width, float height)
    {
        float impactAge = age - IMPACT;

        // 두루마리 하나를 좌우로 펼친 뒤 연기로 전환한다.
        _scroll.enabled = age < SCROLL_END && _scroll.sprite != null;
        if (_scroll.enabled)
        {
            float opening = Ease(age / .42f);
            Place(_scroll, Vector2.zero, Mathf.Lerp(.1f, 1f, opening) * width * .71f,
                height * .23f, 0f, Color.white);
        }
        float smokeAge = age - SCROLL_END;
        _smoke.enabled = smokeAge is >= 0f and < .60f && _smoke.sprite != null;
        if (_smoke.enabled)
        {
            float puff = Ease(smokeAge / .60f);
            Place(_smoke, Vector2.up * puff * .55f, width * Mathf.Lerp(.48f, .93f, puff),
                height * Mathf.Lerp(.18f, .34f, puff), 0f, Fade(Color.white, 1f - puff));
        }

        // 불꽃 꼬리가 포함된 유성 한 개의 바위 중심이 화면 중앙에 도달하도록 움직인다.
        bool falling = age is >= FALL_START and < IMPACT;
        _rock.enabled = falling && _rock.sprite != null;
        if (_rock.enabled)
        {
            float progress = (age - FALL_START) / (IMPACT - FALL_START);
            Vector2 start = new(width * .78f, height * .61f);
            Vector2 position = Vector2.Lerp(start, Vector2.zero, progress * progress);
            Vector2 direction = start.normalized;
            float size = Mathf.Lerp(width * .23f, width * .38f, progress);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 53f;
            Place(_rock, position, size * 1.75f, size * 2.31f, angle, Color.white);
        }

        // 화면 충돌 섬광은 두루마리 소멸 때보다 강하게 표시한다.
        float flash = Mathf.Max(Flash(smokeAge, .14f) * .18f, Flash(impactAge, .26f) * .57f);
        _flash.enabled = flash > 0f;
        if (_flash.enabled)
        {
            Place(_flash, Vector2.zero, width * 1.12f, height * 1.12f, 0f, Fade(_hot, flash));
        }

        RenderImpact(impactAge, width, height);
        RenderFire(impactAge);
    }

    // 충돌 지점에서 원형 충격파와 바위 파편을 퍼뜨린다.
    private void RenderImpact(float age, float width, float height)
    {
        _shockwave.enabled = age is >= 0f and < .82f && _shockwave.sprite != null;
        if (_shockwave.enabled)
        {
            float time = age / .82f;
            float radius = Mathf.Lerp(.15f, Mathf.Sqrt(width * width + height * height) * .53f, Ease(time));
            Color color = Fade(Color.Lerp(_hot, _amber, time), (1f - time) * (1f - time));
            Place(_shockwave, Vector2.zero, radius * 2.55f, radius * 3.06f, 0f, color);
        }

        for (int i = 0; i < _fragments.Length; i++)
        {
            SpriteRenderer fragment = _fragments[i];
            fragment.enabled = age is >= 0f and < .95f && fragment.sprite != null;
            if (!fragment.enabled)
            {
                continue;
            }

            float time = age / .95f;
            Vector2 position = Direction(i * 2.4f) * (Ease(time) * (1.25f + i % 4 * .6f));
            float size = width * (.055f + i % 3 * .018f) * Mathf.Lerp(1f, .38f, time);
            Place(fragment, position, size * 1.3f, size, i * 41f + time * 150f,
                Fade(Color.Lerp(_amber, new Color(.41f, .25f, .21f), time), 1f - time));
        }
    }

    // 바닥 스프라이트 네 종류를 맵 격자에 나누고 낙하지점과의 거리로 점화 시간을 정한다.
    private void PrepareGroundFire(Sprite[] sprites, Vector3 mapCenter, float halfExtent, Vector3 impactPoint)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return;
        }

        _groundFireRoot = new GameObject("Meteor map ground fire").transform;
        _groundFireRoot.SetParent(transform, false);
        _mapCenter = mapCenter;
        _groundFireRoot.position = _mapCenter;
        _fireCellSize = halfExtent * 2f / GROUND_FIRE_GRID;
        Vector2 origin = impactPoint - _mapCenter;
        float maxDistance = new Vector2(halfExtent + Mathf.Abs(origin.x), halfExtent + Mathf.Abs(origin.y)).magnitude;
        for (int y = 0; y < GROUND_FIRE_GRID; y++)
        {
            for (int x = 0; x < GROUND_FIRE_GRID; x++)
            {
                int index = y * GROUND_FIRE_GRID + x;
                var view = new GameObject("Ground fire patch").AddComponent<SpriteRenderer>();
                view.transform.SetParent(_groundFireRoot, false);
                view.sprite = sprites[(x + y * 3) % sprites.Length];
                view.sortingOrder = -10;
                view.enabled = false;

                // 불꽃이 규칙적인 격자로 보이지 않도록 셀 안에서 위치와 회전을 조금 다르게 한다.
                Vector2 offset = new(Mathf.Sin(index * 5.37f), Mathf.Cos(index * 3.19f));
                Vector2 position = (new Vector2(x + .5f, y + .5f) + offset * .14f) * _fireCellSize
                    - Vector2.one * halfExtent;
                view.transform.localPosition = position;
                view.transform.localRotation = Quaternion.Euler(0f, 0f, index * 137.508f);
                view.flipX = (x + y) % 2 == 0;
                float delay = Vector2.Distance(origin, position) / maxDistance * FIRE_SPREAD;
                _groundFire[index] = new GroundFirePatch { View = view, IgnitionDelay = delay };
            }
        }
    }

    // 낙하지점에서 멀어지는 순서로 점화하고 이미 탄 곳에는 낮은 불길을 남긴다.
    private void RenderFire(float age)
    {
        if (_groundFireRoot == null)
        {
            return;
        }

        // 충돌 카메라가 움직여도 불길의 월드 좌표는 맵에 고정한다.
        _groundFireRoot.position = _mapCenter;
        float fade = Mathf.Clamp01((3.2f - age) / .55f);
        for (int i = 0; i < _groundFire.Length; i++)
        {
            GroundFirePatch patch = _groundFire[i];
            SpriteRenderer flame = patch.View;
            float elapsed = age - patch.IgnitionDelay;
            flame.enabled = elapsed > 0f && fade > 0f && flame.sprite != null;
            if (!flame.enabled)
            {
                continue;
            }

            float grow = Mathf.SmoothStep(0f, 1f, elapsed / .42f);
            float pulse = 1f + .045f * Mathf.Sin(age * 13f + i * 1.37f);
            float width = _fireCellSize * Mathf.Lerp(.26f, 1.44f, grow) * pulse;
            Bounds bounds = flame.sprite.bounds;
            float scale = width / Mathf.Max(bounds.size.x, bounds.size.y);
            flame.transform.localScale = Vector3.one * scale;
            flame.color = Fade(Color.white, Mathf.Lerp(.58f, 1f, 1f - Mathf.Clamp01(elapsed / 1.8f)) * grow * fade);
        }
    }


    // 충돌에 맞춰 카메라를 짧게 흔들며 후속 프레임에 이동값을 남기지 않는다.
    public Vector2 GetCameraOffset(float age)
    {
        float time = age - IMPACT;
        if (time is < 0f or >= .32f)
        {
            return Vector2.zero;
        }
        float strength = 1f - time / .32f;
        return new Vector2(Mathf.Sin(time * 112f), Mathf.Cos(time * 98f)) * (.2f * strength * strength);
    }

    // 장면에 놓을 스프라이트를 애니메이션 시작 전 비활성으로 준비한다.
    private SpriteRenderer CreateSprite(string spriteName, Sprite sprite, int order)
    {
        var view = new GameObject(spriteName).AddComponent<SpriteRenderer>();
        view.transform.SetParent(transform, false);
        view.sprite = sprite;
        view.sortingOrder = order;
        view.enabled = false;
        return view;
    }

    // 원본 픽셀 밀도에 관계없이 스프라이트에 월드 폭과 높이를 적용한다.
    private static void Place(SpriteRenderer view, Vector2 position, float width, float height, float angle, Color color)
    {
        if (!view.enabled)
        {
            return;
        }
        Vector3 size = view.sprite.bounds.size;
        view.transform.localPosition = position;
        view.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        view.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        view.color = color;
    }

    private static float Flash(float age, float duration)
    {
        return age >= 0f ? Mathf.Pow(Mathf.Clamp01(1f - age / duration), 2f) : 0f;
    }

    private static float Ease(float value) => 1f - Mathf.Pow(1f - Mathf.Clamp01(value), 3f);
    private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    private static Color Fade(Color value, float alpha) => new(value.r, value.g, value.b, alpha);
}
