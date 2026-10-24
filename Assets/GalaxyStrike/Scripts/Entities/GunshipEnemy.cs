using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【突击强袭舰 · GunshipEnemy.cs】
    // 对应 C++ 原工程 GunshipEnemy：
    // 战术特点：进场后停留在屏幕上方指定高度，来回横向巡航扫射，向玩家精准双联开火。
    // ============================================================================

    public class GunshipEnemy : Enemy
    {
        private readonly float m_targetY;   // 停驻巡航的高度
        private int            m_strafeDir; // 横向移动方向 (+1 向右, -1 向左)

        public GunshipEnemy(Vector2 pos)
            : base(pos, new Vector2(0.0f, 10.0f), 5, 2, 8, GameColors.BrightGreen,
                   EnemyType.Gunship, 300)
        {
            m_targetY = 6.0f + GameRandom.Range(0, 8);
            m_strafeDir = (GameRandom.Range(0, 2) == 0) ? 1 : -1;
            m_shootInterval = 1.4f;
            m_shootTimer = 0.5f;

            m_modelLines.AddRange(new[]
            {
                " [v] ",
                "/---\\"
            });
        }

        public override void UpdateBehavior(float dt, Vector2 playerPos)
        {
            if (m_pos.y < m_targetY)
            {
                m_pos.y += 10.0f * dt;
            }
            else
            {
                // 进入横向来回巡航扫射模式
                m_pos.x += m_strafeDir * 14.0f * dt;
                if (m_pos.x < GameConfig.PLAY_MIN_X + 2)
                {
                    m_pos.x = GameConfig.PLAY_MIN_X + 2;
                    m_strafeDir = 1;
                }
                else if (m_pos.x + m_width > GameConfig.PLAY_MAX_X - 2)
                {
                    m_pos.x = GameConfig.PLAY_MAX_X - 2 - m_width;
                    m_strafeDir = -1;
                }
            }
        }

        public override void Shoot(List<Bullet> bullets, Vector2 playerPos, float dt)
        {
            m_shootTimer += dt;
            if (m_shootTimer >= m_shootInterval && m_pos.y >= m_targetY - 1.0f)
            {
                m_shootTimer = 0.0f;

                Vector2 center = new Vector2(m_pos.x + 2.0f, m_pos.y + 2.0f);
                Vector2 aim = (playerPos - center).NormalizedSafe();
                float bSpeed = 22.0f;

                bullets.Add(new Bullet(
                    new Vector2(m_pos.x + 1.0f, m_pos.y + 2.0f),
                    aim * bSpeed,
                    BulletOwner.Enemy,
                    BulletType.EnemyFast,
                    15,
                    'o',
                    GameColors.BrightGreen
                ));
                bullets.Add(new Bullet(
                    new Vector2(m_pos.x + 3.0f, m_pos.y + 2.0f),
                    aim * bSpeed,
                    BulletOwner.Enemy,
                    BulletType.EnemyFast,
                    15,
                    'o',
                    GameColors.BrightGreen
                ));
            }
        }
    }
}
