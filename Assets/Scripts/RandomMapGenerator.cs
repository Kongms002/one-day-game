using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomMapGenerator : MonoBehaviour
{
    public Tilemap tilemap;     // 사용할 Tilemap
    public TileBase[] tiles;    // 팔레트 타일들

    public int width = 100;
    public int height = 100;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileBase tile = tiles[Random.Range(0, tiles.Length)];

                Vector3Int pos = new Vector3Int(x, y, 0);

                tilemap.SetTile(pos, tile);
            }
        }
    }
}