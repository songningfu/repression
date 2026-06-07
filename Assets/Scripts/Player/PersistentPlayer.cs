// ----------------------------------------------------------------------------
// Script: PersistentPlayer
// 作用：让 player 对象跨场景持久存在（DontDestroyOnLoad 单例）。
//
// 工作原理：
//   - 第一个加载的场景里的 player：注册为全局单例 + DontDestroyOnLoad
//   - 之后切换到的场景如果也有 player（例如 Reality.unity 自己里放着 player）：
//     检测到已有单例 → 销毁自己（保留原 player）
//
// 这样玩家从房间 → 街道 → 回房间，整个过程是同一个 player 实例，状态完全保留。
//
// 使用方法：
//   1. 给 player 对象挂本组件
//   2. 第一个场景（Reality）里保留 player；其他场景（Reality_Street）里不要放 player
//   3. 或者每个场景都可以放 player —— 反正多余的会自动销毁
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Player
{
    [DisallowMultipleComponent]
    public sealed class PersistentPlayer : MonoBehaviour
    {
        public static PersistentPlayer Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 已有 player 实例，销毁这个新场景里多余的副本
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
