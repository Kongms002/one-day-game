using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public static class WeaponAreaEffectPool
    {
        private static readonly Queue<WeaponAreaEffectView> Pool = new Queue<WeaponAreaEffectView>();
        private static Transform s_root;

        public static void Spawn(Vector3 position, float radius, Color color)
        {
            EnsureRoot();
            WeaponAreaEffectView effect = null;
            while (Pool.Count > 0 && effect == null)
            {
                effect = Pool.Dequeue();
            }

            if (effect == null)
            {
                var go = new GameObject("WeaponAreaEffect");
                go.transform.SetParent(s_root, false);
                effect = go.AddComponent<WeaponAreaEffectView>();
                effect.SetDestroyOnComplete(false);
            }

            effect.transform.position = position;
            effect.Completed -= OnCompleted;
            effect.Completed += OnCompleted;
            effect.Initialize(radius, color);
        }

        private static void OnCompleted(WeaponAreaEffectView effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.Completed -= OnCompleted;
            effect.transform.SetParent(s_root, false);
            Pool.Enqueue(effect);
        }

        private static void EnsureRoot()
        {
            if (s_root != null)
            {
                return;
            }

            var root = GameObject.Find("WeaponAreaEffectRoot") ?? new GameObject("WeaponAreaEffectRoot");
            s_root = root.transform;
        }
    }
}
