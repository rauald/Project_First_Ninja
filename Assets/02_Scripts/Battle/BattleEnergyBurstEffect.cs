using System.Collections.Generic;
using UnityEngine;

// 진기폭발의 기 흡수, 공전, 확산과 동시 폭발을 하나의 메시로 그린다.
public sealed class BattleEnergyBurstEffect : MonoBehaviour
{
    private const int MOTE_COUNT = 76;
    private const float GATHER_END = 1f;
    private const float ORBIT_END = 1.5f;
    private const float SPREAD_END = 2.4f;
    private const float DETONATION = 2.9f;
    private const float AFTERSHOCK_DURATION = 1.1f;

    // 어두운 전장에서도 검은 기를 읽을 수 있도록 밝은 외곽을 함께 그린다.
    private static readonly Color[] _colors =
    {
        new(.67f, .64f, .73f), new(.045f, .025f, .066f), new(.68f, .27f, 1f)
    };

    // 매 프레임 같은 버퍼를 재사용해 입자별 오브젝트 생성을 피한다.
    private readonly List<Vector3> _vertices = new(8192);
    private readonly List<Color> _vertexColors = new(8192);
    private readonly List<int> _triangles = new(12288);
    private Mesh _mesh;
    private Material _material;

    // 진기폭발 전용 메시와 머티리얼을 만든다.
    public void Initialize(int sortingOrder)
    {
        var renderer = gameObject.AddComponent<MeshRenderer>();
        var filter = gameObject.AddComponent<MeshFilter>();
        _mesh = new Mesh { name = "Energy burst" };
        _mesh.MarkDynamic();
        filter.sharedMesh = _mesh;
        _material = new Material(Shader.Find("Sprites/Default"));
        renderer.sharedMaterial = _material;
        renderer.sortingOrder = sortingOrder;
    }

    // 컷신을 제외한 각성 시간으로 모든 입자의 위치와 충격파를 평가한다.
    public void Render(float age, Vector2 origin, float width, float height)
    {
        _vertices.Clear();
        _vertexColors.Clear();
        _triangles.Clear();

        // 중앙 폭발은 기가 바깥으로 출발하는 순간에만 짧게 그린다.
        float middleBlast = age - ORBIT_END;
        if (middleBlast is >= 0f and < .65f)
        {
            float progress = middleBlast / .65f;
            Ring(origin, Mathf.Lerp(.12f, height * .45f, Ease(progress)), .16f * (1f - progress),
                WithAlpha(_colors[2], (1f - progress) * .85f), 40);
        }

        // 마지막 폭발이 시작되면 흩어진 모든 지점이 같은 시계로 터진다.
        float impactAge = age - DETONATION;
        if (impactAge is >= 0f and < .28f)
        {
            float flash = 1f - impactAge / .28f;
            Color tint = WithAlpha(_colors[2], .43f * flash * flash);
            Quad(new Vector2(-width, -height), new Vector2(width, -height),
                new Vector2(width, height), new Vector2(-width, height), tint);
        }
        for (int i = 0; i < MOTE_COUNT; i++)
        {
            DrawMote(i, age, impactAge, origin, width, height);
        }

        // 밝기와 위치를 갱신한 뒤 카메라 컬링에 쓰는 메시 범위를 다시 계산한다.
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_vertexColors);
        _mesh.SetTriangles(_triangles, 0);
    }

    // 짧은 화면 흔들림은 동시 폭발 시점에서만 적용한다.
    public Vector2 GetCameraOffset(float age)
    {
        float impact = age - DETONATION;
        if (impact < 0f || impact >= .28f)
        {
            return Vector2.zero;
        }

        float strength = 1f - impact / .28f;
        return new Vector2(Mathf.Sin(impact * 101f), Mathf.Cos(impact * 119f)) * (.15f * strength * strength);
    }

    // 각 기의 출발점과 정지점을 고정해 정지 구간에서 잔상이 이동하지 않게 한다.
    private void DrawMote(int index, float age, float impact, Vector2 origin, float width, float height)
    {
        float angle = (index + Seed(index)) * Mathf.PI * 2f / MOTE_COUNT;
        float spin = Seed(index + 273) * 1.15f;
        float spread = Mathf.Lerp(.42f, .97f, Seed(index + 180));
        float size = Mathf.Lerp(.035f, .092f, Seed(index + 341));
        float orbitRadius = .62f + spread * .62f;
        float finalAngle = angle + spin * .72f + 2.25f;
        Vector2 finalDirection = Direction(finalAngle);
        Vector2 stop = new(finalDirection.x * width * .48f * spread, finalDirection.y * height * .48f * spread);
        Vector2 launch = origin + finalDirection * (orbitRadius * .64f);
        Vector2 position = stop;
        Vector2 tail = Vector2.zero;
        Color color = _colors[index % _colors.Length];

        if (age < GATHER_END)
        {
            float progress = age / GATHER_END;
            Vector2 inward = Direction(angle + spin * progress * .72f);
            Vector2 from = new(inward.x * width * .58f, inward.y * height * .58f);
            from *= Mathf.Lerp(.95f, 1.3f, Seed(index + 92));
            Vector2 near = origin + inward * orbitRadius;
            position = Vector2.LerpUnclamped(from, near, Ease(progress));
            tail = inward * (.12f + .44f * Mathf.Sin(progress * Mathf.PI));
        }
        else if (age < ORBIT_END)
        {
            float progress = (age - GATHER_END) / (ORBIT_END - GATHER_END);
            Vector2 outward = Direction(angle + spin * .72f + progress * 2.25f);
            position = origin + outward * Mathf.Lerp(orbitRadius, orbitRadius * .64f, progress);
            tail = new Vector2(outward.y, -outward.x) * Mathf.Lerp(.12f, .4f, progress);
        }
        else if (age < SPREAD_END)
        {
            float progress = (age - ORBIT_END) / (SPREAD_END - ORBIT_END);
            position = Vector2.LerpUnclamped(launch, stop, Ease(progress));
            tail = (launch - stop).normalized * (.58f * (1f - progress));
        }

        // 2.4~2.9초 동안 멈춘 점을 표시한 뒤 충격파와 파편으로 교체한다.
        if (impact < 0f)
        {
            Color edge = index % 3 == 1 ? _colors[0] : color;
            if (tail.sqrMagnitude > .001f)
            {
                Stroke(position, position + tail, size * .75f, WithAlpha(edge, .62f));
            }
            Diamond(position, size * 1.45f, WithAlpha(edge, .42f));
            Diamond(position, size, color);
        }
        else if (impact < AFTERSHOCK_DURATION)
        {
            DrawAftershock(stop, finalAngle, size, index % 3 == 1 ? _colors[0] : color, impact);
        }
    }

    // 정지한 자리에서 바로 섬광을 표시하고 바깥으로 퍼지는 고리와 파편을 그린다.
    private void DrawAftershock(Vector2 position, float angle, float size, Color color, float impact)
    {
        float progress = impact / AFTERSHOCK_DURATION;
        float radius = .05f + Ease(progress) * (.52f + size * 3f);
        float fade = (1f - progress) * (1f - progress);
        Ring(position, radius, Mathf.Lerp(.065f, .018f, progress), WithAlpha(color, .86f * fade), 12);

        if (impact < .28f)
        {
            float flare = 1f - impact / .28f;
            Diamond(position, size + .09f + Ease(impact / .28f) * .16f,
                WithAlpha(Color.Lerp(color, Color.white, .80f), flare * flare));
        }

        for (int i = 0; i < 5; i++)
        {
            Vector2 ray = Direction(angle + i * Mathf.PI * .4f);
            Stroke(position + ray * radius * .54f, position + ray * radius * 1.2f, .025f,
                WithAlpha(color, .85f * fade));
        }
    }

    // 원형 충격파를 얇은 사각형 띠로 묶는다.
    private void Ring(Vector2 center, float radius, float width, Color color, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            Vector2 a = Direction(i * Mathf.PI * 2f / segments);
            Vector2 b = Direction((i + 1) * Mathf.PI * 2f / segments);
            Quad(center + a * (radius - width), center + a * radius,
                center + b * radius, center + b * (radius - width), color);
        }
    }

    // 이동 궤적과 폭발 파편을 그린다.
    private void Stroke(Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 side = new Vector2(-direction.y, direction.x) * (width * .5f);
        Quad(start - side, start + side, end + side * .25f, end - side * .25f, color);
    }

    // 축에 맞춘 네 꼭짓점으로 기의 핵과 섬광을 그린다.
    private void Diamond(Vector2 center, float radius, Color color)
    {
        Quad(center + Vector2.left * radius, center + Vector2.up * radius,
            center + Vector2.right * radius, center + Vector2.down * radius, color);
    }

    // 색이 같은 두 삼각형을 메시 버퍼에 추가한다.
    private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        int start = _vertices.Count;
        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _vertices.Add(d);
        for (int i = 0; i < 4; i++)
        {
            _vertexColors.Add(color);
        }
        _triangles.Add(start);
        _triangles.Add(start + 1);
        _triangles.Add(start + 2);
        _triangles.Add(start);
        _triangles.Add(start + 2);
        _triangles.Add(start + 3);
    }

    // 각 입자의 배치를 재현할 수 있도록 프레임과 독립된 값을 반환한다.
    private static float Seed(int index) => Mathf.Repeat(Mathf.Sin(index * 71.971f + 13.75f) * 49731.776f, 1f);
    // 두 축의 단위 방향 벡터를 구한다.
    private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    // 확산·충격파가 출발 직후 빠르게 움직이고 점차 멈추도록 한다.
    private static float Ease(float progress)
    {
        float remaining = 1f - Mathf.Clamp01(progress);
        return 1f - remaining * remaining * remaining;
    }

    // 같은 색을 유지하고 현재 재생 구간의 투명도를 적용한다.
    private static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

    // 각성 연출이 끝나면 런타임 렌더링 자원을 해제한다.
    private void OnDestroy()
    {
        Destroy(_mesh);
        Destroy(_material);
    }
}
