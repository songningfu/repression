# 闭眼/睁眼转场效果设置指南

## 🎬 效果说明

在世界切换时（睡觉/床交互），会有自然的闭眼/睁眼效果：
1. 闭眼（屏幕渐黑）
2. 黑屏停留
3. 切换世界
4. 睁眼（屏幕渐亮）

## 🔧 设置步骤

### 步骤 1：创建转场管理器

1. **在 Hierarchy 中创建空对象**
   - 右键 → Create Empty
   - 命名为 `TransitionManager`

2. **添加脚本**
   - 选中 TransitionManager
   - Add Component → 搜索 `EyeBlinkTransition`

3. **设置参数**（可选，使用默认值即可）
   - Close Eye Duration: `0.5` 秒（闭眼时间）
   - Open Eye Duration: `0.5` 秒（睁眼时间）
   - Black Screen Duration: `0.3` 秒（黑屏停留时间）

### 步骤 2：确保在两个场景都有

- 在 **Reality.unity** 场景中创建 TransitionManager
- 在 **Consciousness.unity** 场景中也创建 TransitionManager

或者：
- 将 TransitionManager 保存为预制体
- 在两个场景中都放置这个预制体

## ✅ 完成！

现在当您：
- 按F与床交互
- 世界切换时

会自动播放闭眼/睁眼效果！

## ⚙️ 自定义设置

### 调整转场速度

在 Inspector 中修改：
- **快速转场**：Close/Open Duration = 0.3
- **慢速转场**：Close/Open Duration = 0.8
- **无停顿**：Black Screen Duration = 0

### 调整动画曲线

在 Inspector 中可以看到：
- **Close Eye Curve** - 闭眼曲线（控制渐黑的速度变化）
- **Open Eye Curve** - 睁眼曲线（控制渐亮的速度变化）

点击曲线可以自定义动画效果！

## 🎯 效果预览

```
正常视野 → 渐黑(0.5s) → 黑屏(0.3s) → [切换世界] → 渐亮(0.5s) → 正常视野
```

总时长：约 1.3 秒

---

完成设置后，世界切换会非常自然流畅！🌙✨

