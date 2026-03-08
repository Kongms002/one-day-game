using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class ExpOrbFactory
    {
        private readonly ExpOrbView _prefab;
        private readonly Transform _root;
        private readonly Queue<ExpOrbView> _pool = new Queue<ExpOrbView>();
        private bool _hasLoggedMissingPrefab;

        public ExpOrbFactory(ExpOrbView prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
        }

        public ExpOrbView Spawn(Vector3 position)
        {
            if (_prefab == null)
            {
                if (!_hasLoggedMissingPrefab)
                {
                    Debug.LogError("[OneDayGame] ExpOrb prefab is required for ExpOrbFactory.");
                    _hasLoggedMissingPrefab = true;
                }

                return null;
            }

            ExpOrbView orb = null;
            while (_pool.Count > 0 && orb == null)
            {
                orb = _pool.Dequeue();
            }

            if (orb == null)
            {
                orb = Object.Instantiate(_prefab, position, Quaternion.identity, _root);
            }
            else
            {
                orb.transform.SetParent(_root, false);
                orb.transform.position = position;
                orb.transform.rotation = Quaternion.identity;
                orb.gameObject.SetActive(true);
            }

            return orb;
        }

        public void Release(ExpOrbView orb)
        {
            if (orb == null)
            {
                return;
            }

            orb.gameObject.SetActive(false);
            orb.transform.SetParent(_root, false);
            _pool.Enqueue(orb);
        }

        public void ClearPool()
        {
            while (_pool.Count > 0)
            {
                var orb = _pool.Dequeue();
                if (orb != null)
                {
                    Object.Destroy(orb.gameObject);
                }
            }
        }
    }
}
