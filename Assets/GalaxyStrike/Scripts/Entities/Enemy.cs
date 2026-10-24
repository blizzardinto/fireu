using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【敌机二级抽象基类 · Enemy.cs】
    // 对应 C++ 原工程 Enemy.h / Enemy.cpp：
    // 抽象敌机共性，运用模板方法模式统筹生命周期、屏幕出界判定与掉落物算法。
    // ============================================================================

    public abstract class Enemy : Entity
    {
        protected EnemyType    m_type;
        protected int          m_scoreValue;
        protected float        m_shootTimer;
        protected float        m_shootInterval;
        protected float        m_age;
        protected List<string> m_modelLines = new List<string>();

        protected Enemy(Vector2 pos, Vector2 vel, int width, int height, int hp, Color color,
                        EnemyType type, int scoreVal)
            : base(pos, vel, width, height, hp, color)
        {
            m_type = type;
            m_scoreValue = scoreVal;
            m_shootTimer = 0.0f;
            m_shootInterval = 2.0f;
            m_age = 0.0f;
        }

        // ------------------------------------------------------------------------
        // 【纯虚函数 Hook】：由各个派生敌机子类分别实现自己的特色 AI 和弹幕
        // ------------------------------------------------------------------------
        public abstract void UpdateBehavior(float dt, Vector2 playerPos);
        public abstract void Shoot(List<Bullet> bullets, Vector2 playerPos, float dt);

        // ------------------------------------------------------------------------
        // 【模板方法 (Template Method)】
        // ------------------------------------------------------------------------
        public void UpdateWithTarget(float dt, Vector2 playerPos)
        {
            if (!m_active) return;
            m_age += dt;

            // 1. 多态分发：调用具体派生类重写的战术移动算法
            UpdateBehavior(dt, playerPos);

            // 2. 基类统筹：执行屏幕边界检测
            CheckBoundaries();
        }

        public override void Update(float dt)
        {
            UpdateWithTarget(dt, m_pos);
        }

        protected void CheckBoundaries()
        {
            if (m_pos.y > GameConfig.PLAY_MAX_Y + 1 ||
                m_pos.x < GameConfig.PLAY_MIN_X - 10 ||
                m_pos.x > GameConfig.PLAY_MAX_X + 10)
            {
                m_active = false;
            }
        }

        /// <summary>
        /// 死亡掉落物资判定（工厂方法：根据敌机机型与概率，生成具体物资）
        /// </summary>
        public PowerUp TryDropLoot()
        {
            int r = GameRandom.Range(0, 100);
            Vector2 center = new Vector2(m_pos.x + m_width * 0.5f - 1.0f, m_pos.y + m_height * 0.5f);

            switch (m_type)
            {
                case EnemyType.Scout:
                    if (r < 14) return new PowerUp(center, PowerUpType.Score);
                    if (r < 20) return new PowerUp(center, PowerUpType.Firepower);
                    if (r < 24) return new PowerUp(center, PowerUpType.ShipExp);
                    break;

                case EnemyType.Gunship:
                    if (r < 20) return new PowerUp(center, PowerUpType.Firepower);
                    if (r < 35) return new PowerUp(center, PowerUpType.ShipExp);
                    if (r < 45) return new PowerUp(center, PowerUpType.Shield);
                    break;

                case EnemyType.Bomber:
                    if (r < 30) return new PowerUp(center, PowerUpType.Firepower);
                    if (r < 50) return new PowerUp(center, PowerUpType.Shield);
                    if (r < 65) return new PowerUp(center, PowerUpType.Bomb);
                    if (r < 80) return new PowerUp(center, PowerUpType.ShipExp);
                    break;

                case EnemyType.Elite:
                    if (r < 35) return new PowerUp(center, PowerUpType.Bomb);
                    if (r < 70) return new PowerUp(center, PowerUpType.ShipExp);
                    return new PowerUp(center, PowerUpType.Firepower);

                case EnemyType.Boss:
                    // Boss 必定爆出超级核心物资
                    if (r < 50) return new PowerUp(center, PowerUpType.Bomb);
                    return new PowerUp(center, PowerUpType.ShipExp);
            }
            return null;
        }

        // ------------------------------------------------------------------------
        // 【属性访问器】
        // ------------------------------------------------------------------------
        public EnemyType Type => m_type;
        public int ScoreValue => m_scoreValue;
        public bool IsBoss => m_type == EnemyType.Boss;
        public float Age => m_age;
        public IReadOnlyList<string> ModelLines => m_modelLines;
    }
}
