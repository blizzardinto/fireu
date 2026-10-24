using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【灭世级星际母舰 BOSS · BossEnemy.cs】
    // 对应 C++ 原工程 BossEnemy：
    // 战术特点：全场最大舰体 (23x5)、三阶段形态演进、动态狂暴警报、释放全向旋转地狱弹幕。
    // ============================================================================

    public class BossEnemy : Enemy
    {
        private int   m_phase;        // 阶段：1: 基础双联, 2: 五向散射, 3: 狂暴螺旋弹幕
        private int   m_strafeDir;    // 横向巡弋方向 (+1 向右, -1 向左)
        private float m_spiralAngle;  // 狂暴阶段螺旋弹幕旋转角

        public BossEnemy(Vector2 pos, int maxHp = 450)
            : base(pos, new Vector2(0.0f, 6.0f), 23, 5, maxHp, GameColors.BrightWhite,
                   EnemyType.Boss, 10000)
        {
            m_phase = 1;
            m_strafeDir = 1;
            m_spiralAngle = 0.0f;
            m_shootInterval = 0.5f;
            m_shootTimer = 0.0f;

            m_modelLines.AddRange(new[]
            {
                "   /===============\\   ",
                " /---|    [[X]]    |---\\ ",
                "<====||  MOTHERSHIP ||====>",
                " \\---|    [O---O]   |---/ ",
                "   \\===vvvvvvvvv===/   "
            });
        }

        public int GetPhase()
        {
            float hpPercent = (float)m_hp / m_maxHp;
            if (hpPercent > 0.66f) return 1;
            if (hpPercent > 0.33f) return 2;
            return 3; // 狂暴三阶段
        }

        public int Phase => m_phase;

        public override void UpdateBehavior(float dt, Vector2 playerPos)
        {
            m_phase = GetPhase();

            // 进场：缓缓降落到战场顶部 Y = 3
            if (m_pos.y < 3.0f)
            {
                m_pos.y += 5.0f * dt;
                return;
            }

            // 左右平移巡弋
            float moveSpeed = (m_phase == 3) ? 18.0f : 12.0f;
            m_pos.x += m_strafeDir * moveSpeed * dt;

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

            // 阶段变色反馈
            if (m_phase == 1)
            {
                m_color = GameColors.BrightWhite;
            }
            else if (m_phase == 2)
            {
                m_color = GameColors.BrightYellow;
            }
            else
            {
                // 狂暴阶段红紫警报交替
                m_color = (Mathf.FloorToInt(m_age * 8) % 2 == 0) ? GameColors.BrightRed : GameColors.BrightMagenta;
            }
        }

        public override void Shoot(List<Bullet> bullets, Vector2 playerPos, float dt)
        {
            if (m_pos.y < 2.5f) return;

            m_shootTimer += dt;
            Vector2 center = new Vector2(m_pos.x + m_width * 0.5f, m_pos.y + m_height);

            if (m_phase == 1)
            {
                // 阶段一：双翼重炮 + 中心扩散光弹
                if (m_shootTimer >= 0.8f)
                {
                    m_shootTimer = 0.0f;

                    // 左翼与右翼直射
                    bullets.Add(new Bullet(
                        new Vector2(m_pos.x + 3.0f, m_pos.y + 4.0f),
                        new Vector2(0.0f, 22.0f), BulletOwner.Enemy,
                        BulletType.EnemyFast, 18, '|', GameColors.BrightCyan
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(m_pos.x + m_width - 4.0f, m_pos.y + 4.0f),
                        new Vector2(0.0f, 22.0f), BulletOwner.Enemy,
                        BulletType.EnemyFast, 18, '|', GameColors.BrightCyan
                    ));

                    // 中心三向散弹
                    bullets.Add(new Bullet(
                        center, new Vector2(-9.0f, 20.0f), BulletOwner.Enemy,
                        BulletType.EnemyOrb, 16, 'o', GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        center, new Vector2(0.0f, 22.0f), BulletOwner.Enemy,
                        BulletType.EnemyOrb, 18, 'o', GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        center, new Vector2(9.0f, 20.0f), BulletOwner.Enemy,
                        BulletType.EnemyOrb, 16, 'o', GameColors.BrightYellow
                    ));
                }
            }
            else if (m_phase == 2)
            {
                // 阶段二：五向散射 + 瞄准交叉火力
                if (m_shootTimer >= 0.6f)
                {
                    m_shootTimer = 0.0f;

                    // 5 向散弹
                    float[] angles = { -18.0f, -9.0f, 0.0f, 9.0f, 18.0f };
                    foreach (float a in angles)
                    {
                        bullets.Add(new Bullet(
                            center, new Vector2(a, 22.0f), BulletOwner.Enemy,
                            BulletType.EnemyOrb, 18, '*', GameColors.BrightYellow
                        ));
                    }

                    // 瞄准玩家的高速弹
                    Vector2 aim = (playerPos - center).NormalizedSafe();
                    bullets.Add(new Bullet(
                        center, aim * 26.0f, BulletOwner.Enemy,
                        BulletType.EnemyFast, 22, 'x', GameColors.BrightRed
                    ));
                }
            }
            else
            {
                // 阶段三 (狂暴极速)：螺旋地狱弹幕 + 全向狂轰！
                if (m_shootTimer >= 0.22f)
                {
                    m_shootTimer = 0.0f;
                    m_spiralAngle += 0.55f;

                    float speed = 20.0f;
                    // 4 向旋转喷射
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = m_spiralAngle + i * 1.5707f;
                        bullets.Add(new Bullet(
                            center,
                            new Vector2(Mathf.Cos(angle) * speed, Mathf.Abs(Mathf.Sin(angle)) * speed + 6.0f),
                            BulletOwner.Enemy,
                            BulletType.BossHelix,
                            20,
                            '@',
                            GameColors.BrightRed
                        ));
                    }
                }
            }
        }
    }
}
