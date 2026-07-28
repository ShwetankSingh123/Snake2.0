using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Universal UI selection highlight system that uses a SINGLE global highlight object
    /// to indicate the currently selected UI element. Works with any selectable UI element
    /// including buttons, images, text, panels, and scroll view items.
    /// 
    /// This is more efficient than adding highlight components to every button because:
    /// 1. Only one highlight object exists in the scene
    /// 2. No per-element setup required
    /// 3. Smooth transitions between elements
    /// 4. Works automatically with Unity's EventSystem
    /// </summary>
    public class UISelectionHighlighter : MonoBehaviour
    {
        #region Singleton

        private static UISelectionHighlighter _instance;

        /// <summary>
        /// Singleton instance of the UISelectionHighlighter.
        /// </summary>
        public static UISelectionHighlighter Instance
        {
            get
            {
                if (!UINavigationPlatformUtility.IsNavigationSupported)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UISelectionHighlighter>();
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Highlight Visual")]
        [Tooltip("The RectTransform of the highlight object. Should be a child of the root Canvas.")]
        [SerializeField] private RectTransform _highlightRect;

        [Tooltip("The Image component of the highlight. Use a 9-sliced sprite for rounded corners.")]
        [SerializeField] private Image _highlightImage;

        [Tooltip("Optional CanvasGroup for fading the highlight in/out.")]
        [SerializeField] private CanvasGroup _highlightCanvasGroup;

        [Header("Animation Settings")]
        [Tooltip("Duration for the highlight to move to a new target.")]
        [SerializeField] private float _moveDuration = 0.15f;

        [Tooltip("Duration for the highlight to resize to match the target.")]
        [SerializeField] private float _resizeDuration = 0.12f;

        [Tooltip("Duration for fade in/out animations.")]
        [SerializeField] private float _fadeDuration = 0.1f;

        [Tooltip("Easing for movement animation.")]
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        [Tooltip("Easing for resize animation.")]
        [SerializeField] private Ease _resizeEase = Ease.OutQuad;

        [Header("Padding & Sizing")]
        [Tooltip("Extra padding around the selected element (left, bottom, right, top).")]
        [SerializeField] private Vector4 _defaultPadding = new Vector4(8f, 8f, 8f, 8f);

        [Tooltip("Minimum size of the highlight.")]
        [SerializeField] private Vector2 _minimumSize = new Vector2(50f, 30f);

        [Header("Pulse Animation")]
        [Tooltip("Enable a subtle pulse animation while focused.")]
        [SerializeField] private bool _enablePulse = true;

        [Tooltip("Scale of the pulse (1.0 = no pulse, 1.05 = 5% larger).")]
        [SerializeField] private float _pulseScale = 1.03f;

        [Tooltip("Duration of one pulse cycle.")]
        [SerializeField] private float _pulseDuration = 0.8f;

        [Header("Mouse Hover Support")]
        [Tooltip("If true, highlight will also show when mouse hovers over interactable UI elements.")]
        [SerializeField] private bool _enableMouseHover = true;

        [Tooltip("How often to check for UI under mouse (in seconds). Lower = more responsive but uses more CPU.")]
        [SerializeField] private float _mouseHoverCheckInterval = 0.03f;

        [Tooltip("If true, highlight hides when mouse is not over any interactable element.")]
        [SerializeField] private bool _hideWhenNotHovering = true;

        [Header("Scroll View Support")]
        [Tooltip("If true, the highlight will follow elements inside scroll views.")]
        [SerializeField] private bool _supportScrollViews = true;

        [Tooltip("How often to update position when inside a scroll view (seconds).")]
        [SerializeField] private float _scrollViewUpdateInterval = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Private Fields

        // Cached references
        private Canvas _rootCanvas;
        private RectTransform _canvasRect;
        private Camera _canvasCamera;

        // Current state
        private GameObject _currentTarget;
        private RectTransform _currentTargetRect;
        private UISelectableTarget _currentTargetOverride;
        private ScrollRect _currentScrollRect;

        // Animation tweens
        private Tween _moveTween;
        private Tween _sizeTween;
        private Tween _fadeTween;
        private Tween _pulseTween;

        // Visibility state
        private bool _isVisible;
        private bool _isKeyboardMode = true;
        private float _lastMouseMoveTime;

        // Mouse hover tracking
        private GameObject _currentHoverTarget;
        private float _lastHoverCheckTime;
        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private PointerEventData _pointerEventData;

        // Scroll view tracking
        private Coroutine _scrollTrackingCoroutine;
        private Vector3 _lastTargetWorldPosition;

        // Tween IDs for cleanup
        private readonly string _moveId = "HighlightMove";
        private readonly string _sizeId = "HighlightSize";
        private readonly string _fadeId = "HighlightFade";
        private readonly string _pulseId = "HighlightPulse";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                DisableForUnsupportedPlatform();
                enabled = false;
                return;
            }

            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Find and cache root canvas
            FindRootCanvas();

            // Initialize highlight as hidden
            if (_highlightCanvasGroup != null)
            {
                _highlightCanvasGroup.alpha = 0f;
            }
            else if (_highlightImage != null)
            {
                var color = _highlightImage.color;
                color.a = 0f;
                _highlightImage.color = color;
            }

            _isVisible = false;

            // Initialize pointer event data for mouse hover raycasting
            _pointerEventData = new PointerEventData(EventSystem.current);
        }

        private void OnEnable()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                DisableForUnsupportedPlatform();
                enabled = false;
                return;
            }

            // Start tracking selection changes
            StartCoroutine(TrackSelectionCoroutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            KillAllTweens();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            KillAllTweens();
        }

        private void Update()
        {
            // Handle mouse hover highlighting
            if (_enableMouseHover)
            {
                HandleMouseHover();
            }

            // Detect input mode changes (keyboard vs mouse)
            DetectInputMode();
        }

        #endregion

        /// <summary>
        /// Hides the highlight immediately when navigation is disabled on the current target.
        /// </summary>
        private void DisableForUnsupportedPlatform()
        {
            StopAllCoroutines();
            KillAllTweens();

            if (_highlightCanvasGroup != null)
            {
                _highlightCanvasGroup.alpha = 0f;
            }
            else if (_highlightImage != null)
            {
                var color = _highlightImage.color;
                color.a = 0f;
                _highlightImage.color = color;
            }

            _isVisible = false;
            _currentTarget = null;
            _currentTargetRect = null;
            _currentTargetOverride = null;
            _currentScrollRect = null;
            _currentHoverTarget = null;
        }

        #region Initialization

        /// <summary>
        /// Finds and caches the root canvas for coordinate transformations.
        /// </summary>
        private void FindRootCanvas()
        {
            if (_highlightRect == null)
            {
                Debug.LogError("[UISelectionHighlighter] Highlight RectTransform is not assigned!");
                return;
            }

            // Find root canvas
            _rootCanvas = _highlightRect.GetComponentInParent<Canvas>();
            while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            {
                _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();
            }

            if (_rootCanvas != null)
            {
                _canvasRect = _rootCanvas.GetComponent<RectTransform>();
                _canvasCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay 
                    ? null 
                    : _rootCanvas.worldCamera;
            }
            else
            {
                Debug.LogError("[UISelectionHighlighter] Could not find root Canvas!");
            }
        }

        #endregion

        #region Selection Tracking

        /// <summary>
        /// Coroutine that tracks EventSystem selection changes.
        /// More efficient than checking every frame in Update.
        /// </summary>
        private IEnumerator TrackSelectionCoroutine()
        {
            GameObject lastSelected = null;
            var waitInterval = new WaitForSecondsRealtime(0.02f); // 50 FPS check rate

            while (true)
            {
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    var currentSelected = eventSystem.currentSelectedGameObject;

                    // Selection changed
                    if (currentSelected != lastSelected)
                    {
                        lastSelected = currentSelected;
                        OnSelectionChanged(currentSelected);
                    }
                }

                yield return waitInterval;
            }
        }

        /// <summary>
        /// Called when the EventSystem selection changes.
        /// </summary>
        /// <param name="newSelection">The newly selected GameObject, or null if nothing selected.</param>
        private void OnSelectionChanged(GameObject newSelection)
        {
            if (_debugMode)
            {
                Debug.Log($"[UISelectionHighlighter] Selection changed to: {(newSelection != null ? newSelection.name : "null")}");
            }

            // Stop scroll tracking if active
            StopScrollTracking();

            if (newSelection == null)
            {
                // Nothing selected - hide highlight
                HideHighlight();
                _currentTarget = null;
                _currentTargetRect = null;
                _currentTargetOverride = null;
                _currentScrollRect = null;
                return;
            }

            // Check if the new selection has a RectTransform (is UI element)
            var targetRect = newSelection.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                HideHighlight();
                return;
            }

            // Cache target info
            _currentTarget = newSelection;
            _currentTargetRect = targetRect;
            _currentTargetOverride = newSelection.GetComponent<UISelectableTarget>();

            // Check if this element should be skipped
            if (_currentTargetOverride != null && _currentTargetOverride.SkipHighlight)
            {
                HideHighlight();
                return;
            }

            // Use target override RectTransform if specified
            if (_currentTargetOverride != null && _currentTargetOverride.TargetOverride != null)
            {
                _currentTargetRect = _currentTargetOverride.TargetOverride;
            }

            // Check if target is inside a scroll view
            _currentScrollRect = newSelection.GetComponentInParent<ScrollRect>();

            // Move highlight to new target
            MoveToTarget(_currentTargetRect, _currentTargetOverride);

            // Show highlight - keyboard selection always shows, mouse hover handled separately
            if (_isKeyboardMode)
            {
                ShowHighlight(_currentTargetOverride);
            }

            // Start scroll tracking if inside scroll view
            if (_supportScrollViews && _currentScrollRect != null)
            {
                StartScrollTracking();
            }
        }

        #endregion

        #region Highlight Movement & Sizing

        /// <summary>
        /// Moves and resizes the highlight to match the target element.
        /// </summary>
        /// <param name="targetRect">The target RectTransform to highlight.</param>
        /// <param name="targetOverride">Optional override settings from UISelectableTarget.</param>
        private void MoveToTarget(RectTransform targetRect, UISelectableTarget targetOverride)
        {
            if (_highlightRect == null || targetRect == null) return;

            // Calculate target position and size
            Vector2 targetPosition;
            Vector2 targetSize;
            CalculateHighlightTransform(targetRect, targetOverride, out targetPosition, out targetSize);

            // Store world position for scroll tracking
            _lastTargetWorldPosition = targetRect.position;

            // Animate to new position and size (with optional custom durations)
            AnimateHighlight(targetPosition, targetSize, targetOverride);
        }

        /// <summary>
        /// Calculates the position and size for the highlight based on target element.
        /// </summary>
        private void CalculateHighlightTransform(
            RectTransform targetRect, 
            UISelectableTarget targetOverride,
            out Vector2 position, 
            out Vector2 size)
        {
            // Get padding (use override if available)
            Vector4 padding = targetOverride != null && targetOverride.HasCustomPadding 
                ? targetOverride.Padding 
                : _defaultPadding;

            // Get target's world corners
            Vector3[] worldCorners = new Vector3[4];
            targetRect.GetWorldCorners(worldCorners);

            // Convert world corners to canvas local space
            Vector2[] canvasCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    RectTransformUtility.WorldToScreenPoint(_canvasCamera, worldCorners[i]),
                    _canvasCamera,
                    out canvasCorners[i]
                );
            }

            // Calculate bounds in canvas space
            // worldCorners order: bottom-left, top-left, top-right, bottom-right
            float minX = Mathf.Min(canvasCorners[0].x, canvasCorners[1].x, canvasCorners[2].x, canvasCorners[3].x);
            float maxX = Mathf.Max(canvasCorners[0].x, canvasCorners[1].x, canvasCorners[2].x, canvasCorners[3].x);
            float minY = Mathf.Min(canvasCorners[0].y, canvasCorners[1].y, canvasCorners[2].y, canvasCorners[3].y);
            float maxY = Mathf.Max(canvasCorners[0].y, canvasCorners[1].y, canvasCorners[2].y, canvasCorners[3].y);

            // Apply padding (left, bottom, right, top)
            minX -= padding.x;
            minY -= padding.y;
            maxX += padding.z;
            maxY += padding.w;

            // Calculate size
            float width = maxX - minX;
            float height = maxY - minY;

            // Apply minimum size
            Vector2 minSize = targetOverride != null && targetOverride.HasCustomMinSize 
                ? targetOverride.MinimumSize 
                : _minimumSize;

            width = Mathf.Max(width, minSize.x);
            height = Mathf.Max(height, minSize.y);

            // Calculate center position
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;

            position = new Vector2(centerX, centerY);
            size = new Vector2(width, height);
        }

        /// <summary>
        /// Animates the highlight to a new position and size using DOTween.
        /// </summary>
        /// <param name="targetPosition">Target position in canvas space.</param>
        /// <param name="targetSize">Target size.</param>
        /// <param name="targetOverride">Optional target settings for custom animation durations.</param>
        private void AnimateHighlight(Vector2 targetPosition, Vector2 targetSize, UISelectableTarget targetOverride = null)
        {
            // Kill existing tweens
            DOTween.Kill(_moveId);
            DOTween.Kill(_sizeId);

            // Get animation durations (use custom if specified)
            float moveDuration = (targetOverride != null && targetOverride.HasCustomAnimation) 
                ? targetOverride.MoveDuration 
                : _moveDuration;
            float resizeDuration = (targetOverride != null && targetOverride.HasCustomAnimation) 
                ? targetOverride.ResizeDuration 
                : _resizeDuration;

            // Set highlight anchors to center for easier positioning
            _highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            _highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            _highlightRect.pivot = new Vector2(0.5f, 0.5f);

            // Animate position
            _moveTween = _highlightRect
                .DOAnchorPos(targetPosition, moveDuration)
                .SetEase(_moveEase)
                .SetId(_moveId)
                .SetUpdate(true);

            // Animate size
            _sizeTween = _highlightRect
                .DOSizeDelta(targetSize, resizeDuration)
                .SetEase(_resizeEase)
                .SetId(_sizeId)
                .SetUpdate(true);
        }

        /// <summary>
        /// Immediately snaps the highlight to a position without animation.
        /// Used for initial positioning or when animation would be jarring.
        /// </summary>
        private void SnapToTarget(RectTransform targetRect, UISelectableTarget targetOverride)
        {
            if (_highlightRect == null || targetRect == null) return;

            Vector2 position, size;
            CalculateHighlightTransform(targetRect, targetOverride, out position, out size);

            _highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            _highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            _highlightRect.pivot = new Vector2(0.5f, 0.5f);
            _highlightRect.anchoredPosition = position;
            _highlightRect.sizeDelta = size;
        }

        #endregion

        #region Visibility

        /// <summary>
        /// Shows the highlight with a fade-in animation.
        /// </summary>
        /// <param name="targetOverride">Optional target settings.</param>
        private void ShowHighlight(UISelectableTarget targetOverride = null)
        {
            if (_isVisible) return;
            _isVisible = true;

            DOTween.Kill(_fadeId);

            if (_highlightCanvasGroup != null)
            {
                _fadeTween = _highlightCanvasGroup
                    .DOFade(1f, _fadeDuration)
                    .SetId(_fadeId)
                    .SetUpdate(true);
            }
            else if (_highlightImage != null)
            {
                _fadeTween = _highlightImage
                    .DOFade(1f, _fadeDuration)
                    .SetId(_fadeId)
                    .SetUpdate(true);
            }

            // Start pulse if enabled (and not disabled for this target)
            bool shouldPulse = _enablePulse && (targetOverride == null || !targetOverride.DisablePulse);
            if (shouldPulse)
            {
                StartPulse();
            }

            if (_debugMode)
            {
                Debug.Log("[UISelectionHighlighter] Showing highlight");
            }
        }

        /// <summary>
        /// Hides the highlight with a fade-out animation.
        /// </summary>
        private void HideHighlight()
        {
            if (!_isVisible) return;
            _isVisible = false;

            DOTween.Kill(_fadeId);
            StopPulse();

            if (_highlightCanvasGroup != null)
            {
                _fadeTween = _highlightCanvasGroup
                    .DOFade(0f, _fadeDuration)
                    .SetId(_fadeId)
                    .SetUpdate(true);
            }
            else if (_highlightImage != null)
            {
                _fadeTween = _highlightImage
                    .DOFade(0f, _fadeDuration)
                    .SetId(_fadeId)
                    .SetUpdate(true);
            }

            if (_debugMode)
            {
                Debug.Log("[UISelectionHighlighter] Hiding highlight");
            }
        }

        #endregion

        #region Pulse Animation

        /// <summary>
        /// Starts the pulse animation on the highlight.
        /// Pulse goes outward (scale up) first, then returns to normal size.
        /// </summary>
        private void StartPulse()
        {
            if (!_enablePulse) return;

            DOTween.Kill(_pulseId);

            // Ensure we start at normal scale
            _highlightRect.localScale = Vector3.one;

            // Animate: normal (1.0) → larger (_pulseScale) → normal (1.0) → repeat
            _pulseTween = _highlightRect
                .DOScale(_pulseScale, _pulseDuration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(_pulseId)
                .SetUpdate(true);
        }

        /// <summary>
        /// Stops the pulse animation.
        /// </summary>
        private void StopPulse()
        {
            DOTween.Kill(_pulseId);

            // Reset scale
            if (_highlightRect != null)
            {
                _highlightRect.localScale = Vector3.one;
            }
        }

        #endregion

        #region Mouse Hover Detection

        /// <summary>
        /// Handles mouse hover highlighting by raycasting to find UI elements under the cursor.
        /// </summary>
        private void HandleMouseHover()
        {
            // Don't process mouse hover in keyboard mode
            if (_isKeyboardMode)
            {
                return;
            }

            // Throttle hover checks for performance
            if (Time.unscaledTime - _lastHoverCheckTime < _mouseHoverCheckInterval)
            {
                return;
            }
            _lastHoverCheckTime = Time.unscaledTime;

            // Find interactable UI element under mouse
            GameObject hoveredElement = GetInteractableUIUnderMouse();

            // Hover target changed
            if (hoveredElement != _currentHoverTarget)
            {
                _currentHoverTarget = hoveredElement;

                if (hoveredElement != null)
                {
                    // Show highlight on hovered element
                    OnHoverChanged(hoveredElement);
                }
                else if (_hideWhenNotHovering)
                {
                    // Hide highlight when not hovering anything
                    HideHighlight();
                }
            }
        }

        /// <summary>
        /// Finds the topmost interactable UI element under the mouse cursor.
        /// Only returns elements that are not blocked by other UI.
        /// </summary>
        /// <returns>The interactable GameObject under the mouse, or null if none found.</returns>
        private GameObject GetInteractableUIUnderMouse()
        {
            if (EventSystem.current == null) return null;

            // Update pointer position
            _pointerEventData.position = Input.mousePosition;

            // Raycast to find all UI elements under mouse
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            if (_raycastResults.Count == 0) return null;

            // Find the first (topmost) interactable element
            foreach (var result in _raycastResults)
            {
                GameObject hitObject = result.gameObject;

                // Skip the highlight itself
                if (_highlightRect != null && hitObject.transform.IsChildOf(_highlightRect))
                {
                    continue;
                }

                // Check if the object (or its parent) has an interactable Selectable
                Selectable selectable = hitObject.GetComponent<Selectable>();
                if (selectable == null)
                {
                    selectable = hitObject.GetComponentInParent<Selectable>();
                }

                if (selectable != null && selectable.IsInteractable())
                {
                    // Check if element should be skipped
                    var targetOverride = selectable.GetComponent<UISelectableTarget>();
                    if (targetOverride != null && targetOverride.SkipHighlight)
                    {
                        continue;
                    }

                    return selectable.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Called when the mouse hover target changes.
        /// </summary>
        /// <param name="hoveredElement">The newly hovered element.</param>
        private void OnHoverChanged(GameObject hoveredElement)
        {
            if (_debugMode)
            {
                Debug.Log($"[UISelectionHighlighter] Hover changed to: {hoveredElement.name}");
            }

            // Get RectTransform
            var targetRect = hoveredElement.GetComponent<RectTransform>();
            if (targetRect == null) return;

            // Cache info
            _currentTarget = hoveredElement;
            _currentTargetRect = targetRect;
            _currentTargetOverride = hoveredElement.GetComponent<UISelectableTarget>();

            // Use target override RectTransform if specified
            if (_currentTargetOverride != null && _currentTargetOverride.TargetOverride != null)
            {
                _currentTargetRect = _currentTargetOverride.TargetOverride;
            }

            // Check if inside scroll view
            _currentScrollRect = hoveredElement.GetComponentInParent<ScrollRect>();

            // Move highlight
            MoveToTarget(_currentTargetRect, _currentTargetOverride);

            // Show highlight
            ShowHighlight(_currentTargetOverride);

            // Start scroll tracking if inside scroll view
            StopScrollTracking();
            if (_supportScrollViews && _currentScrollRect != null)
            {
                StartScrollTracking();
            }
        }

        #endregion

        #region Input Mode Detection

        /// <summary>
        /// Detects whether the user is using keyboard/gamepad or mouse/touch.
        /// </summary>
        private void DetectInputMode()
        {
            // Check for keyboard/gamepad input
            bool keyboardInput = Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1);
            
            // Check for navigation input specifically
            bool navInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                           Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f ||
                           Input.GetButtonDown("Submit") ||
                           Input.GetButtonDown("Cancel");

            // Check for mouse movement or click
            bool mouseInput = Input.GetMouseButtonDown(0) || 
                             Input.GetMouseButtonDown(1) ||
                             (Input.mousePosition != (Vector3)_lastMousePosition && 
                              Vector2.Distance(Input.mousePosition, _lastMousePosition) > 5f);

            if (mouseInput)
            {
                _lastMousePosition = Input.mousePosition;
            }

            // Switch to keyboard mode
            if ((keyboardInput || navInput) && !_isKeyboardMode)
            {
                _isKeyboardMode = true;
                _currentHoverTarget = null; // Clear hover target when switching to keyboard

                // Refresh to show keyboard-selected element instead of hovered element
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                {
                    OnSelectionChanged(EventSystem.current.currentSelectedGameObject);
                }
                else
                {
                    HideHighlight();
                }

                if (_debugMode)
                {
                    Debug.Log("[UISelectionHighlighter] Switched to keyboard mode");
                }
            }
            // Switch to mouse mode
            else if (mouseInput && _isKeyboardMode)
            {
                _isKeyboardMode = false;
                
                // Hide highlight - mouse hover will show it on the hovered element
                HideHighlight();

                if (_debugMode)
                {
                    Debug.Log("[UISelectionHighlighter] Switched to mouse mode");
                }
            }
        }

        private Vector2 _lastMousePosition;

        #endregion

        #region Scroll View Support

        /// <summary>
        /// Starts tracking the target element's position for scroll view movement.
        /// </summary>
        private void StartScrollTracking()
        {
            if (_scrollTrackingCoroutine != null)
            {
                StopCoroutine(_scrollTrackingCoroutine);
            }
            _scrollTrackingCoroutine = StartCoroutine(ScrollTrackingCoroutine());
        }

        /// <summary>
        /// Stops scroll view position tracking.
        /// </summary>
        private void StopScrollTracking()
        {
            if (_scrollTrackingCoroutine != null)
            {
                StopCoroutine(_scrollTrackingCoroutine);
                _scrollTrackingCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine that updates highlight position when target moves (inside scroll view).
        /// </summary>
        private IEnumerator ScrollTrackingCoroutine()
        {
            var waitInterval = new WaitForSecondsRealtime(_scrollViewUpdateInterval);

            while (_currentTargetRect != null)
            {
                // Check if target has moved
                if (Vector3.Distance(_currentTargetRect.position, _lastTargetWorldPosition) > 0.1f)
                {
                    _lastTargetWorldPosition = _currentTargetRect.position;

                    // Update highlight position (snap for smoother scroll tracking)
                    Vector2 position, size;
                    CalculateHighlightTransform(_currentTargetRect, _currentTargetOverride, out position, out size);

                    // Use faster animation for scroll tracking
                    DOTween.Kill(_moveId);
                    _highlightRect.DOAnchorPos(position, _scrollViewUpdateInterval)
                        .SetEase(Ease.Linear)
                        .SetId(_moveId)
                        .SetUpdate(true);
                }

                yield return waitInterval;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Forces the highlight to refresh its position and size.
        /// Call this if the target element's size changes dynamically.
        /// </summary>
        public void RefreshHighlight()
        {
            if (_currentTargetRect != null)
            {
                MoveToTarget(_currentTargetRect, _currentTargetOverride);
            }
        }

        /// <summary>
        /// Forces the highlight to a specific element immediately.
        /// </summary>
        /// <param name="target">The element to highlight.</param>
        /// <param name="animate">If true, animates to the target. If false, snaps immediately.</param>
        public void ForceHighlight(GameObject target, bool animate = true)
        {
            if (target == null)
            {
                HideHighlight();
                return;
            }

            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null) return;

            _currentTarget = target;
            _currentTargetRect = targetRect;
            _currentTargetOverride = target.GetComponent<UISelectableTarget>();

            if (animate)
            {
                MoveToTarget(targetRect, _currentTargetOverride);
            }
            else
            {
                SnapToTarget(targetRect, _currentTargetOverride);
            }

            ShowHighlight();
        }

        /// <summary>
        /// Sets the keyboard mode visibility state.
        /// </summary>
        /// <param name="isKeyboard">True for keyboard mode, false for mouse mode.</param>
        public void SetKeyboardMode(bool isKeyboard)
        {
            if (_isKeyboardMode == isKeyboard) return;

            _isKeyboardMode = isKeyboard;

            if (isKeyboard && _currentTarget != null)
            {
                ShowHighlight();
            }
            else if (!isKeyboard)
            {
                HideHighlight();
            }
        }

        /// <summary>
        /// Temporarily disables the highlighter.
        /// </summary>
        public void Disable()
        {
            HideHighlight();
            StopAllCoroutines();
        }

        /// <summary>
        /// Re-enables the highlighter after being disabled.
        /// </summary>
        public void Enable()
        {
            StartCoroutine(TrackSelectionCoroutine());

            // Refresh to current selection
            if (EventSystem.current != null)
            {
                OnSelectionChanged(EventSystem.current.currentSelectedGameObject);
            }
        }

        /// <summary>
        /// Updates the default padding at runtime.
        /// </summary>
        /// <param name="padding">New padding (left, bottom, right, top).</param>
        public void SetPadding(Vector4 padding)
        {
            _defaultPadding = padding;
            RefreshHighlight();
        }

        /// <summary>
        /// Enables or disables the pulse animation at runtime.
        /// </summary>
        /// <param name="enable">True to enable pulse, false to disable.</param>
        public void SetPulseEnabled(bool enable)
        {
            _enablePulse = enable;

            if (_isVisible)
            {
                if (enable)
                {
                    StartPulse();
                }
                else
                {
                    StopPulse();
                }
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Kills all active DOTween animations.
        /// </summary>
        private void KillAllTweens()
        {
            DOTween.Kill(_moveId);
            DOTween.Kill(_sizeId);
            DOTween.Kill(_fadeId);
            DOTween.Kill(_pulseId);
        }

        #endregion

        #region Unity 2023+ Compatibility

#if UNITY_2023_1_OR_NEWER
        private new static T FindFirstObjectByType<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }
#else
        private static T FindFirstObjectByType<T>() where T : Object
        {
            return Object.FindObjectOfType<T>();
        }
#endif

        #endregion
    }
}
