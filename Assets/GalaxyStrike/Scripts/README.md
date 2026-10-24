# Scripts 核心源码分层架构 · 导引与 AI 规范

> **所属资源路径**：`Assets/GalaxyStrike/Scripts/`  
> **设计定位**：包含游戏全部 C# 源码。依据“高内聚、低耦合”原则划分五大层级，支持纯逻辑测试与 Unity 图形渲染解耦。

---

## 目录索引与架构分层

```
Scripts/
├── Core/               # 【纯逻辑】基础结构体、数学扩展、虚拟网格常量 (GameConfig, Types)
├── Entities/           # 【纯逻辑】战机、多态敌机、弹道、掉落物资实体体系 (Player, Enemy, Bullet, PowerUp)
├── Systems/            # 【纯逻辑】波次编排器、星空视差粒子、双层环清屏爆炸系统 (Wave, Starfield, Particle)
├── Presentation/       # 【Unity桥接】SpriteRenderer 动态批渲染、包围盒自适应等比缩放、对象池 (Renderer, VisualSize)
├── UI/                 # 【Unity桥接】战术赛博视口边框、实时遥测面板、标题封面与交互按钮 (GalaxyStrikeHUD)
├── Audio/              # 【Unity桥接】数学波形实时程序化合成 8-bit 音效与 BGM (ProceduralSoundManager)
├── GalaxyStrikeGame.cs # 游戏主控总调度器：状态机切换、输入轮询、物理管线驱动、渲染调度
└── GalaxyStrikeBootstrapper.cs # 全局开箱即用引导器：支持任意场景按下 F1 就地呼出游戏
```

---

## 核心交互调用时序 (Frame Lifecycle)

1. **`Update()`**：
   * `GalaxyStrikeGame` 轮询键盘输入（WASD 移动、Space/J 射击、B/鼠标右键释放双层环核弹）；
   * 调用 `UpdatePhysics(dt)` 推进核心网格物理演算：
     * `m_starfield.Update(dt)`
     * `m_waveMgr.Update(dt, ...)`
     * `m_player.Update(dt)` & `m_player.Move(...)`
     * 敌机多态轨迹推进与射击 `e.UpdateWithTarget(...)`
     * 弹幕推进与 AABB 碰撞消除
     * 掉落物资磁吸判定
     * 粒子与双层冲击波环消减 `m_particles.Update(dt)`
2. **`LateUpdate()`**：
   * 调用 `m_renderer.RenderFrame(...)` 将纯逻辑网格状态投影至 Unity 世界坐标并批处理提交渲染；
3. **`OnGUI()`**：
   * 调用 `m_hud.DrawFrame(...)` 绘制 74% 战术座舱边框与 26% 遥测侧边栏。
