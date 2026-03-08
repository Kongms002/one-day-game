using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomMapGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase[] tiles;

    public int stage = 1;

    public int width = 100;
    public int height = 100;

    [SerializeField]
    private int originX;

    [SerializeField]
    private int originY;

    void Start()
    {
        GenerateMap();
    }

    public void ConfigurePlayableBounds(float minX, float maxX, float minY, float maxY)
    {
        int xMin = Mathf.FloorToInt(Mathf.Min(minX, maxX));
        int xMax = Mathf.CeilToInt(Mathf.Max(minX, maxX));
        int yMin = Mathf.FloorToInt(Mathf.Min(minY, maxY));
        int yMax = Mathf.CeilToInt(Mathf.Max(minY, maxY));

        width = Mathf.Max(1, xMax - xMin + 1);
        height = Mathf.Max(1, yMax - yMin + 1);
        originX = xMin;
        originY = yMin;
        GenerateMap();
    }

    public void UpdateStage(int newStage)
    {
        int normalizedStage = Mathf.Max(1, newStage);
        int biomeStage = Mathf.Clamp(((normalizedStage - 1) / 10) + 1, 1, 3);
        if (stage != biomeStage)
        {
            stage = biomeStage;
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        if (tilemap == null || tiles == null || tiles.Length == 0)
        {
            return;
        }

        tilemap.ClearAllTiles();

        int startIndex = 0;
        int endIndex = 13;

        if (stage == 2)
        {
            startIndex = 14;
            endIndex = 22;
        }
        else if (stage == 3)
        {
            startIndex = 23;
            endIndex = 29;
        }

        startIndex = Mathf.Clamp(startIndex, 0, tiles.Length - 1);
        endIndex = Mathf.Clamp(endIndex, startIndex, tiles.Length - 1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int randomIndex = Random.Range(startIndex, endIndex + 1);
                TileBase tile = tiles[randomIndex];

                tilemap.SetTile(new Vector3Int(originX + x, originY + y, 0), tile);
            }
        }
    }
}
