# GalaxyStrikeArt 美术资产与切片规范 · 架构说明与 AI 规范

> **所属资源路径**：`Assets/GalaxyStrike/Resources/GalaxyStrikeArt/`  
> **设计定位**：存放通过算法与矢量脚本程序化生成的卡通科幻风格 Sprite 贴图、高分辨率开始界面底图及九宫切片图集。

---

## 目录结构与资源分类

```
Assets/GalaxyStrike/Resources/GalaxyStrikeArt/
├── README.md                      # [当前文件] 美术资产规范与色彩定义
├── CartoonEntities.png            # 战机、敌机集群与掉落道具原始总图集
├── CartoonEffects.png             # 尾焰、护盾、恒星、炽热冲击波与爆闪图集
└── Sprites/                       # 独立切片 Sprite 资源 (可直接 Resources.Load)
    ├── TitleBackground.png        # 1920x1080 卡通科幻开始界面高清背景
    ├── PlayerShip.png             # Lv.1 战机基础切片
    ├── PlayerShip_Lv2~5.png       # Lv.2 ~ Lv.5 阶梯进阶战机切片
    ├── Enemy_Scout.png            # 侦察蜂群
    ├── Enemy_Gunship.png          # 定高炮艇
    ├── Enemy_Bomber.png           # 重装轰炸机
    ├── Enemy_EliteCruiser.png     # 精英巡洋舰
    ├── Enemy_BossMothership.png   # 灭世母舰
    ├── Bullet_PlayerPlasma.png    # 玩家等离子弹
    ├── Bullet_PlayerLaser.png     # 玩家激光束
    ├── Bullet_PlayerMissile.png   # 玩家自动追踪飞弹
    ├── Bullet_EnemyOrb.png        # 敌方能量法球
    ├── Bullet_EnemyLaser.png      # 敌方激光
    ├── PowerUp_Firepower.png      # [P] 火力增强
    ├── PowerUp_ShipExp.png        # [U] 经验核心
    ├── PowerUp_Shield.png         # [S] 护盾充能
    ├── PowerUp_Bomb.png           # [B] 全屏核弹
    ├── PowerUp_Score.png          # [$] 战况积分
    ├── FX_Thruster.png            # 离子喷射尾焰
    ├── FX_Shield.png              # 能量防护罩光晕
    ├── FX_Star.png                # 恒星闪烁星尘
    ├── FX_EMP.png                 # 鲜亮炽热双层冲击波环
    ├── FX_Explosion.png           # 核心爆破火球
    └── FX_Debris.png              # 飞旋合金残骸
```

---

## 调色板与视觉设计语言 (Color Palette)

本项目严格采用鲜艳的高对比度赛博街机卡通调色板：

| 元素类别 | 核心 HEX 色号 | 视觉质感 |
| :--- | :--- | :--- |
| **深空底色** | `#030612` ~ `#080d24` | 幽深静谧深空紫黑与星云渐变 |
| **我方与护盾** | `#00F0FF` / `#38bdf8` | 电离青蓝，科技感与高速感 |
| **进阶与升级** | `#FFD000` / `#FFB703` | 金白辉光，醒目且充满激励感 |
| **敌机与弹幕** | `#FF2A55` / `#F43F5E` | 鲜艳警示赤红，弹幕极速辨识 |
| **核弹与清屏** | `#FF4500` / `#FFD700` | 炽热熔岩橙红过渡至核心金白 |

---

## AI Agent 维护与资产追加规范

1. **命名规范**：
   * 切片文件命名必须与代码中 `Resources.Load<Sprite>("GalaxyStrikeArt/Sprites/<Name>")` 保持精确大小写一致。
2. **导入设置 (TextureImporter)**：
   * `Texture Type`: `Sprite (2D and UI)`
   * `Sprite Mode`: `Single`
   * `Alpha Source`: `Input Texture Alpha`
   * `Filter Mode`: `Bilinear` (或 `Point` 以获得锐利像素风格)
   * `Compression`: `None` 或 `High Quality`，保证边缘半透明羽化不产生暗边。
3. **程序化脚本重新生成**：
   * 若需批量刷新切片，可在 PowerShell 中运行 GDI+ 生成脚本或在 Unity 编辑器中触发 `GalaxyStrike/Slice and Setup Art`。
