// ----------------------------------------------------------------------------
// Script: DialogueView
// 作用：负责对话面板显示、打字机与选项按钮生成。
// 使用方法/调用示例：把该脚本挂到对话面板上，并在 Inspector 中绑定 Runner、按钮、TMP 和选项按钮 Prefab。
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 对话 UI 视图层。
    /// </summary>
    public sealed class DialogueView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private DialoguePortraitLibrary portraitLibrary;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Transform choicesRoot;
        [SerializeField] private DialogueChoiceButtonView choiceButtonPrefab;

        [Header("Controls")]
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonLabel;
        [SerializeField] private bool showNextButton;
        [Tooltip("开启后点击屏幕任意位置可推进对话（打字中则瞬间显示全文）。")]
        [SerializeField] private bool clickAnywhereToAdvance = true;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset dialogueFont;

        [Header("Portrait Layout")]
        [SerializeField] private bool applyPortraitLayout = true;
        [SerializeField] private Vector2 portraitMaskSize = new(150f, 170f);
        [SerializeField] private Vector2 portraitSpriteSize = new(150f, 300f);
        [SerializeField] private float portraitSpriteYOffset = -55f;
        [SerializeField] private Vector2 portraitMaskAnchoredPosition = new(100f, 24f);

        [Header("Typewriter")]
        [SerializeField] private float charactersPerSecond = 24f;
        [SerializeField] private float fastCharactersPerSecond = 72f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip typewriterClip;
        [SerializeField] private AudioClip choiceClickClip;
        [SerializeField] private float typewriterClipInterval = 0.05f;

        private readonly List<DialogueChoiceButtonView> _spawnedChoices = new();

        private Coroutine _typewriterRoutine;
        private DialogueControlState _controlState;
        private string _currentFullText = string.Empty;
        private float _typewriterAudioCooldown;
        private bool _subscribed;

        private void Awake()
        {
            panelRoot ??= gameObject;
            // 默认先隐藏，避免场景初始状态下对话面板拦截其它 UI 点击。
            // 真正进入对话时由 DialogueRunner.OnDialogueVisibilityChanged(true) 再显示。
            SetPanelVisible(false);

            if (runner == null)
            {
                runner = GetComponentInParent<DialogueRunner>();
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(HandleNextClicked);
                nextButton.gameObject.SetActive(showNextButton);
            }

            ApplyDialogueFont();
            SetupPortraitMask();

            if (runner != null)
            {
                BindRunner(runner);
            }

            ConfigureRaycastTargets();
        }

        private void OnEnable()
        {
            SubscribeRunner();
        }

        private void OnDisable()
        {
            UnsubscribeRunner();
            StopTypewriter();
            ClearChoices();
        }

        private void OnDestroy()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextClicked);
            }

        }

        private void Update()
        {
            if (!clickAnywhereToAdvance || runner == null || !runner.IsActive)
            {
                return;
            }

            if (!WasAdvanceInputPressed())
            {
                return;
            }

            HandleNextClicked();
        }

        /// <summary>
        /// 给外部运行器绑定当前 View。
        /// </summary>
        public void BindRunner(DialogueRunner newRunner)
        {
            if (runner == newRunner)
            {
                SubscribeRunner();
                return;
            }

            UnsubscribeRunner();
            runner = newRunner;
            SubscribeRunner();
        }

        private void SubscribeRunner()
        {
            if (_subscribed || runner == null)
            {
                return;
            }

            runner.OnDialogueVisibilityChanged += HandleDialogueVisibilityChanged;
            runner.OnNodePresented += HandleNodePresented;
            runner.OnChoicesPresented += HandleChoicesPresented;
            runner.OnControlStateChanged += HandleControlStateChanged;
            runner.OnDialogueEnded += HandleDialogueEnded;
            _subscribed = true;
        }

        private void UnsubscribeRunner()
        {
            if (!_subscribed || runner == null)
            {
                return;
            }

            runner.OnDialogueVisibilityChanged -= HandleDialogueVisibilityChanged;
            runner.OnNodePresented -= HandleNodePresented;
            runner.OnChoicesPresented -= HandleChoicesPresented;
            runner.OnControlStateChanged -= HandleControlStateChanged;
            runner.OnDialogueEnded -= HandleDialogueEnded;
            _subscribed = false;
        }

        private void HandleDialogueVisibilityChanged(bool visible)
        {
            SetPanelVisible(visible);

            if (!visible)
            {
                StopTypewriter();
                ClearChoices();
                SetDialogueText(string.Empty, fullyVisible: true);
            }
        }

        private void HandleNodePresented(DialogueNodeViewModel model)
        {
            UpdatePortrait(model.PortraitKey);

            if (speakerNameText != null)
            {
                speakerNameText.text = model.SpeakerName ?? string.Empty;
            }

            _currentFullText = model.Text ?? string.Empty;
            StartTypewriter(_currentFullText);
        }

        private void HandleChoicesPresented(IReadOnlyList<DialogueChoiceViewModel> choices)
        {
            ClearChoices();

            if (choicesRoot == null || choiceButtonPrefab == null || choices == null)
            {
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choiceView = Instantiate(choiceButtonPrefab, choicesRoot);
                choiceView.Bind(choices[i], HandleChoiceClicked);
                _spawnedChoices.Add(choiceView);
            }
        }

        private void HandleControlStateChanged(DialogueControlState controlState)
        {
            _controlState = controlState;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNextButton && controlState.ShowNextButton);
                nextButton.interactable = controlState.CanAdvance;
            }

            if (nextButtonLabel != null)
            {
                nextButtonLabel.text = controlState.NextButtonLabel ?? string.Empty;
            }
        }

        private void HandleDialogueEnded(DialogueSessionEndedEvent evt)
        {
            StopTypewriter();
        }

        private void HandleNextClicked()
        {
            if (runner == null)
            {
                return;
            }

            if (_controlState.IsTyping)
            {
                CompleteTypewriterImmediately();
                return;
            }

            if (_spawnedChoices.Count > 0)
            {
                return;
            }

            if (!_controlState.CanAdvance)
            {
                return;
            }

            runner.RequestAdvance();
        }

        private void HandleChoiceClicked(int choiceId)
        {
            PlayOneShot(choiceClickClip);
            runner?.RequestSelectChoice(choiceId);
        }

        private void StartTypewriter(string fullText)
        {
            StopTypewriter();
            SetDialogueText(fullText, fullyVisible: false);
            _typewriterRoutine = StartCoroutine(TypewriterRoutine(fullText));
        }

        private void StopTypewriter()
        {
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }
        }

        private void CompleteTypewriterImmediately()
        {
            StopTypewriter();
            SetDialogueText(_currentFullText, fullyVisible: true);
            runner?.NotifyLinePresentationCompleted();
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            if (dialogueText == null)
            {
                runner?.NotifyLinePresentationCompleted();
                yield break;
            }

            dialogueText.ForceMeshUpdate();
            var revealedCount = 0f;
            _typewriterAudioCooldown = 0f;

            while (revealedCount < fullText.Length)
            {
                var speed = Mathf.Max(1f, runner != null && runner.FastModeEnabled ? fastCharactersPerSecond : charactersPerSecond);
                var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (deltaTime <= 0f)
                {
                    yield return null;
                    continue;
                }

                revealedCount += speed * deltaTime;

                var visibleCount = Mathf.Clamp(Mathf.FloorToInt(revealedCount), 0, fullText.Length);
                dialogueText.maxVisibleCharacters = visibleCount;

                _typewriterAudioCooldown -= deltaTime;
                if (typewriterClip != null && visibleCount > 0 && _typewriterAudioCooldown <= 0f)
                {
                    PlayOneShot(typewriterClip);
                    _typewriterAudioCooldown = Mathf.Max(0.01f, typewriterClipInterval);
                }

                yield return null;
            }

            SetDialogueText(fullText, fullyVisible: true);
            _typewriterRoutine = null;
            runner?.NotifyLinePresentationCompleted();
        }

        private void SetDialogueText(string textValue, bool fullyVisible)
        {
            if (dialogueText == null)
            {
                return;
            }

            dialogueText.text = textValue ?? string.Empty;
            dialogueText.ForceMeshUpdate();
            dialogueText.maxVisibleCharacters = fullyVisible ? int.MaxValue : 0;
        }

        private void UpdatePortrait(string portraitKey)
        {
            if (portraitImage == null)
            {
                return;
            }

            if (portraitLibrary != null && portraitLibrary.TryGetPortrait(portraitKey, out var portrait))
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
                portraitImage.preserveAspect = true;
                return;
            }

            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void ClearChoices()
        {
            for (var i = 0; i < _spawnedChoices.Count; i++)
            {
                if (_spawnedChoices[i] != null)
                {
                    Destroy(_spawnedChoices[i].gameObject);
                }
            }

            _spawnedChoices.Clear();
        }

        private void ConfigureRaycastTargets()
        {
            if (portraitImage != null)
            {
                portraitImage.raycastTarget = false;
            }

            DisableTextRaycast(speakerNameText);
            DisableTextRaycast(dialogueText);
            DisableTextRaycast(nextButtonLabel);
        }

        private static void DisableTextRaycast(TMP_Text text)
        {
            if (text != null)
            {
                text.raycastTarget = false;
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || uiAudioSource == null)
            {
                return;
            }

            uiAudioSource.PlayOneShot(clip);
        }

        private void ApplyDialogueFont()
        {
            if (dialogueFont == null)
            {
                return;
            }

            ApplyFontTo(speakerNameText);
            ApplyFontTo(dialogueText);
            ApplyFontTo(nextButtonLabel);
        }

        private void ApplyFontTo(TMP_Text text)
        {
            if (text == null || dialogueFont == null)
            {
                return;
            }

            text.font = dialogueFont;
        }

        private void SetupPortraitMask()
        {
            if (!applyPortraitLayout || portraitImage == null)
            {
                return;
            }

            var spriteRect = portraitImage.rectTransform;
            if (spriteRect.parent != null && spriteRect.parent.name == "PortraitMask")
            {
                return;
            }

            var maskGo = new GameObject("PortraitMask", typeof(RectTransform), typeof(RectMask2D));
            var maskRect = maskGo.GetComponent<RectTransform>();
            maskRect.SetParent(spriteRect.parent, false);
            maskRect.SetSiblingIndex(spriteRect.GetSiblingIndex());

            maskRect.anchorMin = new Vector2(0f, 0f);
            maskRect.anchorMax = new Vector2(0f, 0f);
            maskRect.pivot = new Vector2(0.5f, 0f);
            maskRect.anchoredPosition = portraitMaskAnchoredPosition;
            maskRect.sizeDelta = portraitMaskSize;

            spriteRect.SetParent(maskRect, false);
            spriteRect.anchorMin = new Vector2(0.5f, 0f);
            spriteRect.anchorMax = new Vector2(0.5f, 0f);
            spriteRect.pivot = new Vector2(0.5f, 0f);
            spriteRect.anchoredPosition = new Vector2(0f, portraitSpriteYOffset);
            spriteRect.sizeDelta = portraitSpriteSize;
        }

        private static bool WasAdvanceInputPressed()
        {
            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }

            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

    }
}
