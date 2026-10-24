using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【精英巡洋舰 · EliteCruiser.cs】
    // 对应 C++ 原工程 EliteCruiser：
    // 战术特点：高耐久，在中上方做平滑正弦悬停漂移，释放双向旋转螺旋弹幕。
    // ============================================================================

    public class EliteCruiser : Enemy
    {
        private float m_helixAngle; // 螺旋弹幕当前旋转弧度角

        public EliteCruiser(Vector2 pos)
            : base(pos, new Vector2(0.0f, 7.0f), 9, 3, 60, GameColors.BrightMagenta,
                   EnemyType.Elite, 1200)
        {
            m_helixAngle = 0.0f;
            m_shootInterval = 0.6f;
            m_shootTimer = 0.0f;

            m_modelLines.AddRange(new[]
            {
                "  /-o-\\  ",
                " |<=#=>| ",
                " \\=====/ "
            });
        }

        public override void UpdateBehavior(float dt, Vector2 playerPos)
        {
            if (m_pos.y < 8.0f)
            {
                m_pos.y += 8.0f * dt;
            }
            else
            {
                // 在中上方正弦悬停漂移
                m_pos.x += Mathf.Cos(m_age * 1.5f) * 12.0f * dt;
            }
        }

        public override void Shoot(List<Bullet> bullets, Vector2 playerPos, float dt)
        {
            m_shootTimer += dt;
            if (m_shootTimer >= m_shootInterval && m_pos.y >= 7.0f)
            {
                m_shootTimer = 0.0f;
                m_helixAngle += 0.7f;

                Vector2 center = new Vector2(m_pos.x + 4.0f, m_pos.y + 3.0f);
                float speed = 18.0f;

                // 旋转双向螺旋喷射
                bullets.Add(new Bullet(
                    center,
                    new Vector2(Mathf.Cos(m_helixAngle) * speed, Mathf.Sin(m_helixAngle) * speed),
                    BulletOwner.Enemy,
                    BulletType.BossHelix,
                    14,
                    '+',
                    GameColors.BrightMagenta
                ));
                bullets.Add(new Bullet(
                    center,
                    new Vector2(-Mathf.Cos(m_helixAngle) * speed, -Mathf.Sin(m_helixAngle) * speed),
                    BulletOwner.Enemy,
                    BulletType.BossHelix,
                    14,
                    '+',
                    GameColors.BrightCyan
                ));
            }
        }
    }
}
