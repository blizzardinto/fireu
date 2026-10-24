# 《星际突袭：银河战线》(Galaxy Strike) - Unity 6000 现代重构版

[![Unity](https://img.shields.io/badge/Unity-6000.x-blue.svg?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-10.0%20%2F%20.NET%20Standard%202.1-green.svg)](https://docs.microsoft.com/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-Decoupled%20Pure%20C%23%20Core-purple.svg)](#二核心系统架构与设计亮点)
[![Tests](https://img.shields.io/badge/Logic%20Tests-12%2F12%20PASS-brightgreen.svg)](#六全自动化单元测试)
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](#)

> 本工程完全基于原经典 C++ 控制台打飞机游戏核心算法逻辑，按照工业级面向对象设计思想（OOP）与 Unity 现代图形规范，100% 完整重构实现的现代 2D 街机太空飞行射击游戏（STG）。

---

## 目录
1. [项目简介与技术定位](#一项目简介与技术定位)
2. [核心系统架构与设计亮点](#二核心系统架构与设计亮点)
3. [战机演化与火力系统](#三战机演化与火力系统)
4. [敌军集群与灭世级 BOSS](#四敌军集群与灭世级-boss)
5. [操作控制指南](#五操作控制指南)
6. [如何运行与体验游戏](#六如何运行与体验游戏)
7. [全自动化单元测试](#七全自动化单元测试)
8. [项目源码目录结构](#八项目源码目录结构)

---

## 一、项目简介与技术定位

* **游戏类型**：经典纵向卷轴街机飞行射击（Vertical Arcade STG / Shmup）
* **开发引擎**：Unity 6000.x (Universal 2D Pipeline)
* **核心语言**：C# 10.0+ / .NET Standard 2.1
* **架构特色**：**纯 C# 核心玩法逻辑与 Unity 渲染表现层绝对解耦**。核心状态机、碰撞矩阵、弹道微积分、波次演进完全不依赖 Unity GameObject 运行时，可在 Headless 纯控制台环境下瞬间完成全量自动化单元测试。

---

## 二、核心系统架构与设计亮点

### 1. 极致丝滑的流体飞行动力学
* **柔性阻尼速度过渡（Velocity Smoothing）**：摆脱传统按键刚性硬切，引入毫秒级临界阻尼平滑，在保证 STG 严苛微操即时响应（~0.035s 极速收敛）的同时，彻底消除起步与急停的单帧跳步顿挫。
* **空气动力学动态侧倾（Aerodynamic Banking Roll）**：左右推杆机动时，战机自适应沿 Z 轴动态侧倾（$\pm 14^\circ$ 丝滑倾斜回弹），整机飞行姿态灵动鲜活。
* **引擎油门实时联动**：向前推杆加速时推进器尾焰爆发拉长 1.45 倍，怠速或减速时收拢，伴随自适应微光呼吸。

### 2. 全息深空战术网格与密集视差星空
* **赛博战术滚动网格（Tactical Cyber Grid）**：程序化实时生成 $64\times 64$ 无缝循环的霓虹青蓝全息经纬网格，随战机推进平滑滚动，为玩家双眼提供高精度亚像素连续空间运动参考系。
* **四层致密星空与高速流光（Dense Starfield & Speed Streaks）**：扩充至 140 颗密集星群，涵盖深空微尘（Layer 0）、双色恒星（Layer 1）、近景耀星（Layer 2）以及高速纵向穿梭流光线（Layer 3），营造出宏大深邃的太空穿梭纵深感。

### 3. 弹道光刃化与速度运动拉伸（Motion Stretch）
* **彻底消除频闪走马灯伪影**：高速等离子炮、穿透激光与飞弹沿运动方向自适应动态拉伸纵向视觉长度，填补 60Hz/144Hz 采样下的离散空间跨度，将离散跳跃的子弹点转化为呼啸贯穿的高速流光光刃。
* **能量光球呼吸脉冲**：敌方弹幕与螺旋光球具备程序化相位呼吸脉冲动效。

### 4. 纯数学程序化 8-Bit 实时音频合成
* **零外部音频资源依赖**：基于 Unity `OnAudioFilterRead` 底层 PCM 音频流，纯数学波形（方波、三角波、锯齿波、白噪声）实时合成复古 Chiptune 太空战斗 BGM 与激光射击、爆炸、受创、警报等 8 种高保真 8-Bit 音效。

---

## 三、战机演化与火力系统

### 1. 5 阶机体形态进化 (Ship Evolution)
击毁敌机积累战术经验 (EXP) 或拾取 `[U]` 能量核心自动进阶，战机形态与火力全面蜕变：
* **Lv.1 隼式截击机 (Falcon-I)**：HP: 100，护盾: 50，航速: 32
* **Lv.2 毒蛇突击舰 (Viper-II)**：HP: 140，护盾: 80，航速: 34
* **Lv.3 凤凰重炮巡洋舰 (Phoenix-III)**：HP: 190，护盾: 120，航速: 35
* **Lv.4 战列雷霆机 (Thunder-IV)**：HP: 250，护盾: 170，航速: 36
* **Lv.5 超维恒星战列旗舰 (Hyperion-V)**：HP: 320，护盾: 240，航速: 38
* **脱战护盾自充能**：脱战 3 秒后启动能量矩阵，每秒平稳恢复 15 点护盾（高刷新率自适应累加算法）。

### 2. 5 阶武器火力弹幕 (Weapon Salvo)
拾取 `[P]` 补给即刻跃迁武器火力等级：
* **Lv.1 单发高能等离子炮 (Single Plasma)**：前向高速直射等离子重炮 (伤害: 15)
* **Lv.2 双联装平行极速光束 (Twin Blaster)**：对称双通道平行光束 (单发伤害: 16)
* **Lv.3 三向散射重炮 (Triple Spread)**：中央直射 + 左右对称扇形散射 (伤害: 18 / 16)
* **Lv.4 五向广角散射 + 贯穿高能激光**：中央贯穿高能激光（可连贯穿透 3 架敌机，伤害: 30）+ 4 路扇面弹幕
* **Lv.5 全弹超载齐射 (Omega Overkill Salvo)**：中心穿透重型激光 + 4 路散射弹幕 + **2 枚自动索敌微型飞弹**（具备惯性质量平滑角插值偏转，自动追踪最近敌机，伤害: 24）！

### 3. 全屏 EMP 清屏核弹 (按 B / K 键)
* 引发全屏环形扩散震荡冲击波，**瞬间湮灭清空全场所有敌方弹幕**；
* 对全场所有存活敌机造成 **180 点毁灭性高伤**；
* 赋予战机 2 秒无敌保护与全息屏幕闪光震颤。

### 4. 战术补给物资与 16 码向心磁吸
* `[P]`：升级武器火力（Lv.1 ~ Lv.5）
* `[U]`：增加 40 点经验，触发升阶时装甲与护盾即刻回满
* `[S]`：充能修复 50 点能量护盾
* `[B]`：补给 1 枚全屏 EMP 核弹
* `[$]`：获取 1000 点战场荣誉积分
* **强力向心磁吸**：战机飞入物资附近 16 码空域，补给舱自动以 22 格/秒高速向心吸附飞向战机！

---

## 四、敌军集群与灭世级 BOSS

* **Scout 蜂群截击机**：支持 V 字集中突击、正弦蛇形摆动扫荡、向玩家斜切冲锋三种战术机动；
* **Gunship 突击炮艇**：定高巡航压制，高频双联平行瞄准扫射；
* **Bomber 重甲轰炸机**：极高防御装甲与重盾，持续下压推进，释放三向高爆重弹；
* **Elite Cruiser 精英战巡**：正弦漂移悬停，释放旋转双向螺旋能量弹幕；
* **Boss Mothership 灭世星际母舰**（三阶段狂暴演进）：
  * **Phase 1**：双翼重炮密集直射 + 中心能量高散弹；
  * **Phase 2**：五向全角度扇形弹幕 + 狙击交叉火力；
  * **Phase 3 (狂暴状态)**：全屏警报红紫爆闪，释放全向旋转地狱级微积分螺旋弹幕！

---

## 五、操作控制指南

| 操作指令 | 键盘按键映射 | 手柄 / 鼠标支持 |
| :--- | :--- | :--- |
| **战机八向机动** | `W` / `A` / `S` / `D` 或 `↑` `↓` `←` `→` | 手柄左摇杆 / 方向十字键 |
| **持续开火射击** | `Space`（空格） 或 `J` (按住连发) | 鼠标左键持续按住 / 手柄按键 A |
| **全屏 EMP 核弹** | `B` 或 `K` | 鼠标右键点击 / 手柄按键 B |
| **战术暂停 / 继续** | `P` 或 `Esc` | 手柄 Start 键 |
| **音效开关静音** | `M` | - |
| **退出 / 返回标题** | `Q` | - |
| **任意场景一键开战** | `F1` | 屏幕左上角浮动按钮随时呼出 |

---

## 六、如何运行与体验游戏

我们在工程中提供了两种随时随地即刻开战的途径：

### 方式 1：一键创建独立战斗场景（推荐）
在 Unity 编辑器顶部菜单栏点击：  
`Galaxy Strike` -> **`Create & Open Galaxy Strike Scene (创建并打开打飞机场景)`**  
自动完成相机、控制器、HUD 和渲染器的配置，点击 Unity 顶部的 **Play (运行)** 即可全屏沉浸开战！

### 方式 2：在任意现有场景一键就地开战
点击 Unity 顶部的 **Play (运行)**：
- 屏幕左上角将自动悬浮全息按钮：`★ 启动星际突袭 (Galaxy Strike) [F1] ★`；
- 点击该按钮或在键盘上按下 **`F1`** 键，游戏将自动就地注入并启动！

---

## 七、全自动化单元测试

我们在 Unity 编辑器菜单中内置了全自动化逻辑测试套件：  
点击菜单栏：`Galaxy Strike` -> **`Run Logic Verification Tests (执行完整逻辑单元测试)`**

### 12 项测试用例全部覆盖：
1. `TestPlayerInit`：战机初始属性、命数、核弹数与代号校验
2. `TestPlayerEvolution`：5 阶机体进阶、经验翻倍递增公式与生命演进
3. `TestShieldRegen`：脱战 3 秒不回盾、脱战满 3 秒后每秒恢复 15 点护盾机制
4. `TestShieldAbsorption`：护盾优先完全抵挡伤害、打穿后结算装甲扣血
5. `TestWeaponModes`：5 阶弹道生成、散射角度与双发追踪微型飞弹拓扑
6. `TestLaserPiercing`：穿透型高能激光击穿 3 架敌机后消亡的贯通判定
7. `TestHomingMissile`：自动追踪导弹向最近敌机的转向角平滑插值算法
8. `TestEmpBomb`：全屏 EMP 核弹全屏消弹、全场 180 点高伤与无敌保护
9. `TestPowerUpMagnet`：物资失重飘落轨迹与 16 码强向心磁吸判定
10. `TestEnemyBehaviors`：4 种常规敌机机动与母舰 BOSS 三阶段演进
11. `TestWaveManager`：5 大战术集中波次编队拓扑生成与波次状态机流转
12. `TestCollisions`：AABB 轴对齐包围盒相交几何检测

**测试结果**：12 项核心用例全部通过（`12 / 12 PASS`）！

---

## 八、项目源码目录结构

```
d:/Code/fireu/
├── README.md                            # [当前文件] 项目总览与上手指南
├── design.md                            # 系统架构与详细设计规范 (Design Doc)
├── AGENT.md                             # AI Agent 协同指南与架构不变量说明
└── Assets/GalaxyStrike/
    ├── README.md                        # 面向对象四大支柱在 Unity 中的实践说明
    ├── Editor/                          # 编辑器扩展与全自动化单元测试
    │   ├── GalaxyStrikeLogicTests.cs    # 12/12 项全自动化逻辑测试套件
    │   ├── GalaxyStrikeSceneSetup.cs    # 场景一键自动搭建工厂
    │   ├── GalaxyStrikePrefabGenerator.cs# 预制体自动装配
    │   └── GalaxyStrikeArtSetup.cs      # 美术贴图切片与尺寸校验
    ├── Prefabs/                         # 战机、敌机、弹药、道具预制体资产
    ├── Resources/GalaxyStrikeArt/       # 精灵贴图切片图集与 1080P 主界面原画
    ├── Scenes/                          # 主入口场景
    └── Scripts/                         # 游戏核心代码库 (高内聚分层)
        ├── GalaxyStrikeGame.cs          # 总控协调中枢 (主循环/物理调度/输入)
        ├── GalaxyStrikeBootstrapper.cs  # 全局 F1 就地启动器
        ├── Core/                        # 【纯逻辑】基础几何类型、常量、坐标变换
        │   ├── GameConfig.cs            # 视界网格规格、物理步长与世界空间投射
        │   └── Types.cs                 # 几何包围盒 Rect2D 与颜色数学扩展
        ├── Entities/                    # 【纯逻辑】实体多态体系
        │   ├── Entity.cs                # 实体抽象基类 (AABB 碰撞)
        │   ├── Player.cs                # 玩家战机 (5阶进化/柔性阻尼动力学/核弹)
        │   ├── Enemy.cs                 # 敌机抽象基类
        │   ├── ScoutEnemy.cs            # 侦察机 (蜂群冲锋)
        │   ├── GunshipEnemy.cs          # 炮艇机 (定高扫射)
        │   ├── BomberEnemy.cs           # 轰炸机 (重装散弹)
        │   ├── EliteCruiser.cs          # 精英战巡 (螺旋弹幕)
        │   ├── BossEnemy.cs             # 灭世母舰 (三阶段狂暴)
        │   ├── Bullet.cs                # 弹道体系 (穿透激光/智能导引飞弹)
        │   └── PowerUp.cs               # 战术补给 (正弦飘落/16码磁吸)
        ├── Systems/                     # 【纯逻辑】波次与粒子系统
        │   ├── WaveManager.cs           # 5波集中战术编队拓扑编排器
        │   ├── StarfieldManager.cs      # 140颗四层致密视差星尘系统
        │   └── ParticleSystemManager.cs # 爆炸碎片衰减与 EMP 冲击波环
        ├── Presentation/                # 【表现层】Unity 渲染桥接引擎
        │   ├── GalaxyStrikeRenderer.cs  # 全息网格/机身侧倾/光刃拉伸/批渲染
        │   └── GalaxyStrikeVisualSize.cs# 精灵等比适配与世界尺寸归一化
        ├── UI/                          # 【表现层】HUD 仪表盘与全息遥测
        │   └── GalaxyStrikeHUD.cs       # 动态自适应赛博 HUD 侧边栏/血条/战报
        └── Audio/                       # 【音频层】纯数学程序化实时合成
            └── ProceduralSoundManager.cs# PCM 实时合成 8-bit 音效与太空 BGM
```
