using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【重装轰炸机 · BomberEnemy.cs】
    // 对应 C++ 原工程 BomberEnemy：
    // 战术特点：重甲厚血，缓慢笔直向下压迫推进，释放三向下落重型散弹。
    // ============================================================================

    public class BomberEnemy : Enemy
    {
        public BomberEnemy(Vector2 pos)
            : base(pos, new Vector2(0.0f, 6.0f), 7, 3, 22, GameColors.BrightRed,
                   EnemyType.Bomber, 600)
        {
            m_shootInterval = 1.8f;
            m_shootTimer = 0.8f;

            m_modelLines.AddRange(new[]
            {
                " /===\\ ",
                "|<-o->|",
                " \\===/ "
            });
        }

        public override void UpdateBehavior(float dt, Vector2 playerPos)
        {
            m_pos.y += 6.0f * dt;

            if (m_pos.y > GameConfig.PLAY_MAX_Y + 1)
            {
                m_active = false;
            }
        }

        public override void Shoot(List<Bullet> bullets, Vector2 playerPos, float dt)
        {
            m_shootTimer += dt;
            if (m_shootTimer >= m_shootInterval)
            {
                m_shootTimer = 0.0f;

                Vector2 nose = new Vector2(m_pos.x + 3.0f, m_pos.y + 3.0f);
                // 三向下落散弹
                bullets.Add(new Bullet(
                    nose, new Vector2(-6.0f, 18.0f), BulletOwner.Enemy,
                    BulletType.EnemyOrb, 16, '*', GameColors.BrightRed
                ));
                bullets.Add(new Bullet(
                    nose, new Vector2(0.0f, 20.0f), BulletOwner.Enemy,
                    BulletType.EnemyOrb, 18, '*', GameColors.BrightYellow
                ));
                bullets.Add(new Bullet(
                    nose, new Vector2(6.0f, 18.0f), BulletOwner.Enemy,
                    BulletType.EnemyOrb, 16, '*', GameColors.BrightRed
                ));
            }
        }
    }
}
