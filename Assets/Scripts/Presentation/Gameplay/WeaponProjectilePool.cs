using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public static class WeaponProjectilePool
    {
        private static readonly Queue<WeaponProjectileView> Pool = new Queue<WeaponProjectileView>();
        private static Transform s_root;

        public static void Spawn(
            Vector3 position,
            EnemyView target,
            float damage,
            float speed,
            float lifeTime,
            float hitRadius,
            float knockbackForce,
            int enemyMask,
            Color tint,
            Sprite sprite)
        {
            EnsureRoot();

            WeaponProjectileView projectile = null;
            while (Pool.Count > 0 && projectile == null)
            {
                projectile = Pool.Dequeue();
            }

            if (projectile == null)
            {
                var projectileObject = new GameObject("WeaponProjectile");
                projectileObject.transform.SetParent(s_root, false);
                projectile = projectileObject.AddComponent<WeaponProjectileView>();
                projectile.SetDestroyOnComplete(false);
            }

            projectile.transform.position = position;
            projectile.transform.rotation = Quaternion.identity;
            projectile.Completed -= OnProjectileCompleted;
            projectile.Completed += OnProjectileCompleted;
            projectile.Initialize(target, damage, speed, lifeTime, hitRadius, knockbackForce, enemyMask, tint, sprite);
        }

        private static void OnProjectileCompleted(WeaponProjectileView projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.Completed -= OnProjectileCompleted;
            projectile.transform.SetParent(s_root, false);
            Pool.Enqueue(projectile);
        }

        private static void EnsureRoot()
        {
            if (s_root != null)
            {
                return;
            }

            var root = GameObject.Find("ProjectileRoot") ?? new GameObject("ProjectileRoot");
            s_root = root.transform;
        }
    }
}
