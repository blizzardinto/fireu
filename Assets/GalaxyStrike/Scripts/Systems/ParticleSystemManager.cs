using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Systems
{
    // ============================================================================
    // 【粒子与震荡波视觉系统 · ParticleSystemManager.cs】
    // 对应 C++ 原工程 ParticleSystem.h / ParticleSystem.cpp：
    // 负责爆炸碎片衰减、受创火花、尾翼喷焰与全屏 EMP 震荡波环演算。
    // ============================================================================

    public enum ParticleKind : byte
    {
        Spark,      // 飞溅火花 / 炽热星芒 (放射状稀疏飞溅)
        Fireball,   // 核心微型爆闪 (利落极速消退，无浓烟残留)
        Debris,     // 飞旋破片残骸 (少量微粒)
        Smoke,      // 弥散烟尘
        BranchFork  // 炸弹清屏双层环微型能量分叉 / 能量枝桠
    }

    public struct ParticleData
    {
        public Vector2      pos;
        public Vector2      vel;
        public char         ch;
        public Color        color;
        public float        life;
        public float        maxLife;
        public float        baseScale;
        public float        rotation;
        public float        rotSpeed;
        public ParticleKind kind;
    }

    public struct ShockwaveData
    {
        public Vector2 center;
        public float   currentRadius;
        public float   maxRadius;
        public float   expandSpeed;
        public Color   color;
    }

    public class ParticleSystemManager
    {
        private readonly List<ParticleData>  m_particles  = new List<ParticleData>(384);
        private readonly List<ShockwaveData> m_shockwaves = new List<ShockwaveData>(16);

        private static readonly char[] SparkSymbols = { '*', '+', 'o', 'x', '.' };

        public IReadOnlyList<ParticleData> Particles => m_particles;
        public IReadOnlyList<ShockwaveData> Shockwaves => m_shockwaves;

        public void Clear()
        {
            m_particles.Clear();
            m_shockwaves.Clear();
        }

        public void Update(float dt)
        {
            // 1. 更新粒子
            for (int i = m_particles.Count - 1; i >= 0; i--)
            {
                var p = m_particles[i];
                p.life -= dt;
                if (p.life <= 0.0f)
                {
                    m_particles.RemoveAt(i);
                    continue;
                }

                // 物理阻力与平滑飞旋演算 (利落干脆，衰减迅速)
                if (p.kind == ParticleKind.Spark)
                {
                    p.vel *= Mathf.Pow(0.12f, dt);
                }
                else if (p.kind == ParticleKind.Fireball)
                {
                    p.vel *= Mathf.Pow(0.04f, dt);
                    p.rotation += p.rotSpeed * dt;
                }
                else if (p.kind == ParticleKind.Debris)
                {
                    p.vel *= Mathf.Pow(0.20f, dt);
                    p.rotation += p.rotSpeed * dt;
                }
                else if (p.kind == ParticleKind.BranchFork)
                {
                    // 分叉能量电弧向外疾驰并急速衰减，不拖泥带水
                    p.vel *= Mathf.Pow(0.30f, dt);
                }

                p.pos += p.vel * dt;

                // 经典控制台文本模式颜色与符号随衰减平滑过渡
                float progress = p.life / p.maxLife;
                if (progress < 0.35f)
                {
                    p.ch = '.';
                    p.color = GameColors.Red;
                }
                else if (progress < 0.7f)
                {
                    p.ch = '*';
                }

                m_particles[i] = p;
            }

            // 2. 更新震荡波
            for (int i = m_shockwaves.Count - 1; i >= 0; i--)
            {
                var sw = m_shockwaves[i];
                sw.currentRadius += sw.expandSpeed * dt;
                if (sw.currentRadius >= sw.maxRadius)
                {
                    m_shockwaves.RemoveAt(i);
                    continue;
                }
                m_shockwaves[i] = sw;
            }
        }

        public void AddExplosion(Vector2 pos, int count, Color mainCol)
        {
            // 1. 核心轻微爆闪 (仅1枚，极速绽放与消隐，避免视野遮挡与浓厚泥泞感)
            float fbLife = 0.18f + GameRandom.Range(0, 8) * 0.01f;
            m_particles.Add(new ParticleData
            {
                pos = pos,
                vel = Vector2.zero,
                ch = '@',
                color = mainCol,
                life = fbLife,
                maxLife = fbLife,
                baseScale = 0.58f,
                rotation = GameRandom.Range(0, 360),
                rotSpeed = (GameRandom.Range(0, 100) - 50) * 4.0f,
                kind = ParticleKind.Fireball
            });

            // 2. 稀疏火星放射状喷溅 (数量适度减少、速度提高、衰减加快，清脆利落)
            int sparkCount = Mathf.Clamp(count / 2, 5, 12);
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = GameRandom.Range(0, 360) * Mathf.Deg2Rad;
                float speed = 16.0f + GameRandom.Range(0, 24);
                float life  = 0.18f + GameRandom.Range(0, 12) * 0.01f;

                m_particles.Add(new ParticleData
                {
                    pos = pos,
                    vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.65f),
                    ch = SparkSymbols[GameRandom.Range(0, SparkSymbols.Length)],
                    color = mainCol,
                    life = life,
                    maxLife = life,
                    baseScale = 0.22f + GameRandom.Range(0, 12) * 0.01f,
                    rotation = 0f,
                    rotSpeed = 0f,
                    kind = ParticleKind.Spark
                });
            }

            // 3. 极少量微小碎屑 (1-2枚即可)
            int debrisCount = Mathf.Clamp(count / 10, 1, 2);
            for (int i = 0; i < debrisCount; i++)
            {
                float angle = GameRandom.Range(0, 360) * Mathf.Deg2Rad;
                float speed = 12.0f + GameRandom.Range(0, 16);
                float life  = 0.20f + GameRandom.Range(0, 10) * 0.01f;

                m_particles.Add(new ParticleData
                {
                    pos = pos,
                    vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.6f),
                    ch = 'x',
                    color = GameColors.Red,
                    life = life,
                    maxLife = life,
                    baseScale = 0.28f,
                    rotation = GameRandom.Range(0, 360),
                    rotSpeed = (GameRandom.Range(0, 100) - 50) * 8.0f,
                    kind = ParticleKind.Debris
                });
            }
        }

        public void AddMegaExplosion(Vector2 pos, float scale = 2.0f, bool isBomb = false)
        {
            // 单枚利落的核心爆闪
            float fbLife = 0.22f;
            m_particles.Add(new ParticleData
            {
                pos = pos,
                vel = Vector2.zero,
                ch = '@',
                color = GameColors.BrightRed,
                life = fbLife,
                maxLife = fbLife,
                baseScale = scale * 0.75f,
                rotation = GameRandom.Range(0, 360),
                rotSpeed = (GameRandom.Range(0, 100) - 50) * 5.0f,
                kind = ParticleKind.Fireball
            });

            // 稀疏高能针芒火星
            int sparkCount = isBomb ? 20 : 16;
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = GameRandom.Range(0, 360) * Mathf.Deg2Rad;
                float speed = (isBomb ? 24.0f : 18.0f) + GameRandom.Range(0, 26);
                float life  = 0.22f + GameRandom.Range(0, 14) * 0.01f;

                m_particles.Add(new ParticleData
                {
                    pos = pos,
                    vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.7f),
                    ch = SparkSymbols[GameRandom.Range(0, SparkSymbols.Length)],
                    color = GameColors.BrightRed,
                    life = life,
                    maxLife = life,
                    baseScale = 0.28f,
                    rotation = 0f,
                    rotSpeed = 0f,
                    kind = ParticleKind.Spark
                });
            }

            // 少量高速碎屑
            int debrisCount = isBomb ? 4 : 3;
            for (int i = 0; i < debrisCount; i++)
            {
                float angle = GameRandom.Range(0, 360) * Mathf.Deg2Rad;
                float speed = 16.0f + GameRandom.Range(0, 20);
                float life  = 0.24f + GameRandom.Range(0, 10) * 0.01f;

                m_particles.Add(new ParticleData
                {
                    pos = pos,
                    vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.6f),
                    ch = 'x',
                    color = GameColors.Red,
                    life = life,
                    maxLife = life,
                    baseScale = 0.32f,
                    rotation = GameRandom.Range(0, 360),
                    rotSpeed = (GameRandom.Range(0, 200) - 100) * 7.0f,
                    kind = ParticleKind.Debris
                });
            }
        }

        public void AddHitSpark(Vector2 pos, Color col)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = GameRandom.Range(0, 360) * Mathf.Deg2Rad;
                float speed = 6.0f + GameRandom.Range(0, 14);
                float life  = 0.15f + GameRandom.Range(0, 15) * 0.01f;

                m_particles.Add(new ParticleData
                {
                    pos = pos,
                    vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.6f),
                    ch = 'x',
                    color = col,
                    life = life,
                    maxLife = life,
                    baseScale = 0.22f,
                    rotation = 0f,
                    rotSpeed = 0f,
                    kind = ParticleKind.Spark
                });
            }
        }

        public void AddThruster(Vector2 pos)
        {
            m_particles.Add(new ParticleData
            {
                pos = pos,
                vel = new Vector2((GameRandom.Range(0, 10) - 5) * 0.5f, 15.0f + GameRandom.Range(0, 8)),
                ch = ':',
                color = GameColors.BrightBlue,
                life = 0.15f,
                maxLife = 0.15f,
                baseScale = 0.25f,
                rotation = 0f,
                rotSpeed = 0f,
                kind = ParticleKind.Spark
            });
        }

        public void AddShockwave(Vector2 center, float maxRadius = 45.0f, Color? color = null, float expandSpeed = 55.0f)
        {
            m_shockwaves.Add(new ShockwaveData
            {
                center = center,
                currentRadius = 1.0f,
                maxRadius = maxRadius,
                expandSpeed = expandSpeed,
                color = color ?? GameColors.BrightRed
            });
        }

        public void AddBombDetonation(Vector2 center)
        {
            // 1. 简单清晰的双层环极速清屏 (贴图已重构为金红双层光环 + 微型电弧分叉，单波展开极其通透利落)
            AddShockwave(center, 62.0f, new Color(1f, 0.45f, 0.15f), 120.0f);

            // 2. 辐射状微量高速能量针芒 (辅助增强立体冲击感，迅速消隐)
            int branchCount = 10;
            for (int b = 0; b < branchCount; b++)
            {
                float baseAngle = (b * (360f / branchCount) + GameRandom.Range(-6, 6)) * Mathf.Deg2Rad;
                float mainSpeed = 55.0f + GameRandom.Range(0, 20);
                float mainLife = 0.22f + GameRandom.Range(0, 6) * 0.01f;

                m_particles.Add(new ParticleData
                {
                    pos = center,
                    vel = new Vector2(Mathf.Cos(baseAngle) * mainSpeed, Mathf.Sin(baseAngle) * mainSpeed * 0.75f),
                    ch = '^',
                    color = new Color(1f, 0.85f, 0.35f),
                    life = mainLife,
                    maxLife = mainLife,
                    baseScale = 0.45f,
                    rotation = 0f,
                    rotSpeed = 0f,
                    kind = ParticleKind.BranchFork
                });

                // 侧向微小分支分叉 (1~2个细多分叉)
                float forkAngle1 = baseAngle + 28f * Mathf.Deg2Rad;
                float forkSpeed1 = mainSpeed * 0.72f;
                float forkLife1 = mainLife * 0.75f;
                m_particles.Add(new ParticleData
                {
                    pos = center,
                    vel = new Vector2(Mathf.Cos(forkAngle1) * forkSpeed1, Mathf.Sin(forkAngle1) * forkSpeed1 * 0.75f),
                    ch = '/',
                    color = new Color(1f, 0.45f, 0.15f),
                    life = forkLife1,
                    maxLife = forkLife1,
                    baseScale = 0.35f,
                    rotation = 0f,
                    rotSpeed = 0f,
                    kind = ParticleKind.BranchFork
                });

                if (b % 2 == 0)
                {
                    float forkAngle2 = baseAngle - 26f * Mathf.Deg2Rad;
                    float forkSpeed2 = mainSpeed * 0.68f;
                    float forkLife2 = mainLife * 0.70f;
                    m_particles.Add(new ParticleData
                    {
                        pos = center,
                        vel = new Vector2(Mathf.Cos(forkAngle2) * forkSpeed2, Mathf.Sin(forkAngle2) * forkSpeed2 * 0.75f),
                        ch = '\\',
                        color = new Color(1f, 0.45f, 0.15f),
                        life = forkLife2,
                        maxLife = forkLife2,
                        baseScale = 0.32f,
                        rotation = 0f,
                        rotSpeed = 0f,
                        kind = ParticleKind.BranchFork
                    });
                }
            }

            // 3. 核心微型爆闪 (利落迅速，无浓烟残留)
            AddMegaExplosion(center, scale: 1.6f, isBomb: true);
        }
    }
}
