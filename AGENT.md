# 《星际突袭：银河战线》(Galaxy Strike) - AI Agent 核心开发与协同指南 (AGENT.md)

> **欢迎你，AI Agent / 开发者！**  
> 本文件是本项目为 AI 智能体（LLM Coding Agents）量身定制的**最高优先级工程协议与架构说明书**。  
> 无论你是第一次接触本仓库，还是进行增量功能迭代、数值平衡调优、视觉特效重构或 Bug 修复，请**优先阅读并严格遵守**本指南中定义的架构边界与不变量规则。

---

## 目录
1. [项目全貌与技术栈定位](#1-项目全貌与技术栈定位)
2. [核心架构与代码组织架构图](#2-核心架构与代码组织架构图)
3. [AI Agent 必须遵循的五大架构不变量 (Invariants)](#3-ai-agent-必须遵循的五大架构不变量-invariants)
4. [坐标系与视口投影规范 (核心数学模型)](#4-坐标系与视口投影规范-核心数学模型)
5. [AI Agent 标准开发工作流 (Playbooks)](#5-ai-agent-标准开发工作流-playbooks)
6. [测试验证与质量防护协议](#6-测试验证与质量防护协议)
7. [AI 常见陷阱与反模式清单 (Anti-Patterns)](#7-ai-常见陷阱与反模式清单-anti-patterns)

---

## 1. 项目全貌与技术栈定位

* **游戏类型**：经典街机风格 2D 太空射击游戏 (STG / Vertical Arcade Space Shooter)
* **引擎环境**：Unity 6000.x (Universal 2D Pipeline)
* **开发语言**：C# 10.0+ / .NET Standard 2.1
* **核心架构哲学**：**纯 C# 核心玩法逻辑与 Unity 渲染表现层绝对解耦**
  * 原项目脱胎于高质量的现代 C++ 控制台打飞机游戏，具有极其精纯、严密且高效的面向对象与状态机设计。
  * 游戏核心物理碰撞、数值演进、波次演变**完全不依赖 Unity GameObject / MonoBehaviour 运行时**，可以在纯控制台/纯 Headless 环境下瞬间完成全自动化单元测试。
  * Unity 引擎仅作为表现层桥接器（SpriteRenderer 批渲染、IMGUI 高清赛博视口 HUD、程序化实时数学音频合成）。

---

## 2. 核心架构与代码组织架构图

```
d:/Code/fireu/
├── AGENT.md                                 # [当前文件] AI 核心开发准则与架构索引
├── Logs/Editor.log                          # Unity 运行时与测试输出日志
└── Assets/GalaxyStrike/
    ├── README.md                            # 项目设计思想与 OOP 演化总览
    ├── Editor/                              # 编辑器扩展与自动化测试
    │   ├── README.md                        # 编辑器层设计说明
    │   ├── GalaxyStrikeLogicTests.cs        # 12/12 项 Headless 自动化单元测试集
    │   ├── GalaxyStrikeSceneSetup.cs        # 场景一键构建与自动就绪引导
    │   ├── GalaxyStrikePrefabGenerator.cs   # 预制体自动化生成工厂
    │   └── GalaxyStrikeArtImporter.cs       # 贴图自动化导入与九宫切片处理
    ├── Prefabs/                             # 游戏预制体模型资源
    │   └── README.md
    ├── Resources/GalaxyStrikeArt/           # 游戏原画素材与切片图集
    │   ├── README.md                        # 美术图集规范与调色板说明
    │   ├── TitleBackground.png              # 1080P 卡通科幻开始界面底图
    │   └── Sprites/                         # 战机、敌机、弹道、特效切片
    ├── Scenes/                              # 场景目录
    │   └── GalaxyStrikeMain.unity           # 游戏主入口场景
    └── Scripts/                             # 核心源码目录 (分层高内聚)
        ├── README.md                        # 源码分层总览
        ├── GalaxyStrikeGame.cs              # 游戏总体调度器 (主循环/物理分发/输入)
        ├── GalaxyStrikeBootstrapper.cs      # 全局就地引导器 (支持任意场景 F1 呼出)
        ├── Core/                            # 【纯逻辑】基础类型与数学配置
        │   ├── README.md                    # Core 层架构说明
        │   ├── GameConfig.cs                # 网格常量、视界规格、坐标转换函数
        │   └── Types.cs                     # Rect2D, Vector2Extensions, GameRandom, GameColors
        ├── Entities/                        # 【纯逻辑】面向对象实体层
        │   ├── README.md                    # 实体层继承树与多态规范
        │   ├── Entity.cs                    # 实体抽象基类 (AABB 碰撞)
        │   ├── Player.cs                    # 玩家主力战机 (5阶机体/5阶火力/核弹)
        │   ├── Enemy.cs                     # 敌机抽象基类
        │   ├── ScoutEnemy.cs                # 蜂群冲锋侦察机 (3种编队模式)
        │   ├── GunshipEnemy.cs              # 定高巡航炮艇 (双联瞄准直射)
        │   ├── BomberEnemy.cs               # 重装轰炸机 (扇形三向落弹)
        │   ├── EliteCruiser.cs              # 精英巡洋舰 (正弦悬停机动，双螺旋弹幕)
        │   ├── BossEnemy.cs                 # 灭世星际母舰 (三阶段形态与狂暴弹幕)
        │   ├── Bullet.cs                    # 弹药体系 (激光/穿透/自导飞弹/螺旋弹)
        │   └── PowerUp.cs                   # 掉落物资 (火力P/经验U/护盾S/核弹B/积分$)
        ├── Systems/                         # 【纯逻辑】游戏子系统管理器
        │   ├── README.md                    # 系统层生命周期说明
        │   ├── WaveManager.cs               # 波次编排状态机与出兵阵列
        │   ├── StarfieldManager.cs          # 3层视差星空粒子与超光速跃迁模拟
        │   └── ParticleSystemManager.cs     # 爆炸/破片/双层清屏冲击波与电弧分叉
        ├── Presentation/                    # 【Unity桥接】渲染层
        │   ├── README.md                    # 表现层与对象池设计说明
        │   ├── GalaxyStrikeRenderer.cs      # 2D SpriteRenderer 批处理与动态缩放
        │   └── GalaxyStrikeVisualSize.cs    # 视觉包围盒精确适配算法
        ├── UI/                              # 【Unity桥接】界面层
        │   ├── README.md                    # HUD 与战术边框规范
        │   └── GalaxyStrikeHUD.cs           # 战术赛博视口边框、遥测数据、标题屏幕
        └── Audio/                           # 【Unity桥接】音频层
            └── ProceduralSoundManager.cs    # 纯数学波形程序化 8-bit 音效与 BGM 合成
```

---

## 3. AI Agent 必须遵循的五大架构不变量 (Invariants)

### 📌 不变量 1：纯逻辑层对 Unity 引擎的绝对隔离
* `Core/`、`Entities/`、`Systems/` 目录下的代码**禁止引用 `UnityEngine.GameObject`、`MonoBehaviour` 或 Unity 场景对象**。
* 物理运算、血量结算、弹道推进、经验升阶、波次倒计时必须完全基于纯 C# 数学计算；
* 仅允许使用 `UnityEngine.Vector2`、`UnityEngine.Color` 等轻量级基础结构体以简化跨平台数学。

### 📌 不变量 2：零 GC 内存分配原则 (Zero-Allocation Loop)
* `Update()`、`UpdatePhysics()` 与 `RenderFrame()` **每帧执行的代码路径中严禁 `new` 任何堆对象**（如临时 `List<T>`、委托匿名类、或动态创建 GameObject）。
* 所有的实体、子弹、特效粒子、震荡波、渲染器必须采用**预分配对象池 (Object Pool)** 循环复用。

### 📌 不变量 3：视口分区不变量 (74% 战区 / 26% 遥测边栏)
* 整个游戏视口横向划分为：
  * **主战区 (Combat Zone)**：占左侧宽度的 **$74\%$**，周围由 `DrawCombatViewportFrame` 绘制双层高科技边框、四角瞄准角标与高度标尺；
  * **战术侧边栏 (Telemetry Sidebar)**：占右侧宽度的 **$26\%$**，专门呈现机体完整度、护盾、武器等级、核弹余量与波次雷达。
* 所有敌机与战机的机动范围被 `GameConfig.VIRTUAL_WIDTH * 0.74f` 严格限制在主战区内，绝对不可漂移至右侧遥测区域。

### 📌 不变量 4：双层环快速清屏炸弹规范
* 己方核弹释放规则：
  * **极速双层光环**：外层先导环速度 $\approx 115$，内层明亮伴随环速度 $\approx 92$，整体 $0.35 \sim 0.45$ 秒内完成扩散并以幂次曲线淡出；
  * **微型能量分叉**：伴随双层环向外辐射 $12 \sim 16$ 组微型能量枝桠电弧 (`ParticleKind.BranchFork`)，迅速消散；
  * **稀疏不拖沓**：爆炸火花与敌机殉爆严禁堆积大片浓厚烟尘，单点爆破火球控制在 $1$ 枚以内，保持画面干净利落。

### 📌 不变量 5：全自动化单元测试守护
* 任何对核心逻辑的修改，必须确保 `Assets/GalaxyStrike/Editor/GalaxyStrikeLogicTests.cs` 的 **12 项测试用例全部通过 (12/12 PASS)**。

---

## 4. 坐标系与视口投影规范 (核心数学模型)

本项目包含两套相互转换的坐标系统，AI 开发者在处理位置计算时必须明确当前所处的空间：

```
逻辑网格空间 (Grid Space)              Unity 世界空间 (World Space)
(0,0) ----------- X: 80                +Y: +4.8f  (顶部)
  |                                       |
  |  主战区     | 遥测边栏                |       (0,0) 中心
  |  X: 0~59.2  | X: 59.2~80             |
  v                                       v
Y: 24                                  -Y: -4.8f  (底部)
                                      -X: -7.8f        +X: +7.8f
```

### 转换公式
* 网格向世界坐标映射：
  $$\text{world.x} = (\text{grid.x} - 40.0) \times 0.20f$$
  $$\text{world.y} = (12.0 - \text{grid.y}) \times 0.40f$$
* 已经封装于：[`GameConfig.GridToWorld(Vector2 gridPos)`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Core/GameConfig.cs)

---

## 5. AI Agent 标准开发工作流 (Playbooks)

### 场景 A：添加一种新型敌机 (New Enemy Type)
1. 在 [`Enemy.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Entities/Enemy.cs) 的 `EnemyType` 枚举中增加新类型声明；
2. 在 `Scripts/Entities/` 下新建类（如 `CruiserEnemy.cs`），继承自 `Enemy`；
3. 重写 `UpdateBehavior(float dt, Vector2 playerPos)` 实现特色航迹；
4. 重写 `Shoot(List<Bullet> enemyBullets, float dt, Action<SfxType> playSfx)` 实现攻击弹道；
5. 在 [`WaveManager.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Systems/WaveManager.cs) 中编排该敌机的波次出现时机；
6. 在 [`GalaxyStrikeRenderer.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Presentation/GalaxyStrikeRenderer.cs) 注册对应的 Sprite 或 Prefab 映射。

### 场景 B：增加新的掉落补给 (New PowerUp)
1. 在 [`PowerUp.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Entities/PowerUp.cs) 的 `PowerUpType` 增加枚举值；
2. 在 `Player.cs` 的 `CollectPowerUp(PowerUpType type, ...)` 中实现吸收增益效果；
3. 在 `GalaxyStrikeRenderer.cs` 字典中添加对应的贴图图标切片映射。

### 场景 C：调整 UI 或战术边框外观
1. 界面元素集中在 [`GalaxyStrikeHUD.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/UI/GalaxyStrikeHUD.cs)；
2. 绘制战术边框请修改 `DrawCombatViewportFrame`；
3. 修改主菜单外观请修改 `DrawTitleScreen`；
4. 保持 `GUI.skin` 恢复和颜色栈压栈/出栈平衡。

---

## 6. 测试验证与质量防护协议

每次修改完代码后，AI Agent 请务必按以下步骤进行闭环验证：

1. **查看 Unity 日志文件**：
   ```powershell
   Get-Content "D:\Code\fireu\Logs\Editor.log" -Tail 30
   ```
2. **确认测试通过状态**：
   确认日志中出现 `[Galaxy Strike] 全部 12 项测试全部通过`，且无任何黄色编译警告或红色报错。
3. **保持 HTML 效果预览同步**：
   若涉及重大视觉设计迭代，需同步更新位于 `C:\Users\eric\.gemini\antigravity\brain\<conversation-id>\galaxy_strike_design_preview.html` 的交互式设计预览文件。

---

## 7. AI 常见陷阱与反模式清单 (Anti-Patterns)

| 序号 | 错误做法 (Anti-Pattern) | 正确做法 (Best Practice) |
| :---: | :--- | :--- |
| ❌ 1 | 在实体类中使用 `MonoBehaviour` 或 `Start()` / `Update()` | 继承纯 C# 类 `Entity`，由 `GalaxyStrikeGame` 统一驱动 `Update(dt)` |
| ❌ 2 | 在 `RenderParticles` 或 `RenderBullets` 中调用 `Instantiate` / `Destroy` | 使用预分配的 `EnsurePoolSize` 列表对象池，通过 `SetActive(false)` 回收 |
| ❌ 3 | 在游戏逻辑计算中使用屏幕像素 (Pixel) | 统一使用逻辑网格坐标 `[0, 80] x [0, 24]`，渲染时自动转换 |
| ❌ 4 | 引入庞大的外部音频 MP3/WAV 资产库 | 保持使用 `ProceduralSoundManager` 进行零资源依赖的数学算法音效生成 |
| ❌ 5 | 爆炸粒子过多导致屏幕浓密遮挡视野 | 保持粒子稀疏清脆，火球单发即逝，碎片少量放射，绝不遮挡弹幕 |
