using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;
using GalaxyStrike.Entities;

namespace GalaxyStrike.Systems
{
    // ============================================================================
    // 【战术关卡波次编排器 · WaveManager.cs】
    // 对应 C++ 原工程 WaveManager.h / WaveManager.cpp：
    // 调度 5 大集群编队波次（V字蜂群、双翼钳形、重装护航、精英战巡、母舰 BOSS），
    // 并维护准备、生成、交火、清屏四大状态流转。
    // ============================================================================

    public class WaveManager
    {
        private int         m_wave;
        private WaveState   m_state;
        private float       m_stateTimer;
        private int         m_spawnStep;
        private float       m_spawnInterval;
        private float       m_spawnTimer;
        private bool        m_bossActive;
        private BossEnemy   m_currentBoss;

        public WaveManager()
        {
            Reset();
        }

        public void Reset()
        {
            m_wave = 0;
            m_state = WaveState.Preparation;
            m_stateTimer = 2.0f;
            m_spawnStep = 0;
            m_spawnInterval = 1.2f;
            m_spawnTimer = 0.0f;
            m_bossActive = false;
            m_currentBoss = null;
        }

        public void StartWave(int wave, Action<SfxType> playSfx = null)
        {
            m_wave = wave;
            m_state = WaveState.Spawning;
            m_spawnStep = 0;
            m_spawnTimer = 0.0f;
            m_bossActive = (m_wave % 5 == 0); // 每第 5 波触发灭世母舰决战
            m_currentBoss = null;

            if (m_bossActive)
            {
                playSfx?.Invoke(SfxType.BossAlert);
            }
            else
            {
                playSfx?.Invoke(SfxType.Start);
            }
        }

        public void Update(float dt, List<Enemy> enemies, Action<SfxType> playSfx = null)
        {
            // 校验 Boss 存活状态
            if (m_bossActive)
            {
                if (m_currentBoss == null || !m_currentBoss.IsAlive)
                {
                    m_bossActive = false;
                    m_currentBoss = null;
                }
            }

            switch (m_state)
            {
                case WaveState.Preparation:
                {
                    m_stateTimer -= dt;
                    if (m_stateTimer <= 0.0f)
                    {
                        StartWave(m_wave + 1, playSfx);
                    }
                    break;
                }

                case WaveState.Spawning:
                {
                    m_spawnTimer += dt;
                    if (m_spawnTimer >= m_spawnInterval)
                    {
                        m_spawnTimer = 0.0f;
                        SpawnFormation(m_wave, enemies);
                        m_spawnStep++;

                        // 每个波次通常包含 2~3 个连续集中集群阶段
                        int maxSteps = (m_wave % 5 == 0) ? 2 : 3;
                        if (m_spawnStep >= maxSteps)
                        {
                            m_state = WaveState.Fighting;
                        }
                    }
                    break;
                }

                case WaveState.Fighting:
                {
                    // 当所有敌机均被歼灭时进入 WaveClear
                    if (enemies.Count == 0 && !m_bossActive)
                    {
                        m_state = WaveState.WaveClear;
                        m_stateTimer = 3.0f; // 留 3 秒安全时间拾取掉落物资
                        playSfx?.Invoke(SfxType.SpeedUp);
                    }
                    break;
                }

                case WaveState.WaveClear:
                {
                    m_stateTimer -= dt;
                    if (m_stateTimer <= 0.0f)
                    {
                        m_state = WaveState.Preparation;
                        m_stateTimer = 2.0f;
                    }
                    break;
                }
            }
        }

        public void SpawnFormation(int wave, List<Enemy> enemies)
        {
            int patternIndex = (wave - 1) % 5;

            switch (patternIndex)
            {
                case 0: // ---- Wave 1: 蜂群冲锋 V 字集群 ----
                {
                    if (m_spawnStep == 0)
                    {
                        // 集中敌人 1: 标准 V 字突击编队 (7架侦察机集中突击)
                        float midX = 50.0f;
                        enemies.Add(new ScoutEnemy(new Vector2(midX, -3.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(midX - 8.0f, -6.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(midX + 8.0f, -6.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(midX - 16.0f, -9.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(midX + 16.0f, -9.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(midX - 24.0f, -12.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(midX + 24.0f, -12.0f), 0));
                    }
                    else if (m_spawnStep == 1)
                    {
                        // 集中敌人 2: 蛇形扫荡蜂群 (6架正弦曲线切入)
                        for (int i = 0; i < 6; i++)
                        {
                            float x = 20.0f + i * 12.0f;
                            enemies.Add(new ScoutEnemy(new Vector2(x, -4.0f - i * 3.0f), 1));
                        }
                    }
                    else
                    {
                        // 伴随 2 架突击战机压制
                        enemies.Add(new GunshipEnemy(new Vector2(35.0f, -4.0f)));
                        enemies.Add(new GunshipEnemy(new Vector2(65.0f, -4.0f)));
                    }
                    break;
                }

                case 1: // ---- Wave 2: 双翼钳形集群合围 ----
                {
                    if (m_spawnStep == 0)
                    {
                        // 左翼高速蜂群 (5架斜切)
                        for (int i = 0; i < 5; i++)
                        {
                            enemies.Add(new ScoutEnemy(new Vector2(10.0f + i * 4.0f, -3.0f - i * 3.0f), 2));
                        }
                        // 右翼高速蜂群 (5架向左斜切)
                        for (int i = 0; i < 5; i++)
                        {
                            enemies.Add(new ScoutEnemy(new Vector2(90.0f - i * 4.0f, -3.0f - i * 3.0f), 2));
                        }
                    }
                    else if (m_spawnStep == 1)
                    {
                        // 中央双联突击强攻
                        enemies.Add(new GunshipEnemy(new Vector2(40.0f, -5.0f)));
                        enemies.Add(new GunshipEnemy(new Vector2(60.0f, -5.0f)));
                    }
                    else
                    {
                        // 1 架重装轰炸机居中推进
                        enemies.Add(new BomberEnemy(new Vector2(50.0f, -6.0f)));
                        enemies.Add(new ScoutEnemy(new Vector2(25.0f, -4.0f), 0));
                        enemies.Add(new ScoutEnemy(new Vector2(75.0f, -4.0f), 0));
                    }
                    break;
                }

                case 2: // ---- Wave 3: 重装装甲护航编队 ----
                {
                    if (m_spawnStep == 0)
                    {
                        // 双重装轰炸机并列推进
                        enemies.Add(new BomberEnemy(new Vector2(32.0f, -6.0f)));
                        enemies.Add(new BomberEnemy(new Vector2(68.0f, -6.0f)));
                    }
                    else if (m_spawnStep == 1)
                    {
                        // 4 架突击机形成护航屏障
                        enemies.Add(new GunshipEnemy(new Vector2(18.0f, -4.0f)));
                        enemies.Add(new GunshipEnemy(new Vector2(45.0f, -4.0f)));
                        enemies.Add(new GunshipEnemy(new Vector2(55.0f, -4.0f)));
                        enemies.Add(new GunshipEnemy(new Vector2(82.0f, -4.0f)));
                    }
                    else
                    {
                        // 密集冲锋蜂群
                        for (int i = 0; i < 8; i++)
                        {
                            enemies.Add(new ScoutEnemy(new Vector2(15.0f + i * 10.0f, -3.0f - (i % 3) * 3.0f), 1));
                        }
                    }
                    break;
                }

                case 3: // ---- Wave 4: 精英战巡前锋舰队 ----
                {
                    if (m_spawnStep == 0)
                    {
                        // 精英巡洋舰居中 + 左右重装轰炸机
                        enemies.Add(new EliteCruiser(new Vector2(48.0f, -6.0f)));
                    }
                    else if (m_spawnStep == 1)
                    {
                        enemies.Add(new BomberEnemy(new Vector2(25.0f, -5.0f)));
                        enemies.Add(new BomberEnemy(new Vector2(75.0f, -5.0f)));
                    }
                    else
                    {
                        // 快速蜂群双向掩护
                        for (int i = 0; i < 6; i++)
                        {
                            enemies.Add(new ScoutEnemy(new Vector2(20.0f + i * 12.0f, -4.0f), 0));
                        }
                    }
                    break;
                }

                case 4: // ---- Wave 5 (或 10, 15...): 灭世母舰 BOSS 决战 ----
                default:
                {
                    if (m_spawnStep == 0)
                    {
                        // 诞生巨型母舰 BOSS！根据波次提升最大 HP
                        int bossHp = 450 + (wave / 5 - 1) * 300;
                        var boss = new BossEnemy(new Vector2(42.0f, -6.0f), bossHp);
                        m_currentBoss = boss;
                        m_bossActive = true;
                        enemies.Add(boss);
                    }
                    else
                    {
                        // 母舰侧翼护卫机群
                        enemies.Add(new GunshipEnemy(new Vector2(20.0f, -4.0f)));
                        enemies.Add(new GunshipEnemy(new Vector2(80.0f, -4.0f)));
                        enemies.Add(new ScoutEnemy(new Vector2(12.0f, -5.0f), 1));
                        enemies.Add(new ScoutEnemy(new Vector2(88.0f, -5.0f), 1));
                    }
                    break;
                }
            }
        }

        public string GetWaveTitle()
        {
            int p = (m_wave - 1) % 5;
            switch (p)
            {
                case 0: return "WAVE 1: SCOUT SWARM [V-DIVE]";
                case 1: return "WAVE 2: PINCER ASSAULT SQUAD";
                case 2: return "WAVE 3: HEAVY BOMBER FORMATION";
                case 3: return "WAVE 4: ELITE BATTLEGROUP";
                case 4: return "WAVE 5: FLAGSHIP MOTHERSHIP BOSS";
                default: return "COMBAT SECTOR PATROL";
            }
        }

        public string GetThreatAlert()
        {
            if (m_bossActive) return "! WARNING: BOSS TARGET !";
            if (m_state == WaveState.Spawning) return ">> SWARM DETECTED <<";
            if (m_state == WaveState.WaveClear) return "++ SECTOR SECURED ++";
            return "-- SENSORS NORMAL --";
        }

        public float GetBossHpRatio()
        {
            if (m_bossActive && m_currentBoss != null && m_currentBoss.IsAlive)
            {
                return (float)m_currentBoss.Hp / m_currentBoss.MaxHp;
            }
            return 0.0f;
        }

        public int Wave => m_wave;
        public WaveState State => m_state;
        public float StateTimer => m_stateTimer;
        public bool IsBossActive => m_bossActive;
        public BossEnemy CurrentBoss => m_currentBoss;
    }
}
