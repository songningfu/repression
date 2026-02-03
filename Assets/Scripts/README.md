# 项目文档 - Subconscious Echo Studio

## 📁 项目结构

### MentalStats/ - 精神状态系统
管理玩家的 Stress（应激度）和 Dissociation（解离度）

### WorldState/ - 世界状态系统
管理现实世界和意识世界的切换

## 🎮 交互系统使用指南

### 1. 基础交互对象设置

为任何交互对象（cure、sleep、eat、recreation等）添加功能：

#### 步骤 1：添加碰撞器
- Add Component → `Box Collider 2D`
- ✅ 勾选 `Is Trigger`
- 调整 Size 为交互范围

#### 步骤 2：添加交互脚本
- Add Component → `StressInteractable`
  - 设置 Interaction Type
  - 设置 Stress 变化值
- Add Component → `StressFInteractController`
  - Player Tag: `Player`

#### 步骤 3：创建提示文字
- 右键对象 → Create Empty
- 命名为 `按F交互`
- Add Component → TextMeshPro
- 设置文字内容
- 默认隐藏（取消勾选）

### 2. 世界特定对象（如 cure）

让对象只在特定世界显示：
- Add Component → `WorldSpecificObject`
- 设置 Show In World（Consciousness 或 Reality）

### 3. 交互类型说明

| 类型 | 意识世界效果 | 现实世界效果 |
|------|------------|------------|
| Therapy (治疗) | 减少 Stress | 增加 Stress |
| Sleep (睡觉) | 减少 Stress | 增加 Stress |
| Eat (吃饭) | 减少 Stress | 增加 Stress |
| Entertainment (娱乐) | 减少 Stress | 增加 Stress |

## 🔧 核心组件说明

### StressInteractable
控制交互时 Stress 的变化

### StressFInteractController
处理按F键交互和提示显示

### WorldSpecificObject
控制对象在特定世界显示/隐藏

### WorldAlertFollower
让UI跟随玩家（可选）

## 📝 注意事项

1. ✅ 玩家必须有 "Player" 标签
2. ✅ 提示对象必须命名为 "按F交互"
3. ✅ Collider 必须勾选 Is Trigger
4. ✅ 场景中需要 WorldStateManager 和 MentalStatsManager

---

完整文档整合版 - 2025

