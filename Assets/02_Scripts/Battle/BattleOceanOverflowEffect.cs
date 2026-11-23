using UnityEngine;

// 창해범람의 물과 수룡을 각성기 풀에서 한 번 준비한다.
internal sealed class BattleOceanOverflowEffect : MonoBehaviour
{
    private const int RIM_SEGMENTS = 96;
    private const int WHIRLPOOL_GRID = 7;
    private const float FLOOD_END = 1.05f;
    private const float WHIRLPOOL_START = 1.55f;
    private const float DRAGON_START = 2.85f;
    private const int DRAGON_COLUMNS = 4;
    private const int DRAGON_FRAME_COUNT = 16;
    private const int SHADOW_FRAME = 8;
    private const float LAST_FRAME_START = 4.86f;

    // 매 프레임 정점만 바꿔 범람 영역을 맵 경계 안에 유지한다.
    private readonly Vector3[] _vertices = new Vector3[1 + RIM_SEGMENTS * 2];
    private readonly Vector2[] _uvs = new Vector2[1 + RIM_SEGMENTS * 2];
    private readonly Color32[] _colors = new Color32[1 + RIM_SEGMENTS * 2];
    private readonly SpriteRenderer[] _whirlpools = new SpriteRenderer[WHIRLPOOL_GRID * WHIRLPOOL_GRID];

    // 겉물과 안쪽 포말을 다른 속도로 돌려 물이 중심으로 빨려드는 모양을 만든다.
    private readonly SpriteRenderer[] _innerWhirlpools = new SpriteRenderer[WHIRLPOOL_GRID * WHIRLPOOL_GRID];

    private Mesh _waterMesh;
    private Material _waterMaterial;
    private Texture2D _waterTexture;
    private Texture2D _whirlpoolTexture;
    private Sprite _whirlpoolSprite;
    private SpriteRenderer _shadow;
    private SpriteRenderer _leftEye;
    private SpriteRenderer _rightEye;
    private Texture2D _eyeTexture;
    private SpriteRenderer _dragon;
    // 그림자와 부상·회전·돌진 스프라이트를 전투 풀에서 공유한다.
    private readonly Sprite[] _dragonFrames = new Sprite[DRAGON_FRAME_COUNT];

    private SpriteRenderer _mouthOcclusion;
    private Vector2 _mapCenter;
    private float _mapHalfExtent;

    // 물 레이어와 소용돌이, 수룡의 세 장면을 풀 생성 시 준비한다.
    public void Initialize(Texture2D dragonSheet, Sprite pixel, BattleMap map, int order)
    {
        _mapCenter = map.transform.position;
        _mapHalfExtent = map.HalfExtent;
        PrepareWater(-20);

        _whirlpoolTexture = CreateWhirlpoolTexture();
        _whirlpoolSprite = Sprite.Create(_whirlpoolTexture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 128);
        for (int i = 0; i < _whirlpools.Length; i++)
        {
            _whirlpools[i] = CreateView("Ocean whirlpool water", _whirlpoolSprite, -19);
            _innerWhirlpools[i] = CreateView("Ocean whirlpool foam", _whirlpoolSprite, -18);
        }

        for (int frame = 0; frame < _dragonFrames.Length; frame++)
            _dragonFrames[frame] = ReadFrame(dragonSheet, frame);

        _shadow = CreateView("Submerged dragon", _dragonFrames[SHADOW_FRAME], order + 2);
        _eyeTexture = CreateEyeTexture();
        var eyeSprite = Sprite.Create(_eyeTexture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32);
        _leftEye = CreateView("Dragon left eye flash", eyeSprite, order + 3);
        _rightEye = CreateView("Dragon right eye flash", eyeSprite, order + 3);
        _dragon = CreateView("Animated water dragon", _dragonFrames[0], order + 5);
        _mouthOcclusion = CreateView("Water dragon mouth interior", pixel, order + 4);
    }

    // 전투 시계에서 현재 장면을 계산해 반복 시전에도 이전 프레임이 남지 않게 한다.
    public void Render(float age, Vector2 caster, Vector2 cameraCenter, float width, float height)
    {
        RenderWater(age, caster);
        RenderWhirlpools(age, caster);

        // 잠긴 그림자의 눈빛을 두 번 짧게 밝힌 뒤 부상 직전에 걷어낸다.
        float shadowAge = age - .8f;
        float eyeFlash = Mathf.Exp(-Mathf.Pow((age - 1.4f) / .12f, 2f))
            + .55f * Mathf.Exp(-Mathf.Pow((age - 1.67f) / .15f, 2f));
        float shadowAlpha = Ease(shadowAge / .52f) * (1f - Ease((age - DRAGON_START + .14f) / .57f));
        float shadowSize = Mathf.Min(width * 1.12f, height * .74f);
        Place(_shadow, cameraCenter, shadowSize, shadowAlpha * .71f);
        _shadow.color = new Color(.05f, .15f, .32f, shadowAlpha * .71f);
        float flash = shadowAlpha * Mathf.Clamp01(eyeFlash);
        float shadowHeight = _shadow.sprite.bounds.size.y * shadowSize;
        Place(_leftEye, cameraCenter + new Vector2(-.142f * shadowSize, -.119f * shadowHeight),
            shadowSize * .125f, flash);
        Place(_rightEye, cameraCenter + new Vector2(-.093f * shadowSize, -.108f * shadowHeight),
            shadowSize * .125f, flash);

        RenderDragon(age, cameraCenter, width, height);

    }

    // 전투 시계에 맞춰 프레임을 고르고 돌진 마지막 장면을 피해 판정까지 유지한다.
    private void RenderDragon(float age, Vector2 center, float width, float height)
    {
        if (age < DRAGON_START)
        {
            _dragon.enabled = false;
            _mouthOcclusion.enabled = false;
            return;
        }

        // 물을 뚫고 솟으며 원을 그리는 동작은 시트의 각 프레임에 직접 그려져 있다.
        float progress = Mathf.InverseLerp(DRAGON_START, LAST_FRAME_START, age);
        int frame = Mathf.Min(DRAGON_FRAME_COUNT - 1, Mathf.FloorToInt(progress * (DRAGON_FRAME_COUNT - 1)));
        _dragon.sprite = _dragonFrames[frame];

        // 원을 그릴 때 몸통이 보일 크기를 유지하고 정면 돌진이 시작되면 화면 크기까지 다가온다.
        float size = Mathf.Min(width * 1.19f, height * .84f);
        float closeUp = Ease((progress - .76f) / .24f);
        float screenSize = Mathf.Max(width, height) * 1.05f;
        Place(_dragon, center, Mathf.Lerp(size, screenSize, closeUp), 1f);

        // 마지막 입 프레임 뒤의 남색 레이어가 투명 가장자리에서도 전장을 덮는다.
        _mouthOcclusion.enabled = frame == DRAGON_FRAME_COUNT - 1;
        _mouthOcclusion.transform.position = center;
        _mouthOcclusion.transform.localScale = new Vector3(width + .1f, height + .1f, 1f);
        _mouthOcclusion.color = new Color(.016f, .036f, .14f, 1f);
    }

    // 수면 중앙, 거품 띠, 바깥 경계를 잇는 삼각형을 준비한다.
    private void PrepareWater(int order)
    {
        var host = new GameObject("Ocean surface");
        host.transform.SetParent(transform, false);
        _waterMesh = new Mesh { name = "Ocean flood mesh" };
        _waterMesh.MarkDynamic();
        _waterMesh.vertices = _vertices;
        var indices = new int[RIM_SEGMENTS * 9];
        for (int i = 0; i < RIM_SEGMENTS; i++)
        {
            int first = 1 + i * 2;
            int next = 1 + (i + 1) % RIM_SEGMENTS * 2;
            int index = i * 9;
            indices[index] = 0;
            indices[index + 1] = next;
            indices[index + 2] = first;
            indices[index + 3] = first;
            indices[index + 4] = next;
            indices[index + 5] = first + 1;
            indices[index + 6] = first + 1;
            indices[index + 7] = next;
            indices[index + 8] = next + 1;
        }
        _waterMesh.triangles = indices;

        _waterTexture = CreateWaterTexture();
        _waterMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = _waterTexture };
        host.AddComponent<MeshFilter>().sharedMesh = _waterMesh;
        var renderer = host.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _waterMaterial;
        renderer.sortingOrder = order;
    }

    // 물결 중심은 시전자에 고정하고, 가장 먼 맵 모서리까지 반지름을 늘린다.
    private void RenderWater(float age, Vector2 caster)
    {
        Vector2 extent = Vector2.one * _mapHalfExtent;
        Vector2 offset = caster - _mapCenter;
        float maxDistance = (extent + new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y))).magnitude;
        float radius = Mathf.Lerp(.05f, maxDistance * 1.12f, 1f - Mathf.Pow(1f - Mathf.Clamp01(age / FLOOD_END), 2f));
        Color32 water = new(85, 172, 233, 238);
        Color32 foam = new(229, 251, 255, 250);
        WriteWaterVertex(0, caster, water, age);
        for (int i = 0; i < RIM_SEGMENTS; i++)
        {
            float angle = i * (Mathf.PI * 2f / RIM_SEGMENTS);
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float swell = 1f + .035f * Mathf.Sin(angle * 7f - age * 12f);
            Vector2 edge = ClampToMap(caster + direction * radius * swell);
            Vector2 inner = ClampToMap(caster + direction * Mathf.Max(0f, radius * swell - .55f));
            WriteWaterVertex(1 + i * 2, inner, water, age);
            WriteWaterVertex(2 + i * 2, edge, foam, age);
        }
        _waterMesh.vertices = _vertices;
        _waterMesh.uv = _uvs;
        _waterMesh.colors32 = _colors;
        _waterMesh.RecalculateBounds();
    }

    // 월드 위치로 UV를 계산해 카메라가 움직여도 파도가 바닥에 붙어 보이게 한다.
    private void WriteWaterVertex(int index, Vector2 world, Color32 color, float age)
    {
        _vertices[index] = world - (Vector2)transform.position;
        _uvs[index] = world * .19f + new Vector2(age * .17f, age * -.09f);
        _colors[index] = color;
    }

    private Vector2 ClampToMap(Vector2 point) => new(
        Mathf.Clamp(point.x, _mapCenter.x - _mapHalfExtent, _mapCenter.x + _mapHalfExtent),
        Mathf.Clamp(point.y, _mapCenter.y - _mapHalfExtent, _mapCenter.y + _mapHalfExtent));

    // 맵에 고정된 소용돌이를 시전자와 가까운 곳부터 순차적으로 연다.
    private void RenderWhirlpools(float age, Vector2 caster)
    {
        float cell = _mapHalfExtent * 2f / WHIRLPOOL_GRID;
        for (int i = 0; i < _whirlpools.Length; i++)
        {
            Vector2 jitter = new(Mathf.Sin(i * 7.19f), Mathf.Cos(i * 4.33f));
            Vector2 point = _mapCenter - Vector2.one * _mapHalfExtent +
                (new Vector2(i % WHIRLPOOL_GRID + .5f, i / WHIRLPOOL_GRID + .5f) + jitter * .24f) * cell;
            float delay = Mathf.Clamp01(Vector2.Distance(caster, point) / (_mapHalfExtent * 2.8f)) * .9f;
            float open = Ease((age - WHIRLPOOL_START - delay) / .4f);
            SpriteRenderer view = _whirlpools[i];
            float life = Mathf.Max(0f, age - WHIRLPOOL_START - delay);
            float size = cell * (.86f + .22f * Mathf.Sin(i * 2.9f)) * open;
            float spin = i * 137.5f - (68f * life + 42f * life * life);
            Place(view, point, size, open * .89f);
            view.transform.rotation = Quaternion.Euler(0f, 0f, spin);

            // 안쪽 포말이 더 빠르게 감기고 크기가 맥동하며 깊이를 만든다.
            SpriteRenderer inner = _innerWhirlpools[i];
            float pulse = .56f + .06f * Mathf.Sin(life * 7f + i);
            Place(inner, point, size * pulse, open * .57f);
            inner.transform.rotation = Quaternion.Euler(0f, 0f, spin - 68f * life - 32f);
        }
    }

    // 수면을 빈틈 없이 타일링하도록 두 방향의 물결 무늬를 만든다.
    private static Texture2D CreateWaterTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        var pixels = new Color[size * size];
        Color deep = new(.055f, .29f, .57f);
        Color shallow = new(.49f, .93f, 1f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x * Mathf.PI * 2f / size;
            float v = y * Mathf.PI * 2f / size;
            float ridge = Mathf.Abs(Mathf.Sin(2f * u + Mathf.Sin(2f * v)) + Mathf.Cos(3f * v - .55f * Mathf.Sin(u)));
            float crest = Mathf.Pow(1f - Mathf.Clamp01(ridge), 7f);
            pixels[y * size + x] = Color.Lerp(deep, shallow, .15f + .68f * crest);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // 넓은 청록 물살 위로 흰 포말과 잔물결을 겹쳐 소용돌이 텍스처를 만든다.
    private static Texture2D CreateWhirlpoolTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new((x + .5f) / 64f - 1f, (y + .5f) / 64f - 1f);
            float radius = point.magnitude;
            float angle = Mathf.Atan2(point.y, point.x);
            float phase = angle * 3f + radius * 18f + .8f * Mathf.Sin(angle * 4f - radius * 12f);
            float current = .5f + .5f * Mathf.Cos(phase);
            float foam = Mathf.Pow(.5f + .5f * Mathf.Cos(phase + .65f), 6f);
            float ripples = .5f + .5f * Mathf.Sin(angle * 13f - radius * 41f);
            float edge = Ease((1f - radius) * 4.5f);
            float eye = Ease((radius - .07f) * 6f);
            float white = foam * (.58f + .42f * ripples);
            Color water = Color.Lerp(new Color(.015f, .15f, .39f), new Color(.09f, .78f, .94f), current * eye);
            Color color = Color.Lerp(water, new Color(.89f, .98f, 1f), white * eye);
            color.a = edge * (.55f + current * .26f + white * .19f);
            pixels[y * size + x] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // 시트에 그려진 눈 위로 빠르게 사라지는 청백색 빛을 얹는다.
    private static Texture2D CreateEyeTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new((x + .5f) / 16f - 1f, (y + .5f) / 16f - 1f);
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - point.magnitude), 3f);
            pixels[y * size + x] = new Color(.56f, .98f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // 위에서 아래로 읽는 시트의 행을 Unity 텍스처 좌표로 바꾼다.
    private static Sprite ReadFrame(Texture2D sheet, int index)
    {
        int cell = sheet.width / DRAGON_COLUMNS;
        int x = index % DRAGON_COLUMNS * cell;
        int y = sheet.height - (index / DRAGON_COLUMNS + 1) * cell;
        return Sprite.Create(sheet, new Rect(x, y, cell, cell), new Vector2(.5f, .5f), cell, 0, SpriteMeshType.FullRect);
    }

    private SpriteRenderer CreateView(string objectName, Sprite sprite, int order)
    {
        var view = new GameObject(objectName).AddComponent<SpriteRenderer>();
        view.transform.SetParent(transform, false);
        view.sprite = sprite;
        view.sortingOrder = order;
        view.enabled = false;
        return view;
    }

    private static void Place(SpriteRenderer view, Vector2 world, float size, float alpha)
    {
        view.enabled = alpha > .001f;
        if (!view.enabled) return;

        view.transform.position = world;
        view.transform.localScale = Vector3.one * size;
        view.color = new Color(1f, 1f, 1f, alpha);
    }

    private static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

    // 시전마다 그대로 재사용하고 전투 씬이 닫힐 때 런타임 리소스를 해제한다.
    private void OnDestroy()
    {
        if (_leftEye != null) Destroy(_leftEye.sprite);
        Destroy(_eyeTexture);
        foreach (Sprite frame in _dragonFrames) Destroy(frame);
        Destroy(_whirlpoolSprite);
        Destroy(_whirlpoolTexture);
        Destroy(_waterTexture);
        Destroy(_waterMaterial);
        Destroy(_waterMesh);
    }
}
