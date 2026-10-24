# Systems 游戏逻辑系统层 · 架构说明与 AI 规范

> **所属命名空间**：`GalaxyStrike.Systems`  
> **设计定位**：驱动波次状态流转、太空视差星尘运动，以及爆炸与双层环核弹清屏特效的纯逻辑子系统群。

---

## 核心系统清单与职责分工

| 系统模块 | 核心类 | 架构职责与关键特性 |
| :--- | :--- | :--- |
| **关卡波次编排系统** | [`WaveManager.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Systems/WaveManager.cs) | 采用四阶段状态机（`Spawning` -> `Active` -> `Cleared` -> `GameOver`），编排 5 种集中式战术出兵编队，第 5 波必定激活星际母舰 BOSS 战。 |
| **视差深空背景系统** | [`StarfieldManager.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Systems/StarfieldManager.cs) | 模拟 80 颗不同光谱色系恒星的 3 层远近视差流动；当 Boss 战开启时触发超光速跃迁 (Warp Mode)，星体拉伸为极速穿梭光束。 |
| **粒子与冲击波系统** | [`ParticleSystemManager.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Systems/ParticleSystemManager.cs) | 统一管理爆炸火球、稀疏星芒、飞旋碎屑与全屏双层冲击波环；支持炸弹清屏辐射微型电弧分叉 (`ParticleKind.BranchFork`)。 |

---

## 爆炸与清屏特效设计规范 (VFX Architecture)

### 1. 爆炸粒子微型化与稀疏化原则
为彻底杜绝大面积浓黑烟尘遮挡玩家视野与弹幕轨迹，粒子系统严格遵循以下参数规范：
* **普通爆闪 (`AddExplosion`)**：
  * 核心爆闪火球 (`ParticleKind.Fireball`)：仅 $1$ 枚，基准尺寸 $0.58f$，寿命仅 $0.18 \sim 0.24$ 秒，极速消隐；
  * 放射火星 (`ParticleKind.Spark`)：$5 \sim 12$ 颗，初速 $16 \sim 40$，放射状均匀扩散；
  * 金属碎屑 (`ParticleKind.Debris`)：仅 $1 \sim 2$ 粒，不造成视线干扰。
* **巨型爆破 (`AddMegaExplosion`)**：
  * 用于 BOSS 受创或大型舰船沉没，爆闪缩放 $1.5 \sim 2.0$ 倍，保持清脆干净，严禁多个巨型火球叠加。

### 2. 玩家全屏核弹机制 (`AddBombDetonation`)
* **双层清屏光环**：
  * 外层先导环：最大半径 $64$，扩散初速 $115$，鲜亮炽红；
  * 内层伴随环：最大半径 $50$，扩散初速 $92$，金白高亮；
  * 扩散全过程 $0.35 \sim 0.45$ 秒内完成，并按幂次曲线瞬间隐入虚空。
* **辐射微型能量分叉**：
  * 沿圆周向外辐射 $14$ 组微型能量枝桠 (`BranchFork`)，主枝初速 $50 \sim 70$，附带 $1 \sim 2$ 组侧向小分叉（偏转 $\pm 28^\circ$），伴随双层环向外疾驰并急速衰减，呈现雷霆电弧裂解质感。

---

## AI Agent 开发规范与注意事项

1. **测试同步保障**：
   * `WaveManager.SpawnFormation` 的出兵索引与数量契约受 `GalaxyStrikeLogicTests.cs` 的严密断言保护，增删编队时必须同步更新测试用例。
2. **生命周期回收**：
   * 粒子与冲击波在 `Update(dt)` 中一旦 `life <= 0` 或 `currentRadius >= maxRadius` 必须立即移除，防止列表无节制扩容。
