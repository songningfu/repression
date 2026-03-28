// ----------------------------------------------------------------------------
// Script: DialoguePortraitLibrary
// 作用：提供 PortraitKey 到 Sprite 的可配置映射，避免在代码里硬编码头像资源。
// 使用方法/调用示例：创建资源后，在 DialogueView 上绑定该资源并维护条目映射。
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 对话立绘映射表。
    /// </summary>
    [CreateAssetMenu(menuName = "SeeAPsychologist/Dialogue/DialoguePortraitLibrary", fileName = "DialoguePortraitLibrary")]
    public sealed class DialoguePortraitLibrary : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            public string key;
            public Sprite portrait;
        }

        [SerializeField] private List<Entry> entries = new();

        private readonly Dictionary<string, Sprite> _lookup = new();

        private void OnEnable()
        {
            RebuildLookup();
        }

        /// <summary>
        /// 根据字符串 Key 查找立绘；未命中时返回 false。
        /// </summary>
        public bool TryGetPortrait(string key, out Sprite portrait)
        {
            if (_lookup.Count != entries.Count)
            {
                RebuildLookup();
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                portrait = null;
                return false;
            }

            return _lookup.TryGetValue(key.Trim(), out portrait);
        }

        private void RebuildLookup()
        {
            _lookup.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                var key = entry.key.Trim();
                if (_lookup.ContainsKey(key))
                {
                    Debug.LogWarning($"[Dialogue] Duplicate portrait key '{key}' in library '{name}'. Later entry ignored.");
                    continue;
                }

                _lookup.Add(key, entry.portrait);
            }
        }
    }
}
