# 《星际突袭：银河战线》(Galaxy Strike) - C# / Unity 重构版

> 本工程完全基于原 `X` 文件夹下的 C++ 控制台打飞机游戏核心逻辑，按照标准的现代面向对象设计思想（OOP）与 Unity 引擎架构规范，100% 完整重构实现的飞行射击游戏（STG）。

---

## 目录
1. [重构核心对照与架构演进](#一重构核心对照与架构演进)
2. [面向对象四大支柱在 Unity / C# 中的实践](#二面向对象四大支柱在-unity--c-中的实践)
3. [核心玩法与数值平衡实现](#三核心玩法与数值平衡实现)
4. [操作控制说明](#四操作控制说明)
5. [如何运行与体验游戏](#五如何运行与体验游戏)
6. [全自动化单元测试](#六全自动化单元测试)

---

## 一、重构核心对照与架构演进

| 模块类别 | 原 C++ 实现 (`X/` 目录) | C# / Unity 重构实现 (`Assets/GalaxyStrike/`) |
| :--- | :--- | :--- |
| **基础类型与数学** | `Types.h` (`Vector2`, `Rect`, 运算符重载) | `Core/Types.cs` (`Rect2D`, `Vector2Extensions`, `GameRandom`, `GameColors`) |
| **全局平衡参数** | `GameConfig.h` (`constexpr` 视界/物理步长) | `Core/GameConfig.cs` (网格坐标尺寸与 Unity 世界空间投射矩阵) |
| **实体抽象基类** | `Entity.h / Entity.cpp` (纯虚函数 `update`, `render`) | `Entities/Entity.cs` (`abstract class Entity`, 封装 AABB 相交检测) |
| **玩家主力战机** | `Player.h / Player.cpp` (5阶形态演进/5阶弹道/核弹) | `Entities/Player.cs` (5阶机体演进、5阶火力、脱战自充能、EMP全屏核弹) |
| **弹道与弹药体系** | `Bullet.h / Bullet.cpp` (穿透/智能角插值追踪) | `Entities/Bullet.cs` (普通、双联、散射、穿透激光、自动追踪飞弹、螺旋弹) |
| **战术补给物资** | `PowerUp.h / PowerUp.cpp` (正弦漂浮/16码磁吸) | `Entities/PowerUp.cs` (P火力、U经验、S护盾、B核弹、$积分，16码强向心磁吸) |
| **敌机集群体系** | `Enemy.h / Enemy.cpp` (模板方法/多态子类派生) | `Entities/Enemy.cs` 及派生类：<br>- `ScoutEnemy.cs` (蜂群冲锋，3种编队轨迹)<br>- `GunshipEnemy.cs` (定高巡航扫射，精准双联瞄准)<br>- `BomberEnemy.cs` (重甲厚血推进，三向落弹)<br>- `EliteCruiser.cs` (正弦悬停漂移，双向螺旋弹幕)<br>- `BossEnemy.cs` (灭世母舰，三阶段狂暴演进) |
| **关卡波次编排器** | `WaveManager.cpp` (集中集群编队生成器) | `Systems/WaveManager.cs` (5套集中战术编队编排，4阶段波次状态机) |
| **粒子与冲击波** | `ParticleSystem.h / ParticleSystem.cpp` (爆炸/震荡环) | `Systems/ParticleSystemManager.cs` (火花碎片衰减、全屏 EMP 震荡波环) |
| **视差太空背景** | `Starfield.h / Starfield.cpp` (3层远近视差/曲率加速) | `Systems/StarfieldManager.cs` (3层星尘视差滚动，Boss 战超光速跃迁) |
| **战术仪表盘** | `Hud.h / Hud.cpp` (全息侧边栏/巨幅 Boss 血条) | `UI/GalaxyStrikeHUD.cs` (完整战术遥测仪表盘、血条、护盾条、经验条、Boss血条) |
| **声音与音乐** | `Sound.h / Sound.cpp` (多线程 Beep 蜂鸣/异步 Chiptune) | `Audio/ProceduralSoundManager.cs` (纯数学波形程序化实时合成 8-bit 音效与太空 BGM，零音频文件依赖) |
| **主控协调者** | `Game.h / Game.cpp` (60 FPS 固定主循环) | `GalaxyStrikeGame.cs` (中枢调度器，处理输入、物理更新、碰撞矩阵、渲染分发) |
| **渲染桥接引擎** | `Console.h / Console.cpp` (双缓冲无闪烁字符绘制) | `Presentation/GalaxyStrikeRenderer.cs` (动态矢量生成、对象池管理、平滑抗锯齿霓虹渲染) |
| **即时启动器** | 终端命令行入口 `main.cpp` | `GalaxyStrikeBootstrapper.cs` (任意场景一键 F1 / 浮动按钮就地呼出开战) |

---

## 二、面向对象四大支柱在 Unity / C# 中的实践

1. **封装 (Encapsulation)**：
   - 严格遵循 `private` / `protected` 访问修饰符，对外暴露安全只读属性与动作接口；
   - 比如战机受伤逻辑完全封装于 `Player.TakeDamage()`，护盾优先吸收伤害，脱战计时器自动重置，外部无需也不能直接篡改 `hp` 或 `shield`。
2. **继承 (Inheritance)**：
   - `Entity` 是所有实体的祖先，封装了位置、速度、包围盒与碰撞相交算法；
   - `Enemy` 继承 `Entity` 并成为二级抽象基类；`ScoutEnemy`、`GunshipEnemy`、`BomberEnemy`、`EliteCruiser`、`BossEnemy` 继承 `Enemy`，代码复用率高达 90% 以上。
3. **多态 (Polymorphism)**：
   - 主循环容器使用 `List<Enemy>` 基类引用；
   - 在 `UpdatePhysics` 中，主控制器无需知道具体是哪一类敌机，只需调用 `e.UpdateWithTarget(...)` 与 `e.Shoot(...)`，C# 运行时通过虚方法表 (vtable) 自动动态分发执行对应子类特色行为，彻底消灭臃肿脆弱的 `switch-case`，完美践行**开闭原则 (OCP)**！
4. **组合优于继承 (Favor Composition over Inheritance)**：
   - `GalaxyStrikeGame` 作为总控，并没有继承自任何实体或管理器，而是通过组合模式持有了 `Player`、`WaveManager`、`StarfieldManager`、`ParticleSystemManager`、`ProceduralSoundManager`、`GalaxyStrikeHUD` 和 `GalaxyStrikeRenderer`，模块间职责单一、高内聚低耦合。

---

## 三、核心玩法与数值平衡实现

### 1. 战机进阶演进系统 (5阶机体形态升级)
击毁敌机积累战术经验 (EXP) 或拾取 `[U]` 核心道具自动升阶：
- **Lv.1 隼式截击机 (Falcon-I)**：HP: 100，护盾: 50，航速: 32
- **Lv.2 毒蛇突击舰 (Viper-II)**：HP: 140，护盾: 80，航速: 34
- **Lv.3 凤凰重炮巡洋舰 (Phoenix-III)**：HP: 190，护盾: 120，航速: 35
- **Lv.4 战列雷霆机 (Thunder-IV)**：HP: 250，护盾: 170，航速: 36
- **Lv.5 超维恒星战列旗舰 (Hyperion-V)**：HP: 320，护盾: 240，航速: 38
- **护盾脱战充能**：受到伤害后脱战 3 秒，每秒自动恢复 15 点能量护盾！

### 2. 多阶武器弹道系统 (5阶火力模式)
拾取 `[P]` 道具即刻提升武器火力：
- **Lv.1 单发高能等离子炮 (Single Plasma)**：前向高速直射单发重炮 (伤害: 15)
- **Lv.2 双联装平行极速光束 (Twin Blaster)**：对称双通道光束，火力覆盖翻倍 (单发伤害: 16)
- **Lv.3 三向散射重炮 (Triple Spread)**：中央直射 + 左右对称扇形散射 (伤害: 18 / 16)
- **Lv.4 五向广角散射 + 贯穿高能激光**：中央贯穿型激光（可穿透3架敌机，伤害: 30）搭配4路扇面弹幕
- **Lv.5 全弹超载齐射 (Omega Overkill Salvo)**：中心穿透重型光束 + 4路散射弹幕 + **2枚自动索敌微型飞弹**（具备惯性质量平滑角插值转向，自动追踪最近敌机，伤害: 24）！

### 3. 全屏清屏 EMP 核弹 (按 B / K 键)
- 消耗 1 枚战术核弹；
- 引发全屏环形扩散震荡光波，**瞬间清空湮灭全场所有敌方子弹**；
- 对全场所有存活敌机造成 **180 点毁灭性高伤**；
- 赋予战机 2 秒无敌保护状态。

### 4. 战术补给物资与 16 码近距强磁吸
- `[P]`：升级子弹火力等级 (Lv.1 ~ Lv.5)
- `[U]`：增加 40 点机体经验，触发升级时生命与护盾回满
- `[S]`：充能修复 50 点能量护盾
- `[B]`：补充 1 枚全屏 EMP 清屏核弹
- `[$]`：获取 1000 点战场荣誉积分
- **近距磁吸机制**：玩家飞过掉落物附近 16 码范围内，物资会自动以 22 速度加速向战机飞来！

### 5. 集中敌人编队与星际母舰 BOSS 决战
- **Wave 1**：蜂群冲锋 V 字突击编队 (7架快速切入) + 6架蛇形扫荡梯队 + 2架突击巡航舰
- **Wave 2**：双翼钳形合围编队 (左右两翼各5架斜切对穿) + 中央双联突击舰 + 重装轰炸机
- **Wave 3**：重装装甲护航编队 (2架重装轰炸机并列推进 + 4架突击机护航)
- **Wave 4**：精英战巡前锋舰队 (精英巡洋舰释放旋转双向螺旋弹幕 + 轰炸机压制)
- **Wave 5 (及每第 5 波)**：**灭世级星际母舰 BOSS 决战**：
  - **Phase 1**：双翼重炮直射 + 中心能量散弹
  - **Phase 2**：五向广角散射 + 瞄准交叉火力
  - **Phase 3 (狂暴状态)**：红紫警报交替闪烁，释放全向旋转地狱螺旋弹幕！

---

## 四、操作控制说明

| 操作按键 | 功能说明 |
| :--- | :--- |
| **`W` / `A` / `S` / `D` 或 方向键 `↑` `↓` `←` `→`** | 战机平滑八向矢量飞行 |
| **`Space`（空格） 或 `J`** | 高速主炮开火（按住连发，自动锁定索敌） |
| **`B` 或 `K`** | 释放全屏清屏 EMP 核弹 |
| **`P` 或 `Esc`** | 战术暂停 / 继续任务 |
| **`M`** | 开关 8-Bit 复古音效与 BGM |
| **`Q`** | 退出任务 / 返回标题画面 |
| **`F1`** | 在任意场景内一键就地呼出开战 |

---

## 五、如何运行与体验游戏

我们在工程中提供了三种随时可玩的入口：

### 方式 1：一键创建独立场景（推荐）
在 Unity 编辑器顶部菜单栏点击：
`Galaxy Strike` -> **`Create & Open Galaxy Strike Scene (创建并打开打飞机场景)`**
自动完成相机、游戏控制器、HUD 的配置，点击 Unity 顶部的 **Play (运行)** 即可全屏开战！

### 方式 2：在任意现有场景即时开战
点击 Unity 顶部的 **Play (运行)**：
- 屏幕左上角将出现浮动按钮：`★ 启动星际突袭 (Galaxy Strike) [F1] ★`；
- 点击该按钮或在键盘上按下 **`F1`** 键，游戏立即就地启动！

---

## 六、全自动化单元测试

我们在 Unity 编辑器菜单中内置了全自动化测试套件：
点击菜单栏：
`Galaxy Strike` -> **`Run Logic Verification Tests (执行完整逻辑单元测试)`**

### 测试用例覆盖：
1. `TestPlayerInit`：战机初始属性、命数、核弹与形态代号校验
2. `TestPlayerEvolution`：5阶机体进阶、经验翻倍递增公式与生命上限演进
3. `TestShieldRegen`：脱战 3 秒不回盾、3 秒后每秒恢复 15 点护盾机制
4. `TestShieldAbsorption`：护盾优先完全抵挡伤害、打穿后结算装甲扣血
5. `TestWeaponModes`：5阶弹道生成、散射角度与双发追踪微型飞弹
6. `TestLaserPiercing`：穿透型激光击穿 3 架敌机后消亡的贯通判定
7. `TestHomingMissile`：自动追踪导弹向最近敌机的转向角平滑插值算法
8. `TestEmpBomb`：全屏 EMP 核弹全屏消弹、全场 180 点高伤与无敌保护
9. `TestPowerUpMagnet`：物资失重飘落轨迹与 16 码强向心磁吸判定
10. `TestEnemyBehaviors`：Scout/Gunship/Bomber/Elite 移动机动与母舰 BOSS 三阶段演进
11. `TestWaveManager`：5大集中波次编队拓扑生成与波次状态机流转
12. `TestCollisions`：AABB 轴对齐包围盒相交几何检测

**测试结果**：12 项核心用例全部绿灯通过 (`12 / 12 PASS`)！
