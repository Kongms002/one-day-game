using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    internal sealed class PooledViewFactory<TView> where TView : Component
    {
        private readonly TView _prefab;
        private readonly Transform _root;
        private readonly Queue<TView> _pool = new Queue<TView>();
        private readonly string _missingPrefabMessage;
        private bool _hasLoggedMissingPrefab;

        public PooledViewFactory(TView prefab, Transform root, string missingPrefabMessage)
        {
            _prefab = prefab;
            _root = root;
            _missingPrefabMessage = missingPrefabMessage;
        }

        public TView Spawn(Vector3 position)
        {
            if (_prefab == null)
            {
                if (!_hasLoggedMissingPrefab)
                {
                    Debug.LogError(_missingPrefabMessage);
                    _hasLoggedMissingPrefab = true;
                }

                return null;
            }

            TView view = null;
            while (_pool.Count > 0 && view == null)
            {
                view = _pool.Dequeue();
            }

            if (view == null)
            {
                view = Object.Instantiate(_prefab, position, Quaternion.identity, _root);
            }
            else
            {
                view.transform.SetParent(_root, false);
                view.transform.position = position;
                view.transform.rotation = Quaternion.identity;
                view.gameObject.SetActive(true);
            }

            return view;
        }

        public void Release(TView view)
        {
            if (view == null)
            {
                return;
            }

            view.gameObject.SetActive(false);
            view.transform.SetParent(_root, false);
            _pool.Enqueue(view);
        }

        public void ClearPool()
        {
            while (_pool.Count > 0)
            {
                var view = _pool.Dequeue();
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
        }
    }
}
