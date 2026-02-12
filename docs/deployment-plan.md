# Desktop 3D Cat Companion（P3→可发布）完整部署方案

> 目标：把当前仓库从“开发验证态”推进到“可部署、可回滚、可观测”的 Windows 交付流程。

## 1. 部署范围与交付物

### 1.1 当前范围（建议）
- 平台：Windows 10/11（x64）
- 阶段：P3（桌面集成）可运行交付 + 运行时可观测
- 运行模式：本地单机，无云依赖

### 1.2 交付物清单
- 可执行程序（或 self-contained 打包产物）
- 默认配置文件（宠物配置、运行时参数）
- 运行手册（`docs/p3-runbook.md`）
- 验收脚本与清单（`tests/p3_smoke_test.ps1`, `tests/p3_acceptance_checklist.md`）
- 部署方案文档（本文）

---

## 2. 环境与依赖准备

### 2.1 构建机要求
- Windows 10/11 x64
- .NET SDK（与项目目标框架匹配）
- Git
- Python 3（用于现有校验脚本）
- PowerShell 7（建议，用于 `pwsh` 执行 smoke）

### 2.2 运行机要求
- Windows 10/11 x64
- Visual C++ Runtime（如后续引入原生依赖）
- 桌面 Explorer 正常运行（P3 图标采集依赖）

---

## 3. 分层部署架构

### 3.1 运行时层
- `DesktopIntegrationRuntime`：调度 tick、事件编排、advisory 产出
- `DesktopRuntimeOperator`：对外输出 notice、进度、状态摘要
- `DesktopIconProvider`：管理 source + cache + mode/error telemetry

### 3.2 系统集成层
- `Win32ExplorerIconSource`：ListView 图标读取（native）
- 回退链路：fallback grid + cached only

### 3.3 可观测层
- `IntegrationDiagnostics`：Info/Warn 日志
- 运行健康快照：`RuntimeHealthSnapshot`
- 状态摘要：`RuntimeStatusSummary`

---

## 4. 构建与打包流程（建议 CI/CD）

### 4.1 本地/CI 构建阶段
1. 拉取代码并锁定 commit。
2. 执行 Python 校验：
   - `python tests/validate_behavior.py`
   - `python tests/validate_p3_progress.py`
   - `python tests/validate_desktop_runtime_progress.py`
   - `python tests/validate_runtime_operator_progress.py`
3. 执行 PowerShell smoke（构建机支持时）：
   - `pwsh ./tests/p3_smoke_test.ps1`
4. 打包发布（示例）：
   - `dotnet publish -c Release -r win-x64 --self-contained true`

### 4.2 产物规范
- 目录建议：
  - `bin/Release/<tfm>/win-x64/publish/`
- 产物版本命名建议：
  - `desktop-cat-companion-v{semver}+{shortSha}`

---

## 5. 部署步骤（生产机）

1. 创建目录：`C:\Program Files\DesktopCatCompanion\`
2. 拷贝 publish 产物到目标目录。
3. 首次运行前写入默认配置（宠物 profile、运行参数）。
4. 启动应用并观察：
   - 窗口初始化
   - 点击穿透切换
   - 图标交互提示
5. 执行验收清单（`tests/p3_acceptance_checklist.md` 对照）。

---

## 6. 配置与参数建议

### 6.1 运行参数（建议暴露）
- 轮询频率（Hz）
- 是否启用 native icon source
- fallback/grid 开关
- 日志级别与日志文件路径
- 通知去重开关

### 6.2 默认策略
- native-first，失败自动降级 fallback
- 连续失败进入 recovery（降频）
- notice 去重避免刷屏

---

## 7. 观测与运维（上线后）

### 7.1 核心运行指标
- `Progress.CompletionPercent`
- `ProgressDeltaPercent`
- `StatusLevel`
- `IconSourceMode`
- advisory counters：cached/partial/fallback/recovery

### 7.2 日志排障重点
- `DesktopRuntime`
- `DesktopRuntimeNotice`
- `DesktopIconProvider`

### 7.3 日常巡检建议
- 每次发版后，至少运行 30 分钟稳定性巡检
- 核对 recovery/advisory 计数是否异常增长

---

## 8. 灰度、回滚与容灾

### 8.1 灰度发布
- 先在内部机器灰度（5~10 台）
- 观察 1~2 天日志与 CPU 占用
- 再扩大范围

### 8.2 回滚策略
- 保留最近两个版本目录
- 回滚时切换启动路径到上一个稳定版本
- 配置文件按版本备份，避免 schema 不兼容

### 8.3 异常兜底
- native 读取失败 -> fallback grid
- fallback 异常 -> cached only
- runtime 连续失败 -> recovery mode

---

## 9. 安全与权限建议

- 不请求管理员权限（非必要）
- 避免写入系统敏感目录
- 日志中避免输出敏感用户信息
- 统一异常捕获并降级，不崩溃退出

---

## 10. 发布节奏建议

### 10.1 版本策略
- `v0.3.x`：P3 稳定化阶段
- `v0.4.x`：P4 渲染接入
- `v0.5.x`：P5 AI 接口接入

### 10.2 每次发布最小门槛
- 四个 Python 校验通过
- smoke 脚本通过（有 pwsh 环境时）
- runbook 手工流程可复现
- 回滚演练至少执行 1 次

---

## 11. GitHub 提交流程（你要的“帮你提交到 GitHub”）

当前自动化环境已完成：
- 本地提交（git commit）
- 生成 PR 元数据（make_pr）

你在 GitHub 上需要做：
1. 打开当前分支对应 PR。
2. 审阅变更与测试结果。
3. Merge（Squash 或 Rebase 按团队规范）。
4. 在 release 页面打 tag 并上传发布说明。

如果你希望“自动推送到远程仓库并创建真实 GitHub PR”，需要当前环境配置好远程仓库权限（token/SSH）。

---

## 12. 立即可执行的上线 checklist

- [ ] 代码已合入主干
- [ ] 四项 Python 校验通过
- [ ] smoke 通过（或记录环境限制）
- [ ] 发布包已产出并归档
- [ ] 目标机部署完成并首启成功
- [ ] runbook 手工验收通过
- [ ] 回滚路径已验证
