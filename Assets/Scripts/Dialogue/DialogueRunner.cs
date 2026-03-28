// ----------------------------------------------------------------------------
// Script: DialogueRunner
// 作用：管理对话启动、节点推进、应激过滤、选项选择与历史记录，是 A2 的核心状态机。
// 使用方法/调用示例：把图资源和 View 绑定到该组件，然后调用 StartDialogue("100") 开始一段对话。
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using SeeAPsychologist.MentalStats;
using UnityEngine;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 数据驱动的对话运行器。
    /// UI 只订阅本组件事件做显示，核心状态与应激度修改都由本组件统一调度。
    /// </summary>
    public sealed class DialogueRunner : MonoBehaviour
    {
        private const string NextLabelNext = "下一句";
        private const string NextLabelClose = "关闭";

        [Header("References")]
        [SerializeField] private DialogueGraphData graph;
        [SerializeField] private DialogueView view;
        [SerializeField] private MentalStatsManager mentalStatsManager;
        [SerializeField] private bool autoFindMentalStatsManager = true;

        [Header("Debug")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private string debugStartNodeId;
        [SerializeField] private bool verboseLog;

        private readonly List<DialogueHistoryEntry> _historyEntries = new();
        private readonly List<DialogueChoiceViewModel> _currentPresentedChoices = new();
        private readonly Dictionary<int, DialogueChoiceData> _choiceLookup = new();

        private string _startNodeId;
        private string _currentNodeId;
        private bool _isActive;
        private bool _isTyping;
        private bool _fastModeEnabled;
        private DialogueControlState _lastControlState;

        /// <summary>
        /// 对话面板显隐事件。
        /// </summary>
        public event Action<bool> OnDialogueVisibilityChanged;

        /// <summary>
        /// 当前节点展示事件。
        /// </summary>
        public event Action<DialogueNodeViewModel> OnNodePresented;

        /// <summary>
        /// 当前可见选项事件。
        /// </summary>
        public event Action<IReadOnlyList<DialogueChoiceViewModel>> OnChoicesPresented;

        /// <summary>
        /// 历史记录刷新事件。
        /// </summary>
        public event Action<IReadOnlyList<DialogueHistoryEntry>> OnHistoryChanged;

        /// <summary>
        /// 控件状态刷新事件。
        /// </summary>
        public event Action<DialogueControlState> OnControlStateChanged;

        /// <summary>
        /// 对话结束事件。
        /// </summary>
        public event Action<DialogueSessionEndedEvent> OnDialogueEnded;

        public bool IsActive => _isActive;
        public bool FastModeEnabled => _fastModeEnabled;
        public DialogueGraphData Graph => graph;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponentInChildren<DialogueView>(true);
            }

            if (view != null)
            {
                view.BindRunner(this);
            }

            ResolveMentalStatsManager();
        }

        private void Start()
        {
            if (playOnStart && !string.IsNullOrWhiteSpace(debugStartNodeId))
            {
                StartDialogue(debugStartNodeId);
            }
        }

        /// <summary>
        /// 启动一段对话。
        /// </summary>
        /// <param name="startNodeId">入口节点 ID。</param>
        /// <returns>成功进入对话返回 true。</returns>
        public bool StartDialogue(string startNodeId)
        {
            if (graph == null)
            {
                Debug.LogError("[Dialogue] DialogueGraphData is missing. StartDialogue aborted.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(startNodeId))
            {
                Debug.LogWarning("[Dialogue] StartDialogue called with empty startNodeId.");
                return false;
            }

            if (_isActive)
            {
                Debug.LogWarning("[Dialogue] Another dialogue is already running. Start request ignored.");
                return false;
            }

            if (!graph.TryGetNode(startNodeId, out _))
            {
                Debug.LogError($"[Dialogue] Start node '{startNodeId}' does not exist in graph '{graph.GraphId}'.");
                return false;
            }

            ResolveMentalStatsManager();

            _isActive = true;
            _startNodeId = startNodeId.Trim();
            _historyEntries.Clear();
            NotifyHistoryChanged();
            OnDialogueVisibilityChanged?.Invoke(true);

            if (verboseLog)
            {
                Debug.Log($"[Dialogue] Start graph='{graph.GraphId}' node='{_startNodeId}'.");
            }

            PresentNode(_startNodeId);
            return true;
        }

        /// <summary>
        /// 外部主动结束当前对话。
        /// </summary>
        public void StopDialogue(string reason = "StoppedExternally")
        {
            if (!_isActive)
            {
                return;
            }

            FinishDialogue(reason);
        }

        /// <summary>
        /// 由 UI 在打字机完成时回调。
        /// </summary>
        public void NotifyLinePresentationCompleted()
        {
            if (!_isActive || !_isTyping)
            {
                return;
            }

            _isTyping = false;
            PresentChoicesOrAdvance();
        }

        /// <summary>
        /// UI 在“下一句”按钮可推进状态下调用。
        /// </summary>
        public void RequestAdvance()
        {
            if (!_isActive || _isTyping)
            {
                return;
            }

            if (!graph.TryGetNode(_currentNodeId, out var node))
            {
                FinishDialogue("CurrentNodeMissing");
                return;
            }

            var hasChoices = graph.GetChoices(_currentNodeId).Count > 0;
            if (hasChoices && _currentPresentedChoices.Count > 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(node.nextId))
            {
                PresentNode(node.nextId);
                return;
            }

            FinishDialogue("ReachedTerminalNode");
        }

        /// <summary>
        /// UI 在玩家点击选项时调用。
        /// </summary>
        public void RequestSelectChoice(int choiceId)
        {
            if (!_isActive || _isTyping)
            {
                return;
            }

            if (!_choiceLookup.TryGetValue(choiceId, out var choice))
            {
                Debug.LogWarning($"[Dialogue] Choice '{choiceId}' is not available in current state; click ignored.");
                return;
            }

            if (!AppendHistoryChoice(choice.optionText))
            {
                Debug.LogWarning($"[Dialogue] Choice '{choiceId}' has empty option text.");
            }

            if (mentalStatsManager != null && choice.stressModifier != 0)
            {
                mentalStatsManager.ModifyStress(
                    choice.stressModifier,
                    MentalStatChangeSource.DialogueOption,
                    context: $"Node={_currentNodeId};Choice={choice.choiceId}");
            }

            if (!string.IsNullOrWhiteSpace(choice.nextId))
            {
                PresentNode(choice.nextId);
                return;
            }

            FinishDialogue("ChoiceWithoutNextNode");
        }

        /// <summary>
        /// 设置打字机是否进入加速模式。
        /// </summary>
        public void SetFastMode(bool enabled)
        {
            if (_fastModeEnabled == enabled)
            {
                return;
            }

            _fastModeEnabled = enabled;
            RaiseControlState(
                showNextButton: _lastControlState.ShowNextButton,
                canAdvance: _lastControlState.CanAdvance,
                nextButtonLabel: _lastControlState.NextButtonLabel);
        }

        private void ResolveMentalStatsManager()
        {
            if (mentalStatsManager == null && autoFindMentalStatsManager)
            {
                mentalStatsManager = MentalStatsManager.Instance;
            }
        }

        private void PresentNode(string nodeId)
        {
            if (!graph.TryGetNode(nodeId, out var node))
            {
                Debug.LogError($"[Dialogue] Target node '{nodeId}' does not exist. Dialogue finished.");
                FinishDialogue("TargetNodeMissing");
                return;
            }

            _currentNodeId = node.nodeId?.Trim();
            _isTyping = true;
            _currentPresentedChoices.Clear();
            _choiceLookup.Clear();
            RaiseChoicesPresented();

            AppendHistoryLine(node.speakerName, node.text);

            OnNodePresented?.Invoke(new DialogueNodeViewModel(
                nodeId: node.nodeId,
                speakerName: node.speakerName,
                portraitKey: node.portraitKey,
                text: node.text));

            RaiseControlState(showNextButton: true, canAdvance: true);

            if (verboseLog)
            {
                Debug.Log($"[Dialogue] Present node '{node.nodeId}'.");
            }
        }

        private void PresentChoicesOrAdvance()
        {
            if (!_isActive || !graph.TryGetNode(_currentNodeId, out var node))
            {
                FinishDialogue("CurrentNodeMissingAfterTyping");
                return;
            }

            var sourceChoices = graph.GetChoices(_currentNodeId);
            if (sourceChoices.Count == 0)
            {
                RaiseControlState(showNextButton: true, canAdvance: true, nextButtonLabel: ResolveNextButtonLabel(node));
                return;
            }

            var currentStress = mentalStatsManager != null ? mentalStatsManager.CurrentStress : 0;
            var hasVisibleChoices = false;
            var hasInteractableChoices = false;

            _currentPresentedChoices.Clear();
            _choiceLookup.Clear();

            for (var i = 0; i < sourceChoices.Count; i++)
            {
                var choice = sourceChoices[i];
                if (choice == null)
                {
                    continue;
                }

                var available = choice.IsAvailableForStress(currentStress);
                if (!available && choice.unavailableMode == DialogueChoiceAvailabilityMode.Hidden)
                {
                    continue;
                }

                hasVisibleChoices = true;
                if (available)
                {
                    hasInteractableChoices = true;
                    _choiceLookup[choice.choiceId] = choice;
                }

                _currentPresentedChoices.Add(new DialogueChoiceViewModel(
                    choiceId: choice.choiceId,
                    text: choice.optionText,
                    interactable: available,
                    stressModifier: choice.stressModifier));
            }

            RaiseChoicesPresented();

            if (hasVisibleChoices)
            {
                var canFallbackAdvance = !hasInteractableChoices && !string.IsNullOrWhiteSpace(node.nextId);
                RaiseControlState(
                    showNextButton: canFallbackAdvance,
                    canAdvance: canFallbackAdvance,
                    nextButtonLabel: canFallbackAdvance ? ResolveNextButtonLabel(node) : NextLabelNext);

                if (!hasInteractableChoices)
                {
                    Debug.LogWarning($"[Dialogue] Node '{_currentNodeId}' has no interactable choices under stress={currentStress}. " +
                                     "Fallback advance is enabled only when NextId is configured.");
                }

                return;
            }

            Debug.LogWarning($"[Dialogue] Node '{_currentNodeId}' choices are all hidden under stress={currentStress}. Fallback advance will be used.");
            RaiseControlState(showNextButton: true, canAdvance: true, nextButtonLabel: ResolveNextButtonLabel(node));
        }

        private string ResolveNextButtonLabel(DialogueNodeData node)
        {
            return string.IsNullOrWhiteSpace(node.nextId) ? NextLabelClose : NextLabelNext;
        }

        private void FinishDialogue(string reason)
        {
            var evt = new DialogueSessionEndedEvent(
                startNodeId: _startNodeId,
                endNodeId: _currentNodeId,
                reason: reason);

            _isActive = false;
            _isTyping = false;
            _currentNodeId = null;
            _currentPresentedChoices.Clear();
            _choiceLookup.Clear();

            RaiseChoicesPresented();
            RaiseControlState(showNextButton: false, canAdvance: false);
            OnDialogueVisibilityChanged?.Invoke(false);
            OnDialogueEnded?.Invoke(evt);

            if (verboseLog)
            {
                Debug.Log($"[Dialogue] End reason='{reason}', start='{evt.StartNodeId}', end='{evt.EndNodeId}'.");
            }
        }

        private void RaiseChoicesPresented()
        {
            OnChoicesPresented?.Invoke(_currentPresentedChoices);
        }

        private void RaiseControlState(bool showNextButton, bool canAdvance, string nextButtonLabel = NextLabelNext)
        {
            _lastControlState = new DialogueControlState(
                isTyping: _isTyping,
                showNextButton: showNextButton,
                canAdvance: canAdvance,
                fastModeEnabled: _fastModeEnabled,
                nextButtonLabel: nextButtonLabel);
            OnControlStateChanged?.Invoke(_lastControlState);
        }

        private void NotifyHistoryChanged()
        {
            OnHistoryChanged?.Invoke(_historyEntries);
        }

        private void AppendHistoryLine(string speakerName, string content)
        {
            _historyEntries.Add(new DialogueHistoryEntry(
                isChoice: false,
                speakerName: speakerName,
                content: content));
            NotifyHistoryChanged();
        }

        private bool AppendHistoryChoice(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            _historyEntries.Add(new DialogueHistoryEntry(
                isChoice: true,
                speakerName: string.Empty,
                content: content));
            NotifyHistoryChanged();
            return true;
        }
    }
}
