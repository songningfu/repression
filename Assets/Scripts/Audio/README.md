# 音频系统使用指南

本游戏的音频架构。一处定义，全局可用。

---

## 📐 架构总览

```
┌─────────────────────────────────────────────────────────────┐
│                    AudioManager（单例）                       │
│  • Music / Ambient（同时各 1 个，自动交叉淡入淡出）           │
│  • SFX / UI / Voice（声源池，自动回收）                       │
│  • 5 类独立音量 + Master，PlayerPrefs 持久化                  │
└─────────────────────────────────────────────────────────────┘
           ▲                              ▲
           │ 播放                         │ 配置
           │                              │
   ┌───────┴────────┐            ┌────────┴────────┐
   │   游戏代码      │            │   AudioCue       │
   │ (调用 API)      │            │  (ScriptableObject)│
   └────────────────┘            │  • clips 数组    │
                                 │  • 音量/音调范围 │
                                 │  • loop/淡入淡出 │
                                 │  • 类别          │
                                 └─────────────────┘
```

**核心思想**：每段音效是一个 **AudioCue** 资源（不是 AudioClip）。调用方传 cue，AudioManager 负责播放/音量/池化。

---

## 🚀 三步接入新音效

### 1. 创建 AudioCue 资源

Project 右键 → **Create → SeeAPsychologist → Audio → AudioCue**

命名建议：
- `Cue_BGM_Reality`（现实世界 BGM）
- `Cue_BGM_Consciousness`（意识世界 BGM）
- `Cue_SFX_Footstep_Wood`（木地板脚步）
- `Cue_SFX_Footstep_Tile`（瓷砖脚步）
- `Cue_SFX_DoorOpen`（开门）
- `Cue_UI_Click`（按钮点击）

### 2. 配置 Cue Inspector

| 字段 | 怎么填 |
|---|---|
| **Clips** | 拖入 1~N 个音频片段，多个会随机播 |
| **Category** | Music / Ambient / SFX / UI / Voice |
| **Base Volume** | 0.7~1.0 一般够用 |
| **Volume Random Range** | (0.95, 1.05) 默认就好 |
| **Pitch Range** | (0.95, 1.05) 默认就好；变化更大用 (0.9, 1.1) |
| **Loop** | Music/Ambient 勾，SFX 不勾 |
| **Spatial Blend** | 2D 全屏 = 0；位置感强 = 0.3~0.7 |
| **Min Interval Seconds** | 同一 cue 两次播放的最小间隔（防刷屏） |
| **Fade In / Out Seconds** | 仅 Music/Ambient 用 |

### 3. 代码里调用

```csharp
using SeeAPsychologist.Audio;

// 播 BGM（自动淡入淡出，覆盖上一首）
AudioManager.Instance.PlayMusic(myMusicCue);

// 停 BGM（淡出）
AudioManager.Instance.StopMusic(fadeOut: 1.5f);

// 播 SFX（2D 全屏）
AudioManager.Instance.PlaySfx(doorOpenCue);

// 播 SFX（带位置，3D 空间音）
AudioManager.Instance.PlaySfx(explosionCue, transform.position);

// 播跟随某 Transform 的 SFX（车辆/移动 NPC）
AudioManager.Instance.PlaySfxFollow(carEngineCue, carTransform);

// 环境氛围音（雨声、风声）
AudioManager.Instance.PlayAmbient(rainCue);

// 调音量（自动持久化到 PlayerPrefs）
AudioManager.Instance.SetVolume(AudioCategory.Music, 0.5f);
AudioManager.Instance.SetMasterVolume(0.8f);

// 停一切（场景切换/暂停菜单用）
AudioManager.Instance.StopAll(fadeOut: 0.5f);
```

---

## 🚶 脚步声专用：地面区域切换

### 玩家身上

`player` 对象上挂 **PlayerFootsteps**：
- **Default Cue**: 拖入 `Cue_SFX_Footstep_Wood`（默认木地板音）
- **Stride Distance**: `1.2`（每走多远响一次，建议 = moveSpeed × 0.5）
- **Min Speed To Play**: `0.1`

### 场景里加"特殊地面区域"

比如卧室里有一块地毯：

1. 场景里建空 GameObject → 命名 `Surface_Carpet`
2. 加 **BoxCollider2D**，**勾选 Is Trigger ✅**，调整大小覆盖地毯范围
3. 加 **FootstepSurface** 组件
4. **Surface Cue** 拖入 `Cue_SFX_Footstep_Carpet`
5. **Stack Priority**: 0（多区域重叠时用得到）

效果：玩家踩到地毯 = 闷的脚步声；离开地毯 = 自动恢复默认木地板音。

### 多区域叠加规则

地毯 (priority=1) 套在木地板 (默认) 上：踩在地毯上 → 用地毯音。

如果有一块湿漉漉的地毯（priority=2）盖在普通地毯（priority=1）上：用 priority 高的湿地毯音。

---

## 📋 标准音效清单（你这游戏建议建的 Cue）

| 名字 | 类别 | 用途 | Loop |
|---|---|---|---|
| Cue_BGM_Reality | Music | 现实世界 BGM | ✅ |
| Cue_BGM_Consciousness | Music | 意识世界 BGM | ✅ |
| Cue_BGM_Menu | Music | 主菜单 BGM | ✅ |
| Cue_AMB_Indoor | Ambient | 室内环境音（空气流动） | ✅ |
| Cue_AMB_Street | Ambient | 街道环境音（车流人声） | ✅ |
| Cue_SFX_Footstep_Wood | SFX | 木地板脚步 | ❌ |
| Cue_SFX_Footstep_Tile | SFX | 瓷砖脚步 | ❌ |
| Cue_SFX_Footstep_Carpet | SFX | 地毯脚步（闷） | ❌ |
| Cue_SFX_Footstep_Street | SFX | 街道脚步 | ❌ |
| Cue_SFX_DoorOpen | SFX | 开门 | ❌ |
| Cue_SFX_DoorClose | SFX | 关门 | ❌ |
| Cue_SFX_PickUp | SFX | 拾起物品 | ❌ |
| Cue_SFX_WorldSwitch | SFX | 世界切换（闭眼/睁眼） | ❌ |
| Cue_UI_Click | UI | 按钮点击 | ❌ |
| Cue_UI_Hover | UI | 鼠标悬停 | ❌ |
| Cue_UI_DialogueText | UI | 对话出字音 | ❌ |
| Cue_VOX_LinGuNian_xxx | Voice | 林顾念配音 | ❌ |

---

## 🎚 音量设置 UI 接入示例

设置菜单的滑动条：

```csharp
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void Start()
    {
        var am = AudioManager.Instance;
        masterSlider.value = am.MasterVolume;
        musicSlider.value = am.MusicVolume;
        sfxSlider.value = am.SfxVolume;

        masterSlider.onValueChanged.AddListener(am.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(v => am.SetVolume(AudioCategory.Music, v));
        sfxSlider.onValueChanged.AddListener(v => am.SetVolume(AudioCategory.SFX, v));
    }
}
```

PlayerPrefs 自动持久化，下次启动自动恢复。

---

## ⚙️ 不需要手动做的事

- ❌ 不需要在场景里放 AudioManager —— `AudioManagerBootstrapper` 自动创建
- ❌ 不需要管 AudioSource —— SoundEmitter 池化自动管理
- ❌ 不需要写 PlayOneShot / Stop / 淡入淡出循环 —— AudioCue 配置自动处理
- ❌ 不需要担心同一个 SFX 触发太密集 —— minIntervalSeconds 兜底

---

## 🔄 与现有 WorldMusicManager 的关系

现有的 `WorldMusicManager` 是为世界切换 BGM 写的专用脚本，可以**继续用**，也可以**迁移到 AudioManager**：

### 迁移方案（推荐）
- 为 Reality 和 Consciousness 各建一个 `AudioCue`（Loop ✅，FadeIn 1.5s，FadeOut 1.5s）
- `WorldMusicManager` 内部调用 `AudioManager.Instance.PlayMusic(realityCue / consciousnessCue)` 即可
- 自动获得所有 AudioManager 的好处（统一音量、池化、持久化）

### 不迁移方案
- WorldMusicManager 继续用自己的 AudioSource
- 新的 AudioManager 处理所有非 BGM 音效
- 两套并存也能跑，但建议长期迁移统一

---

## 📁 文件位置

```
Assets/Scripts/Audio/
├── AudioCategory.cs          ← 类别枚举
├── AudioCue.cs               ← 数据资源 SO
├── AudioManager.cs           ← 核心单例
├── AudioManagerBootstrapper.cs ← 自动启动
├── SoundEmitter.cs           ← 池化声源
├── FootstepSurface.cs        ← 地面区域
├── FootstepSurfaceTracker.cs ← 区域栈管理
└── README.md                 ← 本文档

Assets/Audio/                 ← 音频资源
├── SFX/
│   ├── Footsteps/            ← 脚步声 wav
│   ├── Doors/                ← 开关门
│   └── ...
├── Music/                    ← BGM wav
├── Ambient/                  ← 环境音 wav
└── UI/                       ← UI 音效 wav

Assets/Resources/Audio/Cues/  ← 推荐：所有 AudioCue 资源放这
├── BGM/
├── SFX/
└── UI/
```
