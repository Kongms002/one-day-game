using System.Collections.Generic;
using OneDayGame.Domain.Weapons;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    internal static class WeaponSpriteLibrary
    {
        private const string WeaponSpriteRoot = "Weapons";
        private const string DefaultFallbackSheet = "00158436-fc2f-4da9-9b79-8fda94e692ab-Photoroom";

        private sealed class WeaponSpriteRef
        {
            public readonly string FileName;
            public readonly int SpriteIndex;

            public WeaponSpriteRef(string fileName, int spriteIndex)
            {
                FileName = fileName;
                SpriteIndex = spriteIndex;
            }
        }

        private static readonly Dictionary<WeaponId, WeaponSpriteRef> SpriteMap = new()
        {
            { WeaponId.Melee, new WeaponSpriteRef("00158436-fc2f-4da9-9b79-8fda94e692ab-Photoroom", 0) },
            { WeaponId.Arrow, new WeaponSpriteRef("64d41a45-fcee-469f-bdc2-12b3ae00d475-Photoroom", 0) },
            { WeaponId.Boomerang, new WeaponSpriteRef("93927e2f-fbde-4df7-8fec-a7c24b5f869b-Photoroom", 0) },
            { WeaponId.GroundZone, new WeaponSpriteRef("93fef009-7868-444f-846d-c098263c38ff-Photoroom", 0) },
            { WeaponId.Turret, new WeaponSpriteRef("b3fe58e7-05db-4cfd-9e79-59a70f85062e-Photoroom", 0) },
            { WeaponId.BlackHole, new WeaponSpriteRef("b3fe58e7-05db-4cfd-9e79-59a70f85062e-Photoroom", 0) },
            { WeaponId.PoisonCloud, new WeaponSpriteRef("93fef009-7868-444f-846d-c098263c38ff-Photoroom", 0) },
            { WeaponId.LaserPet, new WeaponSpriteRef("64d41a45-fcee-469f-bdc2-12b3ae00d475-Photoroom", 0) },
            { WeaponId.RagePet, new WeaponSpriteRef("93927e2f-fbde-4df7-8fec-a7c24b5f869b-Photoroom", 0) },
            { WeaponId.StunPet, new WeaponSpriteRef("00158436-fc2f-4da9-9b79-8fda94e692ab-Photoroom", 0) }
        };

        private static readonly Dictionary<string, Sprite[]> LoadedSheets = new();
        private static readonly Dictionary<WeaponId, Sprite> CachedWeaponSprites = new();
        private static readonly Dictionary<WeaponType, WeaponId> TypeFallbackWeapon = new()
        {
            { WeaponType.Rotation, WeaponId.Melee },
            { WeaponType.Projectile, WeaponId.Arrow },
            { WeaponType.Area, WeaponId.GroundZone },
            { WeaponType.Persistent, WeaponId.Turret }
        };

        public static Sprite GetWeaponIcon(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return GetFallbackIcon();
            }

            return GetWeaponIcon(definition.Id) ?? GetWeaponIcon(TypeFallbackWeapon[definition.Type]);
        }

        public static Sprite GetWeaponIcon(WeaponId weaponId)
        {
            if (CachedWeaponSprites.TryGetValue(weaponId, out var cached))
            {
                return cached;
            }

            if (!SpriteMap.TryGetValue(weaponId, out var source))
            {
                return GetFallbackIcon();
            }

            var sprite = ResolveSprite(source);
            CachedWeaponSprites[weaponId] = sprite;
            return sprite;
        }

        public static Sprite ResolveProjectileIcon(WeaponDefinition definition)
        {
            return GetWeaponIcon(definition);
        }

        public static Sprite ResolveAreaVisualIcon(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return GetFallbackIcon();
            }

            return GetWeaponIcon(definition.Id);
        }

        private static Sprite GetFallbackIcon()
        {
            if (CachedWeaponSprites.TryGetValue(WeaponId.Melee, out var cached) && cached != null)
            {
                return cached;
            }

            return ResolveSprite(new WeaponSpriteRef(DefaultFallbackSheet, 0));
        }

        private static Sprite ResolveSprite(WeaponSpriteRef source)
        {
            if (source == null || string.IsNullOrEmpty(source.FileName))
            {
                return null;
            }

            if (!LoadedSheets.TryGetValue(source.FileName, out var sprites) || sprites == null || sprites.Length == 0)
            {
                sprites = Resources.LoadAll<Sprite>($"{WeaponSpriteRoot}/{source.FileName}");
                LoadedSheets[source.FileName] = sprites;
            }

            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(source.SpriteIndex, 0, sprites.Length - 1);
            return sprites[index];
        }
    }
}
