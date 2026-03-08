using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomMapGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase[] tiles;

    public int stage = 1;

    public int width = 100;
    public int height = 100;
    void Start()
    {
        GenerateMap();
    }

    public void UpdateStage(int newStage)
    {
        if (stage != newStage)
        {
            stage = newStage;
            GenerateMap();
        }
    }

    void GenerateMap()
    {
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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int randomIndex = Random.Range(startIndex, endIndex + 1);
                TileBase tile = tiles[randomIndex];

                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }
}