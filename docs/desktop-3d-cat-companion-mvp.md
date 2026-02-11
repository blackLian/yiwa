# 桌面 3D 猫宠 Companion（MVP 设计与实现草案）

## 1. MVP 目标

- 在 Windows 桌面常驻透明窗口中渲染 3D 猫咪。
- 提供两种人格：**可爱治愈** 与 **高冷傲娇**。
- 支持基础交互事件：`OnMouseNear`、`OnIconCollision`、`OnIdleTimer`、`OnUserClick`。
- 提供“作弊式动画”能力：多姿势资产切换 + 微位移，降低动画与骨骼系统复杂度。
- 预留 AI 扩展接口，但不纳入 MVP 强制范围。

## 2. 模块架构

```text
Companion
├── Asset 管理
│   ├── PetAssetRegistry
│   ├── PoseSet（每只猫 >= 3 姿势）
│   └── AssetQCMetadata
├── 3D 渲染层（Unity）
│   ├── GaussianRenderAdapter
│   ├── LightingProfile
│   └── PoseSwitcher
├── 交互系统
│   ├── InputEventBridge
│   ├── IconCollisionService
│   └── BehaviorStateMachine
├── 桌面集成层
│   ├── OverlayWindowController
│   ├── ClickThroughController
│   └── TaskbarIntegration
├── 录入与重建 Pipeline
│   ├── CaptureGuide
│   ├── FrameExtractor
│   ├── SfM/3DGS Builder
│   └── QC Validator
└── 数据层
    ├── PetProfileStore
    └── UserPreferenceStore
```

## 3. 人格差异配置（核心）

### 可爱治愈猫

- 鼠标接近时更高概率靠近。
- 被点击时高概率触发打招呼（轻跳、眨眼）。
- 闲逛时对图标区域存在正向吸引。

### 高冷傲娇猫

- 鼠标接近时后退或侧身远离。
- 被快速连点时更倾向“转身离开”反馈。
- 默认停留在用户活动热点外圈，动作频率更低。

## 4. 行为状态机

状态集合：

- `IdleWander`
- `StayNearActiveRegion`
- `Sleep`
- `AvoidMouse`
- `CuriousAtIcon`
- `ReactToClick`

事件触发：

- `OnMouseNear(distance)`
- `OnIconCollision(iconName, iconBounds)`
- `OnIdleTimer(seconds)`
- `OnUserClick(clickRate)`

核心规则：

1. 高优先级：点击反馈 > 鼠标接近回避/靠近 > 图标碰撞 > idle。
2. 每次状态切换触发姿势资产切换（Pose Switch）与短时位移插值。
3. `Sleep` 仅在无交互持续达到阈值后生效。

## 5. 桌面窗口与交互

- 无边框透明窗口（可置顶）。
- 可切换点击穿透模式。
- 只做视觉碰撞，不移动真实桌面图标。
- 支持拖拽锚点（当点击穿透关闭时）。

## 6. 3DGS 资产方案

- 每个角色至少准备 3~4 个姿势 3DGS 资产。
- 可转换为 `PLY/OBJ/FBX/GLTF` 以适配 Unity 管线。
- 通过姿势切换实现“低复杂动画”。

## 7. 录入与建模 Pipeline（建议）

1. 手机环拍 30~60 秒。
2. FFmpeg 抽帧。
3. COLMAP / SfM 估计相机位姿。
4. 训练/生成 3DGS 资产。
5. QC 检测（模糊、曝光、遮挡）。
6. 导出并登记到 Asset Registry。

## 8. AI 扩展预留

- `OnUserTextCommand(text)`
- `OnBehaviorEvent(event)`

后续可挂接本地推理（如 Ollama）或内网 LLM。

## 9. MVP 完成定义（DoD）

- [x] 透明桌面渲染窗口可运行。
- [x] 两只猫均可切换并具备差异化行为。
- [x] 支持图标碰撞反馈气泡（仅视觉层）。
- [x] 支持鼠标接近与点击反馈。
- [x] 至少 3 姿势/角色，支持运行时切换。
- [ ] AI 对话（后续版本）。
