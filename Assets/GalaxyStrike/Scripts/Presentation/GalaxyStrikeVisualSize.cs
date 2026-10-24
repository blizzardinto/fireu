using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Presentation
{
    public static class GalaxyStrikeVisualSize
    {
        // Fit the complete trimmed sprite inside the logical box without distortion.
        public static Vector3 Fit(Sprite sprite, Vector2 worldSize)
        {
            if (sprite == null) return Vector3.one;
            Vector2 size = sprite.bounds.size;
            float scale = Mathf.Min(worldSize.x / Mathf.Max(size.x, 0.001f), worldSize.y / Mathf.Max(size.y, 0.001f));
            return new Vector3(scale, scale, 1f);
        }

        public static Vector2 Grid(float width, float height) => new Vector2(width, height) * GameConfig.WORLD_UNIT_SCALE;

        public static Vector2 Bullet(BulletType type)
        {
            // Long luminous trails are decoration; bullet collision remains 1x1 grid.
            switch (type)
            {
                case BulletType.Laser: return new Vector2(0.2f, 0.6f);
                case BulletType.Homing: return new Vector2(0.2f, 0.35f);
                case BulletType.EnemyOrb:
                case BulletType.BossHelix: return new Vector2(0.2f, 0.2f);
                default: return new Vector2(0.16f, 0.3f);
            }
        }
    }
}
