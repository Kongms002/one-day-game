using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class ExpOrbFactory
    {
        private readonly PooledViewFactory<ExpOrbView> _factory;

        public ExpOrbFactory(ExpOrbView prefab, Transform root)
        {
            _factory = new PooledViewFactory<ExpOrbView>(
                prefab,
                root,
                "[OneDayGame] ExpOrb prefab is required for ExpOrbFactory.");
        }

        public ExpOrbView Spawn(Vector3 position)
        {
            return _factory.Spawn(position);
        }

        public void Release(ExpOrbView orb)
        {
            _factory.Release(orb);
        }

        public void ClearPool()
        {
            _factory.ClearPool();
        }
    }
}
