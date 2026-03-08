using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class EnemyFactory
    {
        private readonly EnemyView _prefab;
        private readonly Transform _root;
        private readonly Queue<EnemyView> _pool = new Queue<EnemyView>();
        private bool _hasLoggedMissingPrefab;

        public EnemyFactory(EnemyView prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
        }

        public EnemyView Spawn(Vector3 position)
        {
            if (_prefab == null)
            {
                if (!_hasLoggedMissingPrefab)
                {
                    Debug.LogError("[OneDayGame] Enemy prefab is required for EnemyFactory.");
                    _hasLoggedMissingPrefab = true;
                }

                return null;
            }

            EnemyView enemy = null;
            while (_pool.Count > 0 && enemy == null)
            {
                enemy = _pool.Dequeue();
            }

            if (enemy == null)
            {
                enemy = Object.Instantiate(_prefab, position, Quaternion.identity, _root);
            }
            else
            {
                enemy.transform.SetParent(_root, false);
                enemy.transform.position = position;
                enemy.transform.rotation = Quaternion.identity;
                enemy.gameObject.SetActive(true);
            }

            return enemy;
        }

        public void Release(EnemyView enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(_root, false);
            _pool.Enqueue(enemy);
        }

        public void ClearPool()
        {
            while (_pool.Count > 0)
            {
                var enemy = _pool.Dequeue();
                if (enemy != null)
                {
                    Object.Destroy(enemy.gameObject);
                }
            }
        }
    }
}
