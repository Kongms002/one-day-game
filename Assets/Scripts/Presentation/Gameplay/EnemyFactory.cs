using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class EnemyFactory
    {
        private readonly PooledViewFactory<EnemyView> _factory;

        public EnemyFactory(EnemyView prefab, Transform root)
        {
            _factory = new PooledViewFactory<EnemyView>(
                prefab,
                root,
                "[OneDayGame] Enemy prefab is required for EnemyFactory.");
        }

        public EnemyView Spawn(Vector3 position)
        {
            return _factory.Spawn(position);
        }

        public void Release(EnemyView enemy)
        {
            _factory.Release(enemy);
        }

        public void ClearPool()
        {
            _factory.ClearPool();
        }
    }
}
