// ----------------------------------------------------------------------------
// Script: FootstepSurfaceTracker
// 作用：跟踪玩家当前所在的所有 FootstepSurface，按优先级选择当前生效的 cue。
//       解决"两块地面重叠时该用哪个"的问题。
//
// 设计：
//   - 每个 PlayerFootsteps 实例对应一个 Tracker（弱引用字典）
//   - Enter → 加入栈；Exit → 从栈移除
//   - 选择 cue：栈中 stackPriority 最高的（同优先级用最后进入的）
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using SeeAPsychologist.Player;

namespace SeeAPsychologist.Audio
{
    public sealed class FootstepSurfaceTracker
    {
        private static readonly Dictionary<PlayerFootsteps, FootstepSurfaceTracker> _trackers = new();

        public static FootstepSurfaceTracker Get(PlayerFootsteps pf)
        {
            if (!_trackers.TryGetValue(pf, out var t))
            {
                t = new FootstepSurfaceTracker(pf);
                _trackers[pf] = t;
            }
            return t;
        }

        private readonly PlayerFootsteps _player;
        private readonly List<FootstepSurface> _stack = new();

        private FootstepSurfaceTracker(PlayerFootsteps p) { _player = p; }

        public void Push(FootstepSurface s)
        {
            if (!_stack.Contains(s)) _stack.Add(s);
            ApplyTop();
        }

        public void Pop(FootstepSurface s)
        {
            _stack.Remove(s);
            ApplyTop();
        }

        private void ApplyTop()
        {
            // 清理失效引用
            for (int i = _stack.Count - 1; i >= 0; i--)
                if (_stack[i] == null) _stack.RemoveAt(i);

            if (_stack.Count == 0)
            {
                _player.ResetToDefaultCue();
                return;
            }

            FootstepSurface top = _stack[0];
            for (int i = 1; i < _stack.Count; i++)
            {
                if (_stack[i].StackPriority >= top.StackPriority) top = _stack[i];
            }
            _player.SetCue(top.Cue);
        }
    }
}
