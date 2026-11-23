using UnityEngine;

public sealed class BattleMap : MonoBehaviour
{
    private const int TILE_COUNT = 10;
    [SerializeField, Min(.1f)] private float _tileSize = 2f;
    [SerializeField] private Sprite[] _floors;

    private SpriteRenderer[] _tiles;
    public float HalfExtent => TILE_COUNT * _tileSize * .5f;

    public void SetTerrain(int type)
    {
        if (_tiles == null)
        {
            _tiles = new SpriteRenderer[TILE_COUNT * TILE_COUNT];
            for (int y = 0; y < TILE_COUNT; y++)
            {
                for (int x = 0; x < TILE_COUNT; x++)
                {
                    GameObject cell = new($"Tile {x}, {y}");
                    cell.transform.SetParent(transform, false);
                    SpriteRenderer tile = cell.AddComponent<SpriteRenderer>();
                    tile.sortingOrder = -100;
                    _tiles[y * TILE_COUNT + x] = tile;
                }
            }
        }

        Sprite floor = _floors[Mathf.Clamp(type - 1, 0, _floors.Length - 1)];
        Vector2 size = floor.bounds.size;
        for (int i = 0; i < _tiles.Length; i++)
        {
            SpriteRenderer tile = _tiles[i];
            tile.sprite = floor;
            Vector3 scale = new(_tileSize / size.x, _tileSize / size.y, 1f);
            tile.transform.localScale = scale;
            tile.transform.localPosition = new Vector3((i % TILE_COUNT + .5f) * _tileSize - HalfExtent,
                (i / TILE_COUNT + .5f) * _tileSize - HalfExtent) - Vector3.Scale(floor.bounds.center, scale);
        }
    }

    public Vector2 ClampInside(Vector2 position, float margin)
    {
        float limit = Mathf.Max(0f, HalfExtent - margin);
        return new Vector2(Mathf.Clamp(position.x, -limit, limit), Mathf.Clamp(position.y, -limit, limit));
    }

    public Vector2 FollowPosition(Vector2 position, Camera camera)
    {
        float x = Mathf.Max(0f, HalfExtent - camera.orthographicSize * camera.aspect);
        float y = Mathf.Max(0f, HalfExtent - camera.orthographicSize);
        return new Vector2(Mathf.Clamp(position.x, -x, x), Mathf.Clamp(position.y, -y, y));
    }
}
