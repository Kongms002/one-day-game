using OneDayGame.Domain.Policies;
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
        private int _propCountMin = 14;

        [SerializeField]
        private int _propCountMax = 26;

        private IMapPolicy _mapPolicy;
        private Transform _tileRoot;
        private Transform _propRoot;
        private int _appliedRound = -1;

        public void Initialize(IMapPolicy mapPolicy)
        {
            _mapPolicy = mapPolicy;
            EnsureRoots();
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

            int seed = _baseSeed + (round * 9973);
            var random = new System.Random(seed);

            Color floorA;
            Color floorB;
            Color propA;
            Color propB;
            GetPalette(round, out floorA, out floorB, out propA, out propB);

            float tileSize = Mathf.Max(0.4f, _tileSize);
            float minX = _mapPolicy.PlayerMinX - 3f;
            float maxX = _mapPolicy.PlayerMaxX + 3f;
            float minY = _mapPolicy.PlayerMinY - 2f;
            float maxY = _mapPolicy.PlayerMaxY + 3f;

            int xMin = Mathf.FloorToInt(minX / tileSize);
            int xMax = Mathf.CeilToInt(maxX / tileSize);
            int yMin = Mathf.FloorToInt(minY / tileSize);
            int yMax = Mathf.CeilToInt(maxY / tileSize);

            var tileSprite = RuntimeSpriteLibrary.GetSquare();
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.SetParent(_tileRoot, false);
                    tile.transform.position = new Vector3(x * tileSize, y * tileSize, 8f);

                    var renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = tileSprite;
                    renderer.sortingOrder = -300;
                    renderer.color = ((x + y + round) & 1) == 0 ? floorA : floorB;
                    tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);
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
