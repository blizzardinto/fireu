using System;
using UnityEngine;
using GalaxyStrike.Core;
using GalaxyStrike.Entities;
using GalaxyStrike.Systems;

namespace GalaxyStrike.UI
{
    // ============================================================================
    // 【战术 HUD 仪表盘与全息中枢 · GalaxyStrikeHUD.cs】
    // 对应 C++ 原工程 Hud.h / Hud.cpp，全面面向现代高分辨率屏幕重构：
    // - 动态响应式自适应字号（支持 720p / 1080p / 2K / 4K）
    // - 科幻机甲风开始主菜单（大字号主标题、三列特色卡片、超大交互出击按钮、键位图谱）
    // - 全息战术遥测侧边栏（加宽排版、加粗血条/护盾条、大号命数与核弹徽章）
    // - 终极母舰 BOSS 顶部巨幅警报血条
    // - 暂停 / 胜利 / 失败多模态窗全交互支持
    // ============================================================================

    public class GalaxyStrikeHUD : MonoBehaviour
    {
        // 外部交互事件状态标志
        public bool StartGameRequested { get; set; }
        public bool RestartGameRequested { get; set; }
        public bool ResumeGameRequested { get; set; }
        public bool TitleGameRequested { get; set; }

        private GUIStyle m_titleStyle;
        private GUIStyle m_subtitleStyle;
        private GUIStyle m_headerStyle;
        private GUIStyle m_bodyStyle;
        private GUIStyle m_bodyBoldStyle;
        private GUIStyle m_alertStyle;
        private GUIStyle m_barStyle;
        private GUIStyle m_btnStyle;
        private GUIStyle m_modalTitleStyle;
        private GUIStyle m_modalBodyStyle;
        private GUIStyle m_cardTitleStyle;

        private Texture2D m_whiteTex;
        private Texture2D m_titleBgTex;
        private Font m_mainFont;

        private Color m_flashColor = Color.clear;
        private float m_flashTimer = 0f;
        private float m_flashDuration = 0f;

        public void TriggerScreenFlash(Color color, float duration)
        {
            m_flashColor = color;
            m_flashDuration = Mathf.Max(0.001f, duration);
            m_flashTimer = duration;
        }

        private void Awake()
        {
            m_whiteTex = new Texture2D(1, 1);
            m_whiteTex.SetPixel(0, 0, Color.white);
            m_whiteTex.Apply();

            m_titleBgTex = Resources.Load<Texture2D>("GalaxyStrikeArt/Sprites/TitleBackground");
        }

        private void OnDestroy()
        {
            if (m_whiteTex != null)
            {
                Destroy(m_whiteTex);
            }
        }

        private void EnsureFont()
        {
            if (m_mainFont != null) return;
            // 优先加载中英文兼备的高清系统字体
            m_mainFont = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei", "SimHei", "PingFang SC", "Segoe UI", "Consolas", "Lucida Console", "Arial"
            }, 16);
        }

        private void InitStyles(float uiScale)
        {
            EnsureFont();

            m_titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(22 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = GameColors.BrightYellow }
            };

            m_subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(13 * uiScale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = GameColors.BrightCyan }
            };

            m_headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(15 * uiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = GameColors.BrightCyan }
            };

            m_bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(14 * uiScale),
                normal = { textColor = Color.white }
            };

            m_bodyBoldStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(14 * uiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            m_alertStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(15 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = GameColors.BrightRed }
            };

            m_barStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(12 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            m_btnStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(18 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            m_modalTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(28 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = GameColors.BrightYellow }
            };

            m_modalBodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(15 * uiScale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            m_cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = m_mainFont,
                fontSize = Mathf.RoundToInt(16 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = GameColors.BrightGreen }
            };
        }

        public void Draw(Player player, WaveManager waveMgr, int score, int highScore, int kills, GameState gameState)
        {
            float screenW = Screen.width;
            float screenH = Screen.height;

            // 动态响应式缩放因子：以 800p 高度为 1.0 标准，范围 1.0 ~ 2.4
            float uiScale = Mathf.Clamp(screenH / 800f, 1.0f, 2.4f);
            InitStyles(uiScale);

            // 1. 如果处于标题开始界面，全屏优先呈现宏大清晰的开始菜单
            if (gameState == GameState.Title)
            {
                DrawTitleScreen(screenW, screenH, highScore, uiScale);
                return;
            }

            // 2. 划分战场与 HUD 侧边栏：左侧为星空战斗空域，右侧为全息数据侧边栏
            float hudW = Mathf.Clamp(screenW * 0.26f, 300f * uiScale, 420f * uiScale);
            float hudX = screenW - hudW - 8f;
            float hudY = 8f;
            float hudH = screenH - 16f;

            // 绘制左侧星空战区科技边框 (Combat Viewport Cockpit Frame)
            float combatX = 8f;
            float combatY = 8f;
            float combatW = hudX - 16f;
            float combatH = screenH - 16f;
            DrawCombatViewportFrame(combatX, combatY, combatW, combatH, uiScale, waveMgr);

            // 侧边栏深色全息半透明背板与青色科技线框
            DrawCornerBox(new Rect(hudX, hudY, hudW, hudH), new Color(0.03f, 0.07f, 0.15f, 0.92f), GameColors.Cyan, 12f * uiScale);

            float padding = 14f * uiScale;
            float curY = hudY + padding;
            float contentX = hudX + padding;
            float contentW = hudW - padding * 2f;

            // 标题区块
            GUI.Label(new Rect(contentX, curY, contentW, 26f * uiScale), "★ GALAXY STRIKE ★", m_titleStyle);
            curY += 26f * uiScale;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "战术空战中枢 · 全息遥测", m_subtitleStyle);
            curY += 22f * uiScale;

            DrawHLine(contentX, curY, contentW, GameColors.Cyan);
            curY += 10f * uiScale;

            // 战绩积分榜
            m_bodyStyle.normal.textColor = GameColors.BrightWhite;
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), $"战况积分 : {score:D7}", m_bodyStyle);
            curY += 20f * uiScale;

            m_bodyStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), $"历史最高 : {highScore:D7}", m_bodyStyle);
            curY += 20f * uiScale;

            m_bodyStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), $"击坠敌机 : {kills} 架", m_bodyStyle);
            curY += 26f * uiScale;

            // 机体完整度 (HULL)
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), "【 机体完好度 HULL 】", m_headerStyle);
            curY += 22f * uiScale;

            Color hpCol = GameColors.BrightGreen;
            if (player.Hp <= player.MaxHp * 0.25f) hpCol = GameColors.BrightRed;
            else if (player.Hp <= player.MaxHp * 0.5f) hpCol = GameColors.BrightYellow;

            m_bodyBoldStyle.normal.textColor = hpCol;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), $"装甲完整度 : {player.Hp} / {player.MaxHp}", m_bodyBoldStyle);
            curY += 20f * uiScale;

            float barH = 18f * uiScale;
            DrawBar(new Rect(contentX, curY, contentW, barH), player.Hp, player.MaxHp, hpCol, new Color(0.35f, 0.08f, 0.08f));
            curY += barH + 12f * uiScale;

            // 能量护盾 (SHIELD)
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), "【 防护盾能量 SHIELD 】", m_headerStyle);
            curY += 22f * uiScale;

            m_bodyBoldStyle.normal.textColor = GameColors.BrightCyan;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), $"护盾矩阵 : {player.Shield} / {player.MaxShield}", m_bodyBoldStyle);
            curY += 20f * uiScale;

            DrawBar(new Rect(contentX, curY, contentW, barH), player.Shield, player.MaxShield, GameColors.BrightCyan, new Color(0.08f, 0.18f, 0.45f));
            curY += barH + 14f * uiScale;

            // 备用机体命数
            m_bodyStyle.normal.textColor = Color.white;
            string livesStr = "备用战机: ";
            for (int i = 0; i < player.Lives; i++) livesStr += " [❤] ";
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), livesStr, m_bodyStyle);
            curY += 24f * uiScale;

            DrawHLine(contentX, curY, contentW, new Color(0.2f, 0.6f, 0.8f, 0.5f));
            curY += 10f * uiScale;

            // 机型形态与升级经验
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), "【 机型演进 CLASS 】", m_headerStyle);
            curY += 22f * uiScale;

            m_bodyBoldStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), player.GetShipClassName(), m_bodyBoldStyle);
            curY += 20f * uiScale;

            if (player.ShipLevel >= GameConfig.MAX_SHIP_LEVEL)
            {
                m_bodyStyle.normal.textColor = GameColors.BrightYellow;
                GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "机体已达终极形态 [MAX UPGRADED]", m_bodyStyle);
                curY += 18f * uiScale;
                DrawBar(new Rect(contentX, curY, contentW, 12f * uiScale), 1, 1, GameColors.BrightYellow, Color.black);
            }
            else
            {
                m_bodyStyle.normal.textColor = GameColors.Yellow;
                GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), $"进化经验 : {player.ShipExp} / {player.ShipExpNext}", m_bodyStyle);
                curY += 18f * uiScale;
                DrawBar(new Rect(contentX, curY, contentW, 12f * uiScale), player.ShipExp, player.ShipExpNext, GameColors.BrightYellow, new Color(0.25f, 0.25f, 0.08f));
            }
            curY += 12f * uiScale + 14f * uiScale;

            // 武器火力系统
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), "【 火力阵列 WEAPON 】", m_headerStyle);
            curY += 22f * uiScale;

            m_bodyBoldStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), player.GetWeaponModeName(), m_bodyBoldStyle);
            curY += 20f * uiScale;

            string bombsStr = $"全屏高爆核弹: {player.Bombs} 组";
            for (int i = 0; i < player.Bombs; i++) bombsStr += " [☢]";
            m_bodyBoldStyle.normal.textColor = GameColors.BrightRed;
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), bombsStr, m_bodyBoldStyle);
            curY += 24f * uiScale;

            DrawHLine(contentX, curY, contentW, new Color(0.2f, 0.6f, 0.8f, 0.5f));
            curY += 10f * uiScale;

            // 战术空域雷达
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), "【 空域波次 RADAR 】", m_headerStyle);
            curY += 22f * uiScale;

            m_bodyStyle.normal.textColor = GameColors.BrightWhite;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), waveMgr.GetWaveTitle(), m_bodyStyle);
            curY += 20f * uiScale;

            m_alertStyle.normal.textColor = waveMgr.IsBossActive ? GameColors.BrightRed :
                (waveMgr.State == WaveState.Spawning ? GameColors.BrightYellow : GameColors.BrightGreen);
            GUI.Label(new Rect(contentX, curY, contentW, 20f * uiScale), waveMgr.GetThreatAlert(), m_alertStyle);
            curY += 26f * uiScale;

            DrawHLine(contentX, curY, contentW, GameColors.Cyan);
            curY += 10f * uiScale;

            // 操作提示
            m_headerStyle.normal.textColor = GameColors.BrightCyan;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "战术操作指南 (CONTROLS):", m_headerStyle);
            curY += 20f * uiScale;

            m_bodyStyle.normal.textColor = new Color(0.88f, 0.88f, 0.88f);
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "WASD / 方向键 : 机动走位", m_bodyStyle);
            curY += 18f * uiScale;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "空格 / J / 鼠标左键 : 连发射击", m_bodyStyle);
            curY += 18f * uiScale;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "B / K / 鼠标右键 : EMP核弹", m_bodyStyle);
            curY += 18f * uiScale;
            GUI.Label(new Rect(contentX, curY, contentW, 18f * uiScale), "P / ESC : 战术暂停", m_bodyStyle);

            // 3. 巨幅 Boss 血条（位于空战区正上方）
            if (waveMgr.IsBossActive)
            {
                DrawBossBar(screenW, waveMgr, uiScale);
            }

            // 4. 模态窗口
            if (gameState == GameState.Paused)
            {
                DrawPauseModal(screenW, screenH, uiScale);
            }
            else if (gameState == GameState.GameOver)
            {
                DrawGameOverModal(screenW, screenH, score, highScore, uiScale);
            }
            else if (gameState == GameState.Victory)
            {
                DrawVictoryModal(screenW, screenH, score, uiScale);
            }

            // 5. 全屏热力闪光 (用于核弹、巨舰爆炸、战机损毁)
            if (m_flashTimer > 0f)
            {
                m_flashTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(m_flashTimer / m_flashDuration) * m_flashColor.a;
                Color prevColor = GUI.color;
                GUI.color = new Color(m_flashColor.r, m_flashColor.g, m_flashColor.b, alpha);
                GUI.DrawTexture(new Rect(0, 0, screenW, screenH), m_whiteTex);
                GUI.color = prevColor;
            }
        }

        // ------------------------------------------------------------------------
        // 【星空战区全息战斗边框 (Combat Viewport Cockpit Frame)】
        // ------------------------------------------------------------------------
        private void DrawCombatViewportFrame(float cx, float cy, float cw, float ch, float uiScale, WaveManager waveMgr)
        {
            // 1. 科技战术边框：双层复合科技线框（外层深青灰，内层明亮青色导引线）
            Color outerBorder = new Color(0.12f, 0.40f, 0.65f, 0.65f);
            Color innerBorder = new Color(0.0f, 0.90f, 1.0f, 0.40f);
            DrawBox(new Rect(cx, cy, cw, ch), Color.clear, outerBorder);
            DrawBox(new Rect(cx + 2f, cy + 2f, cw - 4f, ch - 4f), Color.clear, innerBorder);

            // 2. 四角醒目加粗战术瞄准角标 (Tactical Corner Brackets)
            float cornerLen = 24f * uiScale;
            float cornerThick = 3.5f * uiScale;
            GUI.color = GameColors.Cyan;
            // 左上角
            GUI.DrawTexture(new Rect(cx, cy, cornerLen, cornerThick), m_whiteTex);
            GUI.DrawTexture(new Rect(cx, cy, cornerThick, cornerLen), m_whiteTex);
            // 右上角
            GUI.DrawTexture(new Rect(cx + cw - cornerLen, cy, cornerLen, cornerThick), m_whiteTex);
            GUI.DrawTexture(new Rect(cx + cw - cornerThick, cy, cornerThick, cornerLen), m_whiteTex);
            // 左下角
            GUI.DrawTexture(new Rect(cx, cy + ch - cornerThick, cornerLen, cornerThick), m_whiteTex);
            GUI.DrawTexture(new Rect(cx, cy + ch - cornerLen, cornerThick, cornerLen), m_whiteTex);
            // 右下角
            GUI.DrawTexture(new Rect(cx + cw - cornerLen, cy + ch - cornerThick, cornerLen, cornerThick), m_whiteTex);
            GUI.DrawTexture(new Rect(cx + cw - cornerThick, cy + ch - cornerLen, cornerThick, cornerLen), m_whiteTex);
            GUI.color = Color.white;

            // 3. 顶部科技状态标牌 (Top Bezel Status)
            float topTagW = 280f * uiScale;
            float topTagH = 22f * uiScale;
            DrawBox(new Rect(cx + 20f * uiScale, cy, topTagW, topTagH),
                new Color(0.02f, 0.08f, 0.16f, 0.88f), new Color(0f, 0.85f, 1f, 0.7f));

            m_bodyStyle.fontSize = Mathf.RoundToInt(12 * uiScale);
            m_bodyStyle.normal.textColor = GameColors.BrightCyan;
            GUI.Label(new Rect(cx + 26f * uiScale, cy + 1f, topTagW, topTagH),
                "🛰 SECTOR-07 [ALPHA] · 实时战区监控", m_bodyStyle);

            // 右侧空域坐标标尺
            float coordW = 210f * uiScale;
            DrawBox(new Rect(cx + cw - coordW - 20f * uiScale, cy, coordW, topTagH),
                new Color(0.02f, 0.08f, 0.16f, 0.88f), new Color(0.2f, 0.6f, 0.8f, 0.5f));
            m_bodyStyle.normal.textColor = new Color(0.7f, 0.85f, 1f, 0.85f);
            GUI.Label(new Rect(cx + cw - coordW - 14f * uiScale, cy + 1f, coordW, topTagH),
                "GRID: 100x48 | ORBIT: LOW-EARTH", m_bodyStyle);

            // 4. 左右两侧刻度标尺 (Side Altitude / Telemetry Ticks)
            GUI.color = new Color(0f, 0.8f, 1f, 0.35f);
            float tickLen = 8f * uiScale;
            for (float ty = cy + ch * 0.2f; ty <= cy + ch * 0.85f; ty += ch * 0.15f)
            {
                GUI.DrawTexture(new Rect(cx + 2f, ty, tickLen, 1.5f), m_whiteTex);
                GUI.DrawTexture(new Rect(cx + cw - tickLen - 2f, ty, tickLen, 1.5f), m_whiteTex);
            }
            GUI.color = Color.white;

            // 5. 底部战术快捷提示框 (Bottom Flight Bar)
            float btmBarW = Mathf.Min(560f * uiScale, cw - 40f * uiScale);
            float btmBarH = 24f * uiScale;
            float btmBarX = cx + (cw - btmBarW) * 0.5f;
            float btmBarY = cy + ch - btmBarH;

            DrawBox(new Rect(btmBarX, btmBarY, btmBarW, btmBarH),
                new Color(0.02f, 0.07f, 0.14f, 0.88f), new Color(0.15f, 0.55f, 0.8f, 0.5f));

            m_bodyStyle.fontSize = Mathf.RoundToInt(12 * uiScale);
            m_bodyStyle.alignment = TextAnchor.MiddleCenter;
            m_bodyStyle.normal.textColor = new Color(0.85f, 0.95f, 1.0f, 0.9f);
            GUI.Label(new Rect(btmBarX, btmBarY + 1f, btmBarW, btmBarH),
                "[W A S D] 机动规避   |   [SPACE / J] 持续开火   |   [B / 右键] 全屏高爆核弹", m_bodyStyle);
            m_bodyStyle.alignment = TextAnchor.UpperLeft;
        }

        // ------------------------------------------------------------------------
        // 【全新超清科幻主开始菜单 (Title Screen)】
        // ------------------------------------------------------------------------
        public void DrawTitleScreen(float screenW, float screenH, int highScore, float uiScale)
        {
            // 1. 全屏战术太空背景 (优先使用高清原画背景，带暗角与全息高对比度过滤)
            if (m_titleBgTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, screenW, screenH), m_titleBgTex, ScaleMode.ScaleAndCrop);
                // 叠加轻微科幻半透明暗角，凸显中央战术面板
                GUI.color = new Color(0.01f, 0.02f, 0.05f, 0.50f);
                GUI.DrawTexture(new Rect(0, 0, screenW, screenH), m_whiteTex);
            }
            else
            {
                GUI.color = new Color(0.02f, 0.04f, 0.09f, 0.96f);
                GUI.DrawTexture(new Rect(0, 0, screenW, screenH), m_whiteTex);
            }
            GUI.color = Color.white;

            // 2. 自适应居中高科技全息大面板
            float panelW = Mathf.Min(980f * uiScale, screenW * 0.95f);
            float panelH = Mathf.Min(670f * uiScale, screenH * 0.95f);
            float px = (screenW - panelW) * 0.5f;
            float py = (screenH - panelH) * 0.5f;

            // 科技双层边框与发光切角背板 (深空半透明晶体质感)
            DrawCornerBox(new Rect(px, py, panelW, panelH),
                new Color(0.02f, 0.05f, 0.12f, 0.92f), GameColors.Cyan, 20f * uiScale);

            float y = py + 20f * uiScale;

            // 3. 顶部发光大标题群：阴影与多阶高亮层次
            m_modalTitleStyle.fontSize = Mathf.RoundToInt(34 * uiScale);
            // 阴影沉底
            m_modalTitleStyle.normal.textColor = new Color(0.0f, 0.1f, 0.2f, 0.85f);
            GUI.Label(new Rect(px + 2f, y + 2f, panelW, 42f * uiScale), "《 星 际 突 袭 ： 银 河 战 线 》", m_modalTitleStyle);
            // 顶层辉光金黄
            m_modalTitleStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(px, y, panelW, 42f * uiScale), "《 星 际 突袭 ： 银 河 战 线 》", m_modalTitleStyle);
            y += 42f * uiScale;

            m_subtitleStyle.fontSize = Mathf.RoundToInt(15 * uiScale);
            m_subtitleStyle.fontStyle = FontStyle.Bold;
            m_subtitleStyle.normal.textColor = GameColors.BrightCyan;
            GUI.Label(new Rect(px, y, panelW, 20f * uiScale), "GALAXY STRIKE : TACTICAL GALACTIC FRONT", m_subtitleStyle);
            y += 24f * uiScale;

            m_bodyStyle.fontSize = Mathf.RoundToInt(13 * uiScale);
            m_bodyStyle.alignment = TextAnchor.MiddleCenter;
            m_bodyStyle.normal.textColor = new Color(0.85f, 0.92f, 1.0f, 0.9f);
            GUI.Label(new Rect(px, y, panelW, 18f * uiScale), "【 纯 C# 原生架构演进 · 60 FPS 经典街机太空弹幕 · 全特效深度重塑 】", m_bodyStyle);
            m_bodyStyle.alignment = TextAnchor.UpperLeft;
            y += 24f * uiScale;

            DrawHLine(px + 28f * uiScale, y, panelW - 56f * uiScale, new Color(0f, 0.8f, 1f, 0.4f));
            y += 18f * uiScale;

            // 4. 三大核心战术特色卡片
            float cardGap = 16f * uiScale;
            float cardW = (panelW - 56f * uiScale - cardGap * 2f) / 3f;
            float cardH = 215f * uiScale;
            float cardY = y;

            // 卡片 1: 战机机体演进 (翡翠青绿)
            float cx1 = px + 28f * uiScale;
            DrawCard(new Rect(cx1, cardY, cardW, cardH), "【 战机进化 EVOLUTION 】", GameColors.BrightGreen, new[] {
                "• 5 阶进化: 猎鹰➔蝰蛇➔凤凰➔雷霆➔太阳神",
                "• 动态双发喷气尾焰与护盾矩阵自充能",
                "• 强化机载装甲与脱战 3 秒自动回盾",
                "• 战损无敌保护与高灵敏亚像素机动"
            }, uiScale);

            // 卡片 2: 全域武器火力 (太阳炽金)
            float cx2 = cx1 + cardW + cardGap;
            DrawCard(new Rect(cx2, cardY, cardW, cardH), "【 战备武器 ARSENAL 】", GameColors.BrightYellow, new[] {
                "• 5 阶主炮: 等离子重炮 ➔ 散射重火",
                "• 高能贯穿激光束 (一发连环穿透多重机身)",
                "• 自动索敌微型飞弹群 (智能导引追踪)",
                "• 全屏高爆核弹 (烈焰级联震荡波清屏)"
            }, uiScale);

            // 卡片 3: 敌军集群与母舰 (烈焰高爆红)
            float cx3 = cx2 + cardW + cardGap;
            DrawCard(new Rect(cx3, cardY, cardW, cardH), "【 敌群波次 & BOSS 】", GameColors.BrightRed, new[] {
                "• 5 大敌机机种: 侦察 / 炮艇 / 轰炸 / 巡洋",
                "• 经典 V 型飞翼与钳形包夹等集中突击波",
                "• 灭世母舰 BOSS 决战 (3 阶段形态狂暴)",
                "• 密集中子光球与赤红贯通激光雨"
            }, uiScale);

            y += cardH + 20f * uiScale;

            // 5. 核心交互出击按钮：全面重新设计的 Cyber 战术按钮
            float btnW = Mathf.Min(480f * uiScale, panelW - 60f * uiScale);
            float btnH = 60f * uiScale;
            float btnX = px + (panelW - btnW) * 0.5f;

            if (DrawHeroButton(new Rect(btnX, y, btnW, btnH), "▶ 立即出战 (START MISSION)", uiScale))
            {
                StartGameRequested = true;
            }
            y += btnH + 12f * uiScale;

            // 快捷按键提示
            m_modalBodyStyle.fontSize = Mathf.RoundToInt(15 * uiScale);
            m_modalBodyStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(px, y, panelW, 22f * uiScale), "按 [空格]、[回车]、[任意移动键] 或 [点击出击按钮] 均可立即开战！", m_modalBodyStyle);
            y += 26f * uiScale;

            // 6. 底部信息基座
            float bottomBoxW = panelW - 56f * uiScale;
            float bottomBoxH = 50f * uiScale;
            float bx = px + 28f * uiScale;
            DrawBox(new Rect(bx, y, bottomBoxW, bottomBoxH), new Color(0.03f, 0.07f, 0.16f, 0.88f), new Color(0.18f, 0.65f, 0.85f, 0.55f));

            m_bodyStyle.fontSize = Mathf.RoundToInt(13 * uiScale);
            m_bodyStyle.alignment = TextAnchor.MiddleCenter;
            m_bodyStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(bx, y + 6f * uiScale, bottomBoxW, 18f * uiScale),
                "战术操控 | 移动: WASD / 方向键   射击: 按住空格 / J / 鼠标左键   高爆核弹: B / K / 鼠标右键   暂停: P", m_bodyStyle);

            m_bodyStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(bx, y + 26f * uiScale, bottomBoxW, 18f * uiScale),
                $"[ 历史最高战绩纪录: {highScore:D7} ]    按 [Q] 或 [ESC] 可退出游戏", m_bodyStyle);
            m_bodyStyle.alignment = TextAnchor.UpperLeft;
        }

        private bool DrawHeroButton(Rect r, string text, float uiScale)
        {
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            bool isHover = r.Contains(mouse);

            // 科技感双层发光底板：渐变高亮
            Color fillCol = isHover
                ? new Color(0.12f, 0.38f, 0.55f, 0.98f)
                : new Color(0.05f, 0.22f, 0.36f, 0.92f);
            Color borderCol = isHover
                ? GameColors.BrightYellow
                : new Color(0f, 0.95f, 1f, 0.85f);

            DrawCornerBox(r, fillCol, borderCol, 14f * uiScale);

            // 内部高光导线
            if (isHover)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, 2f), m_whiteTex);
                GUI.color = Color.white;
            }

            m_btnStyle.fontSize = Mathf.RoundToInt(22 * uiScale);
            m_btnStyle.fontStyle = FontStyle.Bold;
            m_btnStyle.normal.textColor = isHover ? Color.white : GameColors.BrightYellow;
            GUI.Label(r, text, m_btnStyle);

            if (Event.current != null && Event.current.type == EventType.MouseDown && isHover)
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        private void DrawCard(Rect r, string title, Color titleColor, string[] bullets, float uiScale)
        {
            DrawBox(r, new Color(0.02f, 0.05f, 0.12f, 0.85f), new Color(titleColor.r, titleColor.g, titleColor.b, 0.65f));

            float pad = 12f * uiScale;
            float cy = r.y + pad;
            float cw = r.width - pad * 2f;

            m_cardTitleStyle.fontSize = Mathf.RoundToInt(16 * uiScale);
            m_cardTitleStyle.normal.textColor = titleColor;
            GUI.Label(new Rect(r.x + pad, cy, cw, 22f * uiScale), title, m_cardTitleStyle);
            cy += 24f * uiScale;

            DrawHLine(r.x + pad, cy, cw, new Color(titleColor.r, titleColor.g, titleColor.b, 0.35f));
            cy += 8f * uiScale;

            m_bodyStyle.fontSize = Mathf.RoundToInt(13 * uiScale);
            m_bodyStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            foreach (var b in bullets)
            {
                GUI.Label(new Rect(r.x + pad, cy, cw, 34f * uiScale), b, m_bodyStyle);
                cy += 34f * uiScale;
            }
        }

        private void DrawPauseModal(float screenW, float screenH, float uiScale)
        {
            float w = Mathf.Min(480f * uiScale, screenW * 0.9f);
            float h = 260f * uiScale;
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            DrawCornerBox(new Rect(x, y, w, h), new Color(0.04f, 0.07f, 0.16f, 0.96f), GameColors.BrightYellow, 14f * uiScale);

            m_modalTitleStyle.fontSize = Mathf.RoundToInt(26 * uiScale);
            m_modalTitleStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(x, y + 25f * uiScale, w, 34f * uiScale), "战术暂停 (GAME PAUSED)", m_modalTitleStyle);

            m_modalBodyStyle.fontSize = Mathf.RoundToInt(15 * uiScale);
            m_modalBodyStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 70f * uiScale, w, 24f * uiScale), "空战进行中，已切断战术交火指令", m_modalBodyStyle);

            float btnW = 220f * uiScale;
            float btnH = 46f * uiScale;
            float btnY = y + 110f * uiScale;

            if (DrawButton(new Rect(x + (w - btnW) * 0.5f, btnY, btnW, btnH), "▶ 继续战斗 [P / ESC]",
                GameColors.BrightGreen, new Color(0.05f, 0.25f, 0.15f), Color.white, Mathf.RoundToInt(16 * uiScale)))
            {
                ResumeGameRequested = true;
            }

            if (DrawButton(new Rect(x + (w - btnW) * 0.5f, btnY + btnH + 12f * uiScale, btnW, btnH), "↩ 返回主菜单 [Q]",
                GameColors.BrightRed, new Color(0.25f, 0.05f, 0.05f), Color.white, Mathf.RoundToInt(16 * uiScale)))
            {
                TitleGameRequested = true;
            }
        }

        private void DrawGameOverModal(float screenW, float screenH, int score, int highScore, float uiScale)
        {
            float w = Mathf.Min(540f * uiScale, screenW * 0.9f);
            float h = 330f * uiScale;
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            DrawCornerBox(new Rect(x, y, w, h), new Color(0.14f, 0.03f, 0.03f, 0.96f), GameColors.BrightRed, 14f * uiScale);

            m_modalTitleStyle.fontSize = Mathf.RoundToInt(28 * uiScale);
            m_modalTitleStyle.normal.textColor = GameColors.BrightRed;
            GUI.Label(new Rect(x, y + 25f * uiScale, w, 36f * uiScale), "=== 任务失败 (MISSION FAILED) ===", m_modalTitleStyle);

            m_modalBodyStyle.fontSize = Mathf.RoundToInt(17 * uiScale);
            m_modalBodyStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(x, y + 75f * uiScale, w, 26f * uiScale), $"本次战斗积分 : {score:D7}", m_modalBodyStyle);

            m_modalBodyStyle.normal.textColor = GameColors.BrightCyan;
            GUI.Label(new Rect(x, y + 105f * uiScale, w, 26f * uiScale), $"历史最高纪录 : {highScore:D7}", m_modalBodyStyle);

            float btnW = 240f * uiScale;
            float btnH = 48f * uiScale;
            float btnY = y + 155f * uiScale;

            if (DrawButton(new Rect(x + (w - btnW) * 0.5f, btnY, btnW, btnH), "↻ 再次出击 [空格 / 回车]",
                GameColors.BrightYellow, new Color(0.35f, 0.25f, 0.05f), Color.white, Mathf.RoundToInt(17 * uiScale)))
            {
                RestartGameRequested = true;
            }

            if (DrawButton(new Rect(x + (w - btnW) * 0.5f, btnY + btnH + 12f * uiScale, btnW, btnH), "↩ 返回主菜单 [Q / ESC]",
                new Color(0.6f, 0.6f, 0.6f), new Color(0.15f, 0.15f, 0.15f), Color.white, Mathf.RoundToInt(16 * uiScale)))
            {
                TitleGameRequested = true;
            }
        }

        private void DrawVictoryModal(float screenW, float screenH, int score, float uiScale)
        {
            float w = Mathf.Min(560f * uiScale, screenW * 0.9f);
            float h = 330f * uiScale;
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            DrawCornerBox(new Rect(x, y, w, h), new Color(0.02f, 0.15f, 0.05f, 0.96f), GameColors.BrightGreen, 14f * uiScale);

            m_modalTitleStyle.fontSize = Mathf.RoundToInt(28 * uiScale);
            m_modalTitleStyle.normal.textColor = GameColors.BrightGreen;
            GUI.Label(new Rect(x, y + 25f * uiScale, w, 36f * uiScale), "★★★ 战役大捷 (VICTORY) ★★★", m_modalTitleStyle);

            m_modalBodyStyle.fontSize = Mathf.RoundToInt(17 * uiScale);
            m_modalBodyStyle.normal.textColor = GameColors.BrightYellow;
            GUI.Label(new Rect(x, y + 75f * uiScale, w, 26f * uiScale), "灭世母舰已被彻底摧毁！银河系已恢复和平！", m_modalBodyStyle);

            m_modalBodyStyle.normal.textColor = GameColors.BrightCyan;
            GUI.Label(new Rect(x, y + 105f * uiScale, w, 26f * uiScale), $"最终战斗总分 : {score:D7}", m_modalBodyStyle);

            float btnW = 260f * uiScale;
            float btnH = 48f * uiScale;
            float btnY = y + 155f * uiScale;

            if (DrawButton(new Rect(x + (w - btnW) * 0.5f, btnY, btnW, btnH), "▶ 开启无尽深空模式 [空格 / 回车]",
                GameColors.BrightGreen, new Color(0.05f, 0.35f, 0.15f), Color.white, Mathf.RoundToInt(16 * uiScale)))
            {
                RestartGameRequested = true;
            }

            if (DrawButton(new Rect(x + (w - btnW) * 0.5f, btnY + btnH + 12f * uiScale, btnW, btnH), "↩ 返回主菜单 [Q / ESC]",
                new Color(0.6f, 0.6f, 0.6f), new Color(0.15f, 0.15f, 0.15f), Color.white, Mathf.RoundToInt(16 * uiScale)))
            {
                TitleGameRequested = true;
            }
        }

        private void DrawBossBar(float screenW, WaveManager waveMgr, float uiScale)
        {
            float playW = screenW * 0.72f;
            float barW = Mathf.Min(560f * uiScale, playW * 0.85f);
            float barH = 24f * uiScale;
            float x = (playW - barW) * 0.5f;
            float y = 14f * uiScale;

            float ratio = waveMgr.GetBossHpRatio();

            m_modalTitleStyle.fontSize = Mathf.RoundToInt(18 * uiScale);
            m_modalTitleStyle.normal.textColor = GameColors.BrightRed;
            GUI.Label(new Rect(x, y, barW, 24f * uiScale),
                $"⚠ 极限警报: 灭世旗舰母舰 (MOTHERSHIP) 残余装甲 [{Mathf.RoundToInt(ratio * 100)}%] ⚠", m_modalTitleStyle);

            y += 26f * uiScale;
            DrawBar(new Rect(x, y, barW, barH), Mathf.RoundToInt(ratio * 1000), 1000, GameColors.BrightRed, new Color(0.4f, 0.05f, 0.05f));
        }

        // ------------------------------------------------------------------------
        // 【UI 基础绘制辅助函数 · 科技感边框与交互按钮】
        // ------------------------------------------------------------------------

        private bool DrawButton(Rect r, string text, Color borderCol, Color fillCol, Color textCol, int fontSize)
        {
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            bool isHover = r.Contains(mouse);

            // 悬停动画高亮
            Color curFill = isHover ? Color.Lerp(fillCol, Color.white, 0.22f) : fillCol;
            Color curBorder = isHover ? Color.white : borderCol;
            DrawBox(r, curFill, curBorder);

            m_btnStyle.fontSize = fontSize;
            m_btnStyle.normal.textColor = isHover ? Color.white : textCol;
            GUI.Label(r, text, m_btnStyle);

            if (Event.current != null && Event.current.type == EventType.MouseDown && isHover)
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        private void DrawCornerBox(Rect r, Color bg, Color border, float cornerLen)
        {
            DrawBox(r, bg, border);

            // 绘制四角加粗科技感高亮角标
            float thick = 3f;
            GUI.color = border;
            // 左上角
            GUI.DrawTexture(new Rect(r.x, r.y, cornerLen, thick), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, thick, cornerLen), m_whiteTex);
            // 右上角
            GUI.DrawTexture(new Rect(r.x + r.width - cornerLen, r.y, cornerLen, thick), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x + r.width - thick, r.y, thick, cornerLen), m_whiteTex);
            // 左下角
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - thick, cornerLen, thick), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - cornerLen, thick, cornerLen), m_whiteTex);
            // 右下角
            GUI.DrawTexture(new Rect(r.x + r.width - cornerLen, r.y + r.height - thick, cornerLen, thick), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x + r.width - thick, r.y + r.height - cornerLen, thick, cornerLen), m_whiteTex);

            GUI.color = Color.white;
        }

        private void DrawBox(Rect r, Color bg, Color border)
        {
            // 背景
            GUI.color = bg;
            GUI.DrawTexture(r, m_whiteTex);

            // 边框
            GUI.color = border;
            float bw = 2f;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, bw), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - bw, r.width, bw), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, bw, r.height), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x + r.width - bw, r.y, bw, r.height), m_whiteTex);

            GUI.color = Color.white;
        }

        private void DrawHLine(float x, float y, float w, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, 2f), m_whiteTex);
            GUI.color = Color.white;
        }

        private void DrawBar(Rect r, int current, int max, Color fg, Color empty)
        {
            if (max <= 0) max = 1;
            if (current < 0) current = 0;
            if (current > max) current = max;

            float ratio = (float)current / max;

            // 空槽底色
            GUI.color = empty;
            GUI.DrawTexture(r, m_whiteTex);

            // 填充色条
            if (ratio > 0f)
            {
                GUI.color = fg;
                GUI.DrawTexture(new Rect(r.x, r.y, r.width * ratio, r.height), m_whiteTex);
            }

            // 外部亮白边框
            GUI.color = new Color(0.85f, 0.9f, 1.0f, 0.6f);
            float bw = 1.5f;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, bw), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - bw, r.width, bw), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, bw, r.height), m_whiteTex);
            GUI.DrawTexture(new Rect(r.x + r.width - bw, r.y, bw, r.height), m_whiteTex);

            GUI.color = Color.white;
        }
    }
}
