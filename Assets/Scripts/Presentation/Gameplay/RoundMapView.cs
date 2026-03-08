using OneDayGame.Domain.Policies;
using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class RoundMapView : MonoBehaviour
    {
        private const int StagesPerRound = 10;

        [SerializeField]
        private int _baseSeed = 1741;

        [SerializeField]
        private float _tileSize = 1f;

        [SerializeField]
        private Texture2D _terrainTileSheet;

        [SerializeField]
        private int _sheetColumns = 2;

        [SerializeField]
        private int _sheetRows = 3;

        [SerializeField]
        private int _blockedTileIndex = 5;

        [SerializeField]
        private int _tileRenderWidthPixels = 30;

        [SerializeField]
        private int _tileRenderHeightPixels = 50;

        [SerializeField]
        private float _pixelsPerUnit = 100f;

        [SerializeField]
        private int _propCountMin = 14;

        [SerializeField]
        private int _propCountMax = 26;

        private IMapPolicy _mapPolicy;
        private Transform _tileRoot;
        private Transform _propRoot;
        private int _appliedRound = -1;
        private readonly List<Sprite> _sheetSprites = new List<Sprite>();
        private readonly HashSet<long> _blockedCells = new HashSet<long>();
        private Vector2 _tileStep = Vector2.one;
        private bool _hasGeneratedCells;
        private int _xMinCell;
        private int _xMaxCell;
        private int _yMinCell;
        private int _yMaxCell;

        public void Initialize(IMapPolicy mapPolicy)
        {
            _mapPolicy = mapPolicy;
            EnsureRoots();
        }

        public Vector2 GetTileStep()
        {
            if (_tileStep.x <= 0f || _tileStep.y <= 0f)
            {
                return Vector2.one;
            }

            return _tileStep;
        }

        public Vector2 SnapToTileCenter(Vector2 worldPosition)
        {
            var tileStep = GetTileStep();
            float x = Mathf.Round(worldPosition.x / tileStep.x) * tileStep.x;
            float y = Mathf.Round(worldPosition.y / tileStep.y) * tileStep.y;
            return new Vector2(x, y);
        }

        public Vector2 GetPlayableCenter()
        {
            if (_mapPolicy == null)
            {
                return Vector2.zero;
            }

            float centerX = (_mapPolicy.PlayerMinX + _mapPolicy.PlayerMaxX) * 0.5f;
            float centerY = (_mapPolicy.PlayerMinY + _mapPolicy.PlayerMaxY) * 0.5f;
            var center = SnapToTileCenter(new Vector2(centerX, centerY));
            if (IsWalkable(center))
            {
                return center;
            }

            int centerCellX = Mathf.RoundToInt(center.x / Mathf.Max(0.05f, _tileStep.x));
            int centerCellY = Mathf.RoundToInt(center.y / Mathf.Max(0.05f, _tileStep.y));
            for (int radius = 1; radius <= 24; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        int cellX = centerCellX + x;
                        int cellY = centerCellY + y;
                        if (cellX < _xMinCell || cellX > _xMaxCell || cellY < _yMinCell || cellY > _yMaxCell)
                        {
                            continue;
                        }

                        if (_blockedCells.Contains(ToCellKey(cellX, cellY)))
                        {
                            continue;
                        }

                        return new Vector2(cellX * _tileStep.x, cellY * _tileStep.y);
                    }
                }
            }

            return center;
        }

        public bool TryGetWorldBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (_mapPolicy == null || !_hasGeneratedCells)
            {
                minX = 0f;
                maxX = 0f;
                minY = 0f;
                maxY = 0f;
                return false;
            }

            float stepX = Mathf.Max(0.05f, _tileStep.x);
            float stepY = Mathf.Max(0.05f, _tileStep.y);
            minX = _xMinCell * stepX;
            maxX = _xMaxCell * stepX;
            minY = _yMinCell * stepY;
            maxY = _yMaxCell * stepY;
            return true;
        }

        public void ApplyForStage(int stage)
        {
            if (_mapPolicy == null)
            {
                return;
            }

            int stagesPerRound = Mathf.Max(1, StagesPerRound);
            int round = Mathf.Max(0, (Mathf.Max(1, stage) - 1) / stagesPerRound);
            if (round == _appliedRound)
            {
                return;
            }

            _appliedRound = round;
            Rebuild(round);
        }

        public void ResetToStage(int stage)
        {
            _appliedRound = -1;
            ApplyForStage(stage);
        }

        private void Rebuild(int round)
        {
            EnsureRoots();
            ClearRoot(_tileRoot);
            ClearRoot(_propRoot);
            _blockedCells.Clear();
            _hasGeneratedCells = false;
            BuildSheetSprites();

            int seed = _baseSeed + (round * 9973);
            var random = new System.Random(seed);

            Color floorA;
            Color floorB;
            Color propA;
            Color propB;
            GetPalette(round, out floorA, out floorB, out propA, out propB);

            float tileSize = Mathf.Max(0.4f, _tileSize);
            float tileStepX = tileSize;
            float tileStepY = tileSize;
            if (_terrainTileSheet != null)
            {
                int cols = Mathf.Max(1, _sheetColumns);
                int rows = Mathf.Max(1, _sheetRows);
                int tileW = _terrainTileSheet.width / cols;
                int tileH = _terrainTileSheet.height / rows;
                if (tileW > 0 && tileH > 0)
                {
                    tileStepX = Mathf.Max(0.05f, tileW / Mathf.Max(1f, _pixelsPerUnit));
                    tileStepY = Mathf.Max(0.05f, tileH / Mathf.Max(1f, _pixelsPerUnit));
                }
                else
                {
                    tileStepX = Mathf.Max(0.05f, _tileRenderWidthPixels / Mathf.Max(1f, _pixelsPerUnit));
                    tileStepY = Mathf.Max(0.05f, _tileRenderHeightPixels / Mathf.Max(1f, _pixelsPerUnit));
                }
            }

            _tileStep = new Vector2(tileStepX, tileStepY);
            float minX = _mapPolicy.PlayerMinX;
            float maxX = _mapPolicy.PlayerMaxX;
            float minY = _mapPolicy.PlayerMinY;
            float maxY = _mapPolicy.PlayerMaxY;

            int xMin = Mathf.FloorToInt(minX / tileStepX);
            int xMax = Mathf.CeilToInt(maxX / tileStepX);
            int yMin = Mathf.FloorToInt(minY / tileStepY);
            int yMax = Mathf.CeilToInt(maxY / tileStepY);
            _xMinCell = xMin;
            _xMaxCell = xMax;
            _yMinCell = yMin;
            _yMaxCell = yMax;
            _hasGeneratedCells = true;
            int centerX = Mathf.RoundToInt(((_mapPolicy.PlayerMinX + _mapPolicy.PlayerMaxX) * 0.5f) / tileStepX);
            int centerY = Mathf.RoundToInt(((_mapPolicy.PlayerMinY + _mapPolicy.PlayerMaxY) * 0.5f) / tileStepY);
            const int safeSpawnRadius = 2;

            var tileSprite = RuntimeSpriteLibrary.GetSquare();
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.SetParent(_tileRoot, false);
                    tile.transform.position = new Vector3(x * tileStepX, y * tileStepY, 8f);

                    var renderer = tile.AddComponent<SpriteRenderer>();
                    int tileIndex = _sheetSprites.Count > 0 ? random.Next(0, _sheetSprites.Count) : 0;
                    bool inSafeSpawnZone = Mathf.Abs(x - centerX) <= safeSpawnRadius && Mathf.Abs(y - centerY) <= safeSpawnRadius;
                    if (inSafeSpawnZone && tileIndex == _blockedTileIndex)
                    {
                        tileIndex = 0;
                    }

                    if (_sheetSprites.Count > 0)
                    {
                        renderer.sprite = _sheetSprites[tileIndex];
                        renderer.color = Color.white;
                        tile.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        renderer.sprite = tileSprite;
                        renderer.color = ((x + y + round) & 1) == 0 ? floorA : floorB;
                        tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);
                    }

                    renderer.sortingOrder = -300;
                    if (!inSafeSpawnZone && tileIndex == _blockedTileIndex)
                    {
                        _blockedCells.Add(ToCellKey(x, y));
                    }
                }
            }

            int propCount = random.Next(Mathf.Max(1, _propCountMin), Mathf.Max(_propCountMin + 1, _propCountMax + 1));
            var circleSprite = RuntimeSpriteLibrary.GetCircle();
            var diamondSprite = RuntimeSpriteLibrary.GetDiamond();

            for (int i = 0; i < propCount; i++)
            {
                float px = Mathf.Lerp(minX, maxX, (float)random.NextDouble());
                float py = Mathf.Lerp(minY, maxY, (float)random.NextDouble());

                var prop = new GameObject($"Prop_{i}");
                prop.transform.SetParent(_propRoot, false);
                prop.transform.position = new Vector3(px, py, 6.5f + (i % 3) * 0.01f);

                var renderer = prop.AddComponent<SpriteRenderer>();
                renderer.sprite = (i & 1) == 0 ? circleSprite : diamondSprite;
                renderer.sortingOrder = -240 + (i % 3);
                renderer.color = Color.Lerp(propA, propB, (float)random.NextDouble());

                float sx = Mathf.Lerp(0.35f, 1.05f, (float)random.NextDouble());
                float sy = Mathf.Lerp(0.35f, 1.2f, (float)random.NextDouble());
                prop.transform.localScale = new Vector3(sx, sy, 1f);
            }
        }

        public bool IsWalkable(Vector2 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / Mathf.Max(0.05f, _tileStep.x));
            int y = Mathf.RoundToInt(worldPosition.y / Mathf.Max(0.05f, _tileStep.y));
            if (x < _xMinCell || x > _xMaxCell || y < _yMinCell || y > _yMaxCell)
            {
                return false;
            }

            return !_blockedCells.Contains(ToCellKey(x, y));
        }

        private void BuildSheetSprites()
        {
            _sheetSprites.Clear();
            if (_terrainTileSheet == null)
            {
                return;
            }

            int cols = Mathf.Max(1, _sheetColumns);
            int rows = Mathf.Max(1, _sheetRows);
            int tileW = _terrainTileSheet.width / cols;
            int tileH = _terrainTileSheet.height / rows;
            if (tileW <= 0 || tileH <= 0)
            {
                return;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = col * tileW;
                    int y = _terrainTileSheet.height - ((row + 1) * tileH);
                    var sprite = Sprite.Create(
                        _terrainTileSheet,
                        new Rect(x, y, tileW, tileH),
                        new Vector2(0.5f, 0.5f),
                        Mathf.Max(1f, _pixelsPerUnit));
                    _sheetSprites.Add(sprite);
                }
            }
        }

        private static long ToCellKey(int x, int y)
        {
            return ((long) x << 32) ^ (uint) y;
        }

        private void EnsureRoots()
        {
            if (_tileRoot == null)
            {
                var tileRoot = new GameObject("MapTiles");
                tileRoot.transform.SetParent(transform, false);
                _tileRoot = tileRoot.transform;
            }

            if (_propRoot == null)
            {
                var propRoot = new GameObject("MapProps");
                propRoot.transform.SetParent(transform, false);
                _propRoot = propRoot.transform;
            }
        }

        private static void ClearRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private static void GetPalette(int round, out Color floorA, out Color floorB, out Color propA, out Color propB)
        {
            switch (round % 4)
            {
                case 0:
                    floorA = new Color(0.17f, 0.25f, 0.19f, 1f);
                    floorB = new Color(0.13f, 0.2f, 0.15f, 1f);
                    propA = new Color(0.26f, 0.36f, 0.21f, 0.52f);
                    propB = new Color(0.18f, 0.29f, 0.16f, 0.52f);
                    break;
                case 1:
                    floorA = new Color(0.21f, 0.22f, 0.29f, 1f);
                    floorB = new Color(0.15f, 0.17f, 0.23f, 1f);
                    propA = new Color(0.31f, 0.32f, 0.44f, 0.52f);
                    propB = new Color(0.23f, 0.25f, 0.37f, 0.52f);
                    break;
                case 2:
                    floorA = new Color(0.3f, 0.24f, 0.18f, 1f);
                    floorB = new Color(0.24f, 0.19f, 0.14f, 1f);
                    propA = new Color(0.4f, 0.3f, 0.2f, 0.52f);
                    propB = new Color(0.32f, 0.24f, 0.16f, 0.52f);
                    break;
                default:
                    floorA = new Color(0.14f, 0.24f, 0.28f, 1f);
                    floorB = new Color(0.11f, 0.19f, 0.22f, 1f);
                    propA = new Color(0.2f, 0.31f, 0.35f, 0.52f);
                    propB = new Color(0.15f, 0.26f, 0.3f, 0.52f);
                    break;
            }
        }
    }
}
