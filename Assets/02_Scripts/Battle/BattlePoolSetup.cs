using System.Collections;
using UnityEngine;

internal sealed class BattlePoolSetup
{
    private const int MONSTER_COUNT = 200;
    private const int EXPERIENCE_COLOR_COUNT = 5;
    private const int EXPERIENCE_COUNT_PER_COLOR = 50;
    private const int SILVER_COUNT = 20;
    private const int GOLD_COUNT = 10;
    private const int PREWARM_PER_FRAME = 20;

    private readonly ObjectPool<SpriteRenderer>[] _pickups = new ObjectPool<SpriteRenderer>[EXPERIENCE_COLOR_COUNT + 2];

    public ObjectPool<SpriteRenderer> Monsters { get; }
    public ObjectPool<SpriteRenderer> Chests { get; }

    public BattlePoolSetup(Transform root)
    {
        PoolManager manager = PoolManager.Instance;
        Monsters = manager.CreateScenePool(root, parent => CreateActor(parent, true), 0);
        Chests = manager.CreateScenePool(root, parent => CreateActor(parent, false), 0);
        for (int index = 0; index < _pickups.Length; index++)
        {
            _pickups[index] = manager.CreateScenePool(root, parent => CreateActor(parent, false), 0);
        }
    }

    public IEnumerator Prepare(int chestCount)
    {
        yield return Prewarm(Monsters, MONSTER_COUNT);
        yield return Prewarm(Chests, chestCount);
        for (int index = 0; index < EXPERIENCE_COLOR_COUNT; index++)
        {
            yield return Prewarm(_pickups[index], EXPERIENCE_COUNT_PER_COLOR);
        }
        yield return Prewarm(_pickups[EXPERIENCE_COLOR_COUNT], SILVER_COUNT);
        yield return Prewarm(_pickups[EXPERIENCE_COLOR_COUNT + 1], GOLD_COUNT);
    }

    public ObjectPool<SpriteRenderer> GetPickupPool(int type, int amount)
    {
        int index = type == 0 ? GetExperienceIndex(amount) : EXPERIENCE_COLOR_COUNT + type - 1;
        return _pickups[index];
    }

    public static int GetExperienceIndex(int amount) => amount switch
    {
        2 => 1,
        3 => 2,
        5 => 3,
        200 => 4,
        _ => 0
    };

    public void ReturnAll()
    {
        Monsters.ReturnAll();
        Chests.ReturnAll();
        foreach (ObjectPool<SpriteRenderer> pool in _pickups)
        {
            pool.ReturnAll();
        }
    }

    private static IEnumerator Prewarm(ObjectPool<SpriteRenderer> pool, int count)
    {
        while (pool.AvailableCount < count)
        {
            pool.Prewarm(Mathf.Min(pool.AvailableCount + PREWARM_PER_FRAME, count));
            yield return null;
        }
    }

    private static SpriteRenderer CreateActor(Transform parent, bool animated)
    {
        var actor = new GameObject("Pooled_Battle_Actor");
        actor.transform.SetParent(parent, false);
        if (animated)
        {
            actor.AddComponent<Animator>();
        }
        return actor.AddComponent<SpriteRenderer>();
    }
}
