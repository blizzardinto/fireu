# 《星际突袭：银河战线》(Galaxy Strike) - 游戏系统架构与详细设计规范 (design.md)

---

## 1. 总体架构哲学与解耦体系 (Architecture Philosophy)

本项目的设计核心哲学为：**“纯 C# 核心玩法逻辑与 Unity 表现层绝对解耦”**。

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Galaxy Strike 系统层次拓扑                      │
├────────────────────────────────────────────────────────────────────────┤
│ 【调度层】GalaxyStrikeGame (60~240 FPS 主循环 / 输入捕获 / 跨系统协调)  │
├────────────────────────────────────────────────────────────────────────┤
│ 【纯逻辑层】(Pure C# - 零 GameObject / 零 MonoBehaviour 依赖)           │
│   ├── Core: Types, GameConfig (网格规格, 物理步长, 亚像素坐标模型)      │
│   ├── Entities: Player, Enemy, Bullet, PowerUp (5阶进化, AABB 碰撞)    │
│   └── Systems: WaveManager, StarfieldManager, ParticleSystemManager    │
├────────────────────────────────────────────────────────────────────────┤
│ 【表现与呈现层】(Unity 桥接层)                                         │
│   ├── Presentation: GalaxyStrikeRenderer (全息网格/侧倾/拉伸/对象池)   │
│   ├── UI: GalaxyStrikeHUD (赛博战术仪表盘/动态字号/Boss超长血条)       │
│   └── Audio: ProceduralSoundManager (底层 PCM 数学实时合成 8-bit 音效) │
└────────────────────────────────────────────────────────────────────────┘
```

### 1.1 核心解耦收益
1. **零依赖可测试性 (Headless Testability)**：所有关卡、数值升级公式、碰撞几何相交、穿透算法、追踪导引在纯 C# 单元测试框架中秒级执行，不启动 Unity 场景即可全量回归（见 `GalaxyStrikeLogicTests.cs`）。
2. **逻辑状态无污染 (Single Source of Truth)**：实体的坐标、速度、血量、存活状态完全由逻辑类独立持有，表现层仅在 `LateUpdate` 中做单向只读投射（Read-only projection），避免了物理演算与动画渲染相互篡改状态的竞态问题。
3. **确定性模拟 (Deterministic Simulation)**：无论是 60Hz 还是 240Hz 高刷屏，物理与数值演算均基于显式时间步长 `dt`，杜绝帧率相关的不稳定 Bug。

---

## 2. 坐标系统与亚像素投影数学模型 (Coordinate & Projection Model)

游戏内部使用双层坐标系统，既保留了原控制台字符网格的经典几何比例，又实现了现代平滑抗锯齿的亚像素连续世界渲染。

```
      逻辑网格空间 (Grid Coordinates)                Unity 世界空间 (World Space)
  (1, 1) ┌──────────────────────────┐ (104, 1)    (-10.4, 4.3) ┌──────────────┐ (10.4, 4.3)
         │                          │                          │              │
         │   空战交火空域 (104x43)  │            ───►          │   (0, 0, 0)  │
         │                          │                          │              │
 (1, 43) └──────────────────────────┘ (104, 43)  (-10.4, -4.3) └──────────────┘ (10.4, -4.3)
```

### 2.1 映射矩阵推导
* **交火空域规格**：横向 $X \in [1, 104]$（宽 104 格），纵向 $Y \in [1, 43]$（高 43 格）。
* **逻辑网格中心**：
  $$\text{Origin}_X = \frac{1 + 104}{2} = 52.5, \quad \text{Origin}_Y = \frac{1 + 43}{2} = 22.0$$
* **世界缩放系数**：$S_{\text{world}} = 0.2$。
* **投射公式**：
  $$W_x = (G_x - 52.5) \times 0.2, \quad W_y = (22.0 - G_y) \times 0.2, \quad W_z = 0$$
  * *注：逻辑网格 $Y$ 向下递增，Unity 世界空间 $Y$ 向上递增，故纵向取反。*

### 2.2 视口分割与相机黄金分割构图
* **正交相机尺寸**：`orthographicSize = 5.2f`。
* **水平视口偏置**：相机中心定于 `(3.2, 0.0, -10.0)`。
  * **左侧 75% 视口**：完整呈现 $20.8 \times 8.6$ 的太空交火空域。
  * **右侧 25% 视口**：承载赛博全息战术 HUD 遥测仪表盘（机体装甲条、护盾条、经验演化、击坠榜）。

---

## 3. 面向对象核心模型与设计模式 (OOP Domain Model)

```
                            ┌───────────────┐
                            │    Entity     │
                            │  (抽象基类)   │
                            └───────┬───────┘
                                    │
       ┌──────────────┬─────────────┴─────────────┬──────────────┐
       ▼              ▼                           ▼              ▼
┌──────────────┐┌──────────────┐           ┌──────────────┐┌──────────────┐
│    Player    ││    Bullet    │           │   PowerUp    ││    Enemy     │
│ (玩家战机)   ││  (弹药体系)  │           │  (战术补给)  ││ (敌机基类)   │
└──────────────┘└──────────────┘           └──────────────┘└──────┬───────┘
                                                                  │
              ┌────────────────┬────────────────┬─────────────────┼────────────────┐
              ▼                ▼                ▼                 ▼                ▼
       ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  ┌──────────────┐ ┌──────────────┐
       │  ScoutEnemy  │ │GunshipEnemy  │ │ BomberEnemy  │  │ EliteCruiser │ │  BossEnemy   │
       │  (蜂群侦察)  │ │ (巡航炮艇)   │ │  (重装轰炸)  │  │  (精英战巡)  │ │ (灭世母舰)   │
       └──────────────┘ └──────────────┘ └──────────────┘  └──────────────┘ └──────────────┘
```

### 3.1 抽象与封装 (Abstraction & Encapsulation)
* `Entity` 封装了 `m_pos` (Vector2)、`m_vel` (Vector2)、`m_width`、`m_height`、`m_hp`、`m_active` 以及标准的 AABB 相交检测算法 `GetBounds().Intersects(other.GetBounds())`。
* 外部严禁直接侵入修改受损逻辑，所有伤害由 `TakeDamage(int dmg)` 统一流转，优先触发护盾吸收矩阵，护盾打穿后平滑穿透至装甲并重置脱战充能计时器。

### 3.2 继承与开闭原则 (Inheritance & Polymorphism / OCP)
* `Enemy` 派生出 5 类具象战术敌机，彻底消除臃肿脆弱的 `switch(type)`，主控中枢仅持有 `List<Enemy>` 基类容器。
* 主循环通过虚方法 `e.UpdateWithTarget(dt, playerPos)` 与 `e.Shoot(bullets, playerPos, dt)` 进行动态多态分发，新增任意新型号敌机仅需继承派生，无需改动主调度循环一行代码。

---

## 4. 流体动力学与丝滑渲染管道 (Flight Kinematics & Silky Rendering)

针对传统网格游戏移动“生硬跳步、频闪断裂”的痛点，构建了四重现代流体动力学表现系统：

### 4.1 柔性速度阻尼物理模型 (Velocity Damping)
玩家按键不再直接瞬间硬切速度，而是引入临界阻尼速度逼近方程：
$$\vec{V}_{\text{target}} = \vec{D}_{\text{input}} \times V_{\text{speed}}$$
$$\vec{V}_{\text{smoothed}} = \text{Lerp}\left(\vec{V}_{\text{smoothed}}, \vec{V}_{\text{target}}, \text{Clamp01}(\alpha \cdot dt)\right)$$
* 当有推杆输入时，$\alpha = 28.0$，在 $0.035$ 秒内迅速响应（消除迟滞感）；
* 当松开推杆时，$\alpha = 24.0$ 自然柔和刹车，起步急停顺滑如丝。

### 4.2 空气动力学滚转角 (Aerodynamic Banking Roll)
战机水平机动时依据横向位移方向沿 Z 轴产生机动倾斜反馈：
$$\theta_{\text{target}} = -D_x \times 14.0^\circ$$
$$\theta_{\text{current}} = \text{Lerp}\left(\theta_{\text{current}}, \theta_{\text{target}}, \text{Clamp01}(16.0 \cdot dt)\right)$$
* 转向机动平滑侧倾，推杆回正时战机平滑反弹归平。

### 4.3 速度自适应运动拉伸 (Velocity Motion Stretch)
对于高速子弹（速度高达 $50$ 格/秒），为消除人眼在 60Hz 采样下的频闪虚线效应（Strobe Effect），视觉渲染纵向长度与即时速率形成正比拉伸：
$$L_{\text{visual}} = L_{\text{base}} \times \max\left(1.0, 1.0 + (\|\vec{V}\| - 22.0) \times 0.020\right)$$
* 激光额外放大 $1.35$ 倍，弹身旋转角精准对齐速度向量 $\text{Atan2}(V_y, V_x)$，彻底将离散跳跃点化为连续贯穿的“流光光刃”。

### 4.4 全息赛博网格与四层视差星空
* **赛博战术网格**：程序化生成 $64\times 64$ 无缝重复贴图，覆盖战斗区域网格，随飞行深度平滑向下滚动，提供精确亚像素运动感知。
* **四层密集星空**：
  * **Layer 0 (深空微尘)**：细密、暗弱、微蓝紫漂移（速度 $2.0 \sim 4.0$）；
  * **Layer 1 (中景恒星)**：青蓝/金黄明亮星芒（速度 $6.0 \sim 9.5$）；
  * **Layer 2 (近景耀星)**：耀眼白光星芒（速度 $12.0 \sim 16.0$）；
  * **Layer 3 (高速流光)**：纵向拉伸流光线段（速度 $22.0 \sim 30.0$），营造超高速冲刺感。

---

## 5. 武器系统与弹幕微积分 (Weapon & Barrage Calculus)

```
[Lv.1 Single]        [Lv.2 Twin]        [Lv.3 Spread]        [Lv.4 Laser]        [Lv.5 Salvo]
      │                 │  │               \  │  /             \ │ │ │ /          \ \ | | / /
      *                 *  *                * * *              * | * | *          * * | * *
                                                                   (穿透)        (穿透+2追踪飞弹)
```

### 5.1 智能微型飞弹导引算法 (Homing Missile Guidance)
飞弹具备物理质量惯性，向最近敌机目标做角平滑插值（Turn Rate Slerp）：
$$\vec{D}_{\text{desired}} = \text{NormalizeSafe}(\vec{P}_{\text{target}} - \vec{P}_{\text{missile}})$$
$$\vec{D}_{\text{current}} = \text{NormalizeSafe}(\vec{V}_{\text{missile}})$$
$$\vec{D}_{\text{new}} = \text{NormalizeSafe}\left(\vec{D}_{\text{current}} + \vec{D}_{\text{desired}} \times (8.0 \cdot dt)\right)$$
$$\vec{V}_{\text{missile}} = \vec{D}_{\text{new}} \times V_{\text{speed}}$$

### 5.2 灭世母舰螺旋地狱弹幕微积分 (Boss Helix Barrage)
Boss 狂暴状态下以自身为中心发射能量球，子弹在飞行中依据固定角速度微分方程自主旋转，形成大范围旋转花瓣弹幕：
$$\theta_{t+dt} = \text{Atan2}(V_y, V_x) + \omega \cdot dt \quad (\omega = 3.5\text{ rad/s})$$
$$V_x = \cos(\theta_{t+dt}) \times \|\vec{V}\|, \quad V_y = \sin(\theta_{t+dt}) \times \|\vec{V}\|$$

---

## 6. 关卡状态机与波次拓扑 (Wave State Machine)

波次管理器封装了 4 阶段状态机（`Preparing` -> `Spawning` -> `Active` -> `Clear`）：

```
[波次准备 2.0s] ──► [集中编队拓扑批量生成] ──► [全场交火清除监控] ──► [波次结算清算]
                             │
     ┌───────────────────────┼───────────────────────┐
     ▼                       ▼                       ▼
  Wave 1                  Wave 2                  Wave 5 (Boss)
V字突击编队(7架)        双翼钳形合围(10架)      灭世母舰旗舰登场
+ 蛇形扫荡扫荡           + 重装轰炸前锋          + 曲率引擎过载跃迁
```

---

## 7. 纯数学程序化音频合成引擎 (Procedural Sound Engine)

为实现零音频资源依赖与极致包体轻量化，本项目完全通过数学波形实时生成所有声音：

### 7.1 数学振荡器 (Waveform Oscillators)
* **方波 (Square Wave)**：高能量主频音效（射击、升级）：
  $$S(t) = \text{sgn}(\sin(2\pi f t))$$
* **三角波 (Triangle Wave)**：圆润贝斯与背景和弦：
  $$S(t) = \frac{2}{\pi}\arcsin(\sin(2\pi f t))$$
* **白噪声 (White Noise)**：带衰减高斯噪声用于爆破与撞击。

### 7.2 异步 Chiptune BGM 编排器
内置轻量级 8-bit 音轨序列器（Tracker），基于四度、五度和弦步进，在独立的音频线程上实时混音写入输出缓冲区，彻底免去加载音频剪辑的开销。

---

## 8. 高刷新率显示器自适应 (High-Hz Adaptation)

* **解锁高刷限制**：`QualitySettings.vSyncCount = 1` 配合 `Application.targetFrameRate = -1`，自适应玩家硬件（60Hz、120Hz、144Hz、240Hz）。
* **帧率无关护盾充能累加器**：
  由于 $15.0 \times dt$ 在高刷时小于 $0.1$，传统 `RoundToInt` 将导致回盾归零；设计引入 `m_shieldRegenAccumulator` 浮点数跨帧积分，保证任何帧率下回盾恒定为 $15\text{ HP/s}$。
