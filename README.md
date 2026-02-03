## SeeAPsychologist（Unity）

本仓库为 Unity 项目 **SeeAPsychologist** 的源码仓库。

### 运行环境

- **Unity**：`2022.3.50f1c1`（建议使用 Unity Hub 安装同版本）

### 功能实现进度（A1/A2/A3）

- **A1 MentalStats（应激/解离）**：已实现（MVP）
  - 单一数据源：`MentalStatsManager`
  - 唯一修改入口：`ModifyStress(...)`
  - Stress 变化后触发 Dissociation 重算，并通过 `OnStatsChanged` 广播
  - 自动变化：Stress=0/100 时按配置 tick（见开发日志）
- **A2 Dialogue（对话）**：**暂未实现**
  - 目标：支持 CSV/Excel 导入、阈值过滤选项、打字机/跳过/历史等
- **A3 WorldState（世界切换）**：已实现（MVP）
  - 对外入口：`TrySwitchWorld(InteractionType, out WorldType targetWorld, string context=null)`
  - 支持床/药物/剧情事件三类触发
  - 解离度变化订阅驱动灰度画面效果（不反向写核心数值）

### 快速开始（打开场景）

1. 用 Unity Hub 打开本项目目录（包含 `Assets/`、`Packages/`、`ProjectSettings/` 的这一层）。
2. 打开场景：
   - `Assets/Scenes/Reality.unity`
   - `Assets/Scenes/Consciousness.unity`
3. 运行 Play，根据开发日志的“验收方式”验证 A1/A3 行为。

### 关键目录

- `Assets/Scripts/`：项目脚本（含 A1/A3 相关代码）
- `Assets/Scenes/`：主场景（Reality/Consciousness）
- `Assets/Resources/`：配置资源（如世界切换配置等）
- `Plans/开发日志/`：阶段性实现说明与验收口径（推荐从这里开始读）

### 开发日志索引（节选）

- `Plans/开发日志/2026-02-01_A1_MVP完成.md`
- `Plans/开发日志/2026-02-01_A3_世界切换与灰度滤镜MVP.md`
- `Plans/开发日志/2026-02-01_A3_床交互按F提示与触发MVP.md`
- `Plans/开发日志/2026-02-01_A3_灰度规则调整.md`

