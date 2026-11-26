using UnityEngine;

// 각성기의 인트로와 속성별 화면 연출을 관리한다.
public sealed class BattleAwakeningEffect : MonoBehaviour
{
    // 컷신과 전장 효과의 재생 시간을 피해 처리와 공유한다.
    public const float CUTSCENE_DURATION = .5f;
    public const float EFFECT_DURATION = 4f;

    // 속성별 연출은 기존 5초 시간축을 사용하므로 재생 속도로 전체 길이를 맞춘다.
    private const float EFFECT_TIMELINE_DURATION = 5f;
    private const float EFFECT_PLAYBACK_SPEED = EFFECT_TIMELINE_DURATION / EFFECT_DURATION;

    // 기존 전투 이펙트보다 위에서 어둠, 파티클, 컷신을 순서대로 합성한다.
    private const int SHADE_SORT_ORDER = 90;
    private const int PARTICLE_SORT_ORDER = 95;
    private const int CUTSCENE_SORT_ORDER = 100;

    // 무, 화, 수, 목, 토, 풍 순서의 각성기 강조색이다.
    private static readonly Color[] _colors =
    {
        new(.68f, .4f, 1f), new(1f, .26f, .22f), new(.13f, .82f, 1f),
        new(.45f, .94f, .32f), new(1f, .64f, .29f), new(.7f, 1f, .86f)
    };

    // 카메라의 표시 범위를 덮는 어둠 레이어다.
    private SpriteRenderer _shade;
    // 컷신 스프라이트를 표시하는 렌더러다.
    private SpriteRenderer _portrait;
    // 컷신의 스프라이트 키프레임을 재생한다.
    private Animator _cutsceneAnimator;
    private static readonly int _cutsceneState = Animator.StringToHash("Base Layer.Cutscene");
    // 기존 스킬 스프라이트를 사용하는 속성 파티클이다.
    private ParticleSystem _particles;
    // 진기폭발만 사용하는 기 흡수와 동시 폭발 연출이다.
    private BattleEnergyBurstEffect _energyBurst;
    // 유성낙화 전용 소환과 충돌 연출이다.
    private BattleMeteorFallEffect _meteorFall;
    // 창해범람 전용 범람과 수룡 돌진 연출이다.
    private BattleOceanOverflowEffect _oceanOverflow;
    private BattleThornBloomEffect _thornBloom;

    // 가시만개는 소멸 전에 열매가 터지는 프레임에서 타격한다.
    internal float ImpactAge => CUTSCENE_DURATION + (_thornBloom != null
        ? BattleThornBloomEffect.BURST_TIME / EFFECT_PLAYBACK_SPEED : EFFECT_DURATION);
    // 비교 옵션으로 선택한 두 기술의 파티클 버전이다.
    private BattleAwakeningParticleEffect _particleVariant;
    private Material _particleTemplate;
    private Transform _caster;

    // 카메라 추적 위치에 더할 마지막 폭발의 짧은 흔들림이다.
    public Vector2 CameraOffset { get; private set; }
    // 화면 전체에 잔여 에너지를 방출하는 다음 시점이다.
    private float _nextAmbientPulse;
    // 어둠 레이어를 위한 공용 흰색 텍스처의 스프라이트다.
    private Sprite _pixel;
    // 장면의 월드 좌표와 화면 크기를 결정하는 카메라다.
    private Camera _camera;
    // 현재 각성기의 종류와 색이다.
    private int _skillId;
    // 입자 렌더러에 전용 텍스처를 지정하기 위한 머티리얼이다.
    private Material _particleMaterial;
    private Color _tint;

    // 전투 카메라 안에 인트로와 파티클을 준비한다.
    public static BattleAwakeningEffect Create(Transform parent, Camera camera, RuntimeAnimatorController cutsceneController, SpriteRenderer playerView,
        Sprite particle, Material material, int skillId, bool useParticleSystem = false)
    {
        var effect = new GameObject("Awakening_presentation").AddComponent<BattleAwakeningEffect>();
        effect.transform.SetParent(parent, false);
        effect._camera = camera;
        effect._caster = playerView.transform;
        effect._skillId = skillId;
        effect._tint = _colors[skillId - 19];

        // 기존 이미지를 재사용하고 화면을 덮는 막은 흰색 텍스처로 만든다.
        Texture2D white = Texture2D.whiteTexture;
        effect._pixel = Sprite.Create(white, new Rect(0, 0, white.width, white.height),
            new Vector2(.5f, .5f), white.width);
        effect._shade = effect.CreateView("Awakening_shade", effect._pixel, SHADE_SORT_ORDER);
        effect._portrait = effect.CreateView("Awakening_portrait", playerView.sprite, CUTSCENE_SORT_ORDER);
        if (cutsceneController != null)
        {
            effect._cutsceneAnimator = effect._portrait.gameObject.AddComponent<Animator>();
            effect._cutsceneAnimator.runtimeAnimatorController = cutsceneController;
            effect._cutsceneAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            effect._cutsceneAnimator.speed = 0f;
        }
        if (useParticleSystem && material != null && skillId is 19 or 20)
        {
            var variant = new GameObject("Awakening_Particle_System");
            variant.transform.SetParent(effect.transform, false);
            effect._particleVariant = variant.AddComponent<BattleAwakeningParticleEffect>();
            effect._particleTemplate = material;
            if (skillId == 19)
            {
                effect._particleVariant.ConfigureEnergy(effect._pixel, material, PARTICLE_SORT_ORDER);
            }
        }
        else if (skillId == 19)
        {
            var energy = new GameObject("Kage_energy_burst");
            energy.transform.SetParent(effect.transform, false);
            effect._energyBurst = energy.AddComponent<BattleEnergyBurstEffect>();
            effect._energyBurst.Initialize(PARTICLE_SORT_ORDER);
        }
        else if (skillId is not (20 or 22) && particle != null && material != null)
        {
            effect.CreateParticles(particle, material);
        }

        effect.Render(0f, 0f);
        return effect;
    }

    // 반복 시전 때 입자와 카메라 상태를 초기화하고 컷신부터 시작한다.
    internal void BeginCast(Sprite portrait)
    {
        CameraOffset = Vector2.zero;
        _nextAmbientPulse = 0f;
        if (_cutsceneAnimator == null)
            _portrait.sprite = portrait;
        if (_particles != null)
            _particles.Clear(false);

        Vector2 impact = _camera.transform.position;
        _meteorFall?.SetImpactPoint(impact);
        _particleVariant?.SetImpactPoint(impact);
        if (_meteorFall != null) _meteorFall.gameObject.SetActive(false);
        if (_oceanOverflow != null) _oceanOverflow.gameObject.SetActive(false);
        if (_thornBloom != null)
        {
            _thornBloom.BeginCast();
            _thornBloom.gameObject.SetActive(false);
        }
        if (_energyBurst != null) _energyBurst.gameObject.SetActive(false);
        if (_particleVariant != null) _particleVariant.gameObject.SetActive(false);
        Render(0f, 0f);
    }

    // 유성낙화 전용 시트의 여섯 스프라이트를 연출에 연결한다.
    public void ConfigureMeteor(Sprite scroll, Sprite smoke, Sprite meteorBody, Sprite debris, Sprite[] groundFire, Sprite shockwave, BattleMap map)
    {
        if (_particleVariant != null)
        {
            _particleVariant.ConfigureMeteor(scroll, smoke, meteorBody, debris, groundFire, shockwave, _pixel,
                _particleTemplate, PARTICLE_SORT_ORDER, map, _camera.transform.position);
            return;
        }


        var meteor = new GameObject("Kunoichi_meteor_fall");
        meteor.transform.SetParent(transform, false);
        _meteorFall = meteor.AddComponent<BattleMeteorFallEffect>();
        _meteorFall.Initialize(scroll, smoke, meteorBody, debris, groundFire, shockwave, _pixel,
            PARTICLE_SORT_ORDER, map.transform.position, map.HalfExtent, _camera.transform.position);
    }


    // 풀을 생성할 때 창해범람의 물과 수룡 렌더러를 준비한다.
    internal void ConfigureOcean(Texture2D dragonSheet, BattleMap map, Texture2D surfaceTexture,
        Sprite whirlpoolSprite, Sprite eyeSprite)
    {
        var ocean = new GameObject("Ocean_overflow");
        ocean.transform.SetParent(transform, false);
        _oceanOverflow = ocean.AddComponent<BattleOceanOverflowEffect>();
        _oceanOverflow.Initialize(dragonSheet, _pixel, map, PARTICLE_SORT_ORDER, surfaceTexture, whirlpoolSprite,
            eyeSprite);
        ocean.SetActive(false);
    }

    // 넝쿨과 가시 두 메시를 각성기 풀에서 한 번 준비한다.
    internal void ConfigureThornBloom(BattleMap map)
    {
        var thorn = new GameObject("Thorn_bloom");
        thorn.transform.SetParent(transform, false);
        _thornBloom = thorn.AddComponent<BattleThornBloomEffect>();
        _thornBloom.Initialize(map, PARTICLE_SORT_ORDER);
        thorn.SetActive(false);
    }

    // 어둠과 인트로 캐릭터는 카메라 중앙의 같은 월드 위치를 사용한다.
    private SpriteRenderer CreateView(string viewName, Sprite sprite, int sortingOrder)
    {
        var view = new GameObject(viewName).AddComponent<SpriteRenderer>();
        view.transform.SetParent(transform, false);
        view.sprite = sprite;
        view.sortingOrder = sortingOrder;
        return view;
    }

    // 원본 스프라이트의 아틀라스 좌표를 사용하도록 파티클을 설정한다.
    private void CreateParticles(Sprite sprite, Material material)
    {
        var emitter = new GameObject("Awakening_particles");
        emitter.SetActive(false);
        emitter.transform.SetParent(transform, false);
        _particles = emitter.AddComponent<ParticleSystem>();
        var main = _particles.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;
        var emission = _particles.emission;
        emission.enabled = false;
        var shape = _particles.shape;
        shape.enabled = false;
        var sheet = _particles.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Sprites;
        sheet.AddSprite(sprite);
        var renderer = _particles.GetComponent<ParticleSystemRenderer>();
        _particleMaterial = new Material(material) { mainTexture = sprite.texture };
        renderer.sharedMaterial = _particleMaterial;
        renderer.sortingOrder = PARTICLE_SORT_ORDER;
        emitter.SetActive(true);
    }

    // 전투 시간에 맞춰 인트로, 어둠 및 파티클 움직임을 갱신한다.
    public void Render(float age, float delta)
    {
        // 카메라 위치나 해상도가 변해도 화면 끝까지 덮는다.
        Vector3 center = _camera.transform.position;
        center.z = 0f;
        transform.position = center;
        float height = _camera.orthographicSize * 2f;
        float width = height * _camera.aspect;
        _shade.transform.localScale = new Vector3(width + .1f, height + .1f, 1f);
        float effectElapsed = Mathf.Max(0f, age - CUTSCENE_DURATION);
        float effectAge = effectElapsed * EFFECT_PLAYBACK_SPEED;
        // 컷신 뒤로 전투 화면을 그대로 보여주고 전장 이펙트에서만 어둡게 한다.
        float darkness = age < CUTSCENE_DURATION ? 0f
            : .60f * Mathf.Clamp01((EFFECT_TIMELINE_DURATION - effectAge) * 3f);
        // 지면 불꽃의 색이 암막에 묻히지 않도록 유성낙화의 후반 밝기를 되돌린다.
        if (_skillId == 20)
        {
            darkness *= 1f - .42f * Mathf.SmoothStep(0f, 1f, (effectAge - 1.8f) / .5f);
        }
        if (_oceanOverflow != null || _thornBloom != null) darkness *= .28f;
        _shade.color = new Color(.018f, .022f, .045f, darkness);

        RenderCutscene(age, width, height);

        // 진기폭발은 컷신 다음 프레임부터 플레이어 위치에서 별도 연출을 시작한다.
        if (_particleVariant != null && age >= CUTSCENE_DURATION)
        {
            _particleVariant.gameObject.SetActive(true);
            CameraOffset = _particleVariant.Render(effectAge, _caster.position, center, width, height);
        }
        else if (_energyBurst != null && age >= CUTSCENE_DURATION)
        {
            _energyBurst.gameObject.SetActive(true);
            _energyBurst.Render(effectAge, _caster.position - center, width, height);
            CameraOffset = _energyBurst.GetCameraOffset(effectAge);
        }
        else if (_meteorFall != null && age >= CUTSCENE_DURATION)
        {
            _meteorFall.gameObject.SetActive(true);
            _meteorFall.Render(effectAge, width, height);
            CameraOffset = _meteorFall.GetCameraOffset(effectAge);
        }


        // 전투 진행 시간으로만 입자를 갱신해 일시정지와 동기화한다.
        if (_oceanOverflow != null && age >= CUTSCENE_DURATION)
        {
            _oceanOverflow.gameObject.SetActive(true);
            _oceanOverflow.Render(effectAge, _caster.position, center, width, height);
        }

        if (_thornBloom != null && age >= CUTSCENE_DURATION)
        {
            _thornBloom.gameObject.SetActive(true);
            _thornBloom.Render(effectAge, _caster.position);
        }

        if (_oceanOverflow == null && _particles != null && delta > 0f)
        {
            // 단발형도 잔여 충격파와 파편은 5초 연출 끝까지 유지한다.
            if (age >= CUTSCENE_DURATION && effectAge >= _nextAmbientPulse && effectAge < EFFECT_TIMELINE_DURATION - .2f)
            {
                EmitParticles(Mathf.FloorToInt(effectAge), _nextAmbientPulse == 0f ? 32 : 12);
                _nextAmbientPulse = effectAge + .48f;
            }

            _particles.Simulate(Mathf.Min(delta, effectElapsed) * EFFECT_PLAYBACK_SPEED, false, false, false);
        }
    }

    // 컷신 시트의 프레임을 화면 가운데에 순서대로 표시한다.
    private void RenderCutscene(float age, float width, float height)
    {
        _portrait.enabled = age < CUTSCENE_DURATION;
        if (!_portrait.enabled)
        {
            return;
        }

        // 전투 시계를 Animator에 전달해 일시정지 및 컷신 종료와 동기화한다.
        if (_cutsceneAnimator != null)
        {
            _cutsceneAnimator.Play(_cutsceneState, 0, age / CUTSCENE_DURATION);
            _cutsceneAnimator.Update(0f);
        }

        // Animator가 표시한 프레임의 크기와 피벗을 화면에 맞춘다.
        Sprite frame = _portrait.sprite;
        if (frame == null)
        {
            return;
        }
        Bounds bounds = frame.bounds;
        float scale = Mathf.Min(width / bounds.size.x, height / bounds.size.y);
        _portrait.transform.localScale = Vector3.one * scale;
        _portrait.transform.localPosition = -bounds.center * scale;
    }

    // 중심과 방향을 속성에 맞춰 배치하고 지정된 수의 입자를 방출한다.
    private void EmitParticles(int pulse, int count)
    {
        if (_particles == null)
        {
            return;
        }

        float height = _camera.orthographicSize;
        float width = height * _camera.aspect;
        for (int i = 0; i < count; i++)
        {
            float progress = i / (float)(count - 1);
            Vector2 ray = new(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f));
            Vector2 start = Vector2.zero;
            Vector2 velocity = ray * 8f;
            switch (_skillId)
            {
                case 20: // 불타는 유성이 화면 위에서 사선으로 떨어진다.
                    start = new Vector2(Mathf.Lerp(-width, width * 1.6f, progress), height);
                    velocity = new Vector2(-3f, -11f - i % 5);
                    break;
                case 21: // 파도가 화면 왼쪽에서 오른쪽으로 밀려온다.
                    start = new Vector2(-width - i % 4, Mathf.Lerp(-height, height, progress));
                    velocity = new Vector2(14f, Mathf.Sin(i) * 1.8f);
                    break;
                case 22: // 가시가 여러 위치에서 위로 솟아오른다.
                    start = new Vector2(Mathf.Lerp(-width, width, progress), Mathf.Sin(i * 2.4f) * height);
                    velocity = new Vector2(0f, 3f + i % 4);
                    break;
                case 23: // 충격점에서 바위 파편이 사방으로 튄다.
                    start = ray * .4f;
                    velocity = ray * (4f + i % 7);
                    break;
                case 24: // 회오리의 접선을 따라 파편과 바람을 위로 띄운다.
                    start = ray * (.6f + pulse * .5f + i % 5 * .5f);
                    velocity = new Vector2(-ray.y, ray.x) * 6f + Vector2.up * 3f;
                    break;
            }

            var particle = new ParticleSystem.EmitParams
            {
                position = transform.position + (Vector3)start,
                velocity = velocity,
                startColor = Color.Lerp(_tint, Color.white, (i % 4) * .2f),
                startSize = .32f + i % 7 * .12f,
                startLifetime = .6f + i % 5 * .16f,
                rotation = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg
            };
            _particles.Emit(particle, 1);
        }
    }

    // 씬을 떠날 때 임시 스프라이트를 해제한다.
    private void OnDestroy()
    {
        if (_particleMaterial != null)
        {
            Destroy(_particleMaterial);
        }
        if (_pixel != null)
        {
            Destroy(_pixel);
        }
    }
}
