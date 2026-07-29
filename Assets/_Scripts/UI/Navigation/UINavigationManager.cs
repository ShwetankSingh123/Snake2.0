using System;
using System.Collections.Generic;
//using Almace.SpeedRacer.UI;
//using Constant;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Manages keyboard/gamepad navigation for Unity UI while preserving mouse/touch input.
    /// Attach this to a persistent GameObject in your scene (e.g., EventSystem or dedicated manager).
    /// Requires: EventSystem with StandaloneInputModule or InputSystemUIInputModule.
    /// </summary>
    public class UINavigationManager : MonoBehaviour
    {
        #region Static Events

        /// <summary>
        /// Fired when input mode switches to keyboard/gamepad.
        /// Subscribe to this to clear mouse hover states.
        /// </summary>
        public static event Action OnSwitchedToKeyboardMode;

        /// <summary>
        /// Fired when input mode switches to mouse.
        /// </summary>
        public static event Action OnSwitchedToMouseMode;

        /// <summary>
        /// Fired when the user requests exit confirmation (e.g. pressed Escape on home screen
        /// or when no groups are active).
        /// </summary>
        public static event Action OnRequestExitConfirmation;

        #endregion

        #region Input System Compatibility Helpers

        private Vector2 GetSafeMousePosition()
        {
            try
            {
                return Input.mousePosition;
            }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                }
#endif
                return _lastMousePosition;
            }
        }

        private bool SafeGetKeyDown(KeyCode kc)
        {
            try { return Input.GetKeyDown(kc); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb == null) return false;
                switch (kc)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        return kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey?.wasPressedThisFrame == true;
                    case KeyCode.Space:
                        return kb.spaceKey.wasPressedThisFrame;
                    case KeyCode.Escape:
                        return kb.escapeKey.wasPressedThisFrame;
                    case KeyCode.Tab:
                        return kb.tabKey.wasPressedThisFrame;
                    case KeyCode.LeftArrow:
                        return kb.leftArrowKey.wasPressedThisFrame;
                    case KeyCode.RightArrow:
                        return kb.rightArrowKey.wasPressedThisFrame;
                    case KeyCode.UpArrow:
                        return kb.upArrowKey.wasPressedThisFrame;
                    case KeyCode.DownArrow:
                        return kb.downArrowKey.wasPressedThisFrame;
                    case KeyCode.A:
                        return kb.aKey.wasPressedThisFrame;
                    case KeyCode.D:
                        return kb.dKey.wasPressedThisFrame;
                    case KeyCode.W:
                        return kb.wKey.wasPressedThisFrame;
                    case KeyCode.S:
                        return kb.sKey.wasPressedThisFrame;
                    default:
                        break;
                }
#endif
                return false;
            }
        }

        private bool SafeGetKey(KeyCode kc)
        {
            try { return Input.GetKey(kc); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb == null) return false;
                switch (kc)
                {
                    case KeyCode.LeftArrow:
                        return kb.leftArrowKey.isPressed;
                    case KeyCode.RightArrow:
                        return kb.rightArrowKey.isPressed;
                    case KeyCode.UpArrow:
                        return kb.upArrowKey.isPressed;
                    case KeyCode.DownArrow:
                        return kb.downArrowKey.isPressed;
                    case KeyCode.A:
                        return kb.aKey.isPressed;
                    case KeyCode.D:
                        return kb.dKey.isPressed;
                    case KeyCode.W:
                        return kb.wKey.isPressed;
                    case KeyCode.S:
                        return kb.sKey.isPressed;
                    default:
                        break;
                }
#endif
                return false;
            }
        }

        private float SafeGetAxis(string axisName)
        {
            try { return Input.GetAxis(axisName); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                // Attempt to approximate using gamepad left stick if available
                var gp = UnityEngine.InputSystem.Gamepad.current;
                if (gp != null)
                {
                    var v = gp.leftStick.ReadValue();
                    if (axisName == _horizontalAxis) return v.x;
                    if (axisName == _verticalAxis) return v.y;
                }
#endif
                return 0f;
            }
        }

        private bool SafeGetButtonDown(string buttonName)
        {
            try { return Input.GetButtonDown(buttonName); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                // No generic mapping for named buttons in new Input System here - return false
                return false;
#else
                return false;
#endif
            }
        }

        #endregion

        #region Singleton

        private static UINavigationManager s_instance;

        /// <summary>
        /// Singleton instance of the UINavigationManager.
        /// </summary>
        public static UINavigationManager Instance
        {
            get
            {
                if (!UINavigationPlatformUtility.IsNavigationSupported)
                {
                    return null;
                }

                if (s_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    s_instance = FindFirstObjectByType<UINavigationManager>();
#else
                    s_instance = FindObjectOfType<UINavigationManager>();
#endif
                    if (s_instance == null)
                    {
                        Debug.LogWarning("[UINavigationManager] No instance found in scene. Creating one.");
                        var go = new GameObject("UINavigationManager");
                        s_instance = go.AddComponent<UINavigationManager>();
                    }
                }
                return s_instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Input Detection")]
        [Tooltip("Axes used for navigation (Legacy Input). Supports both arrow keys and WASD by default.")]
        [SerializeField] private string _horizontalAxis = "Horizontal";
        [SerializeField] private string _verticalAxis = "Vertical";

        [Tooltip("Button names for submit action (Legacy Input).")]
        [SerializeField] private string[] _submitButtons = { "Submit", "Jump" };

        [Tooltip("Button names for cancel action (Legacy Input).")]
        [SerializeField] private string[] _cancelButtons = { "Cancel" };

        [Header("Navigation Settings")]
        [Tooltip("Time in seconds before allowing repeated navigation while holding direction.")]
        [SerializeField] private float _repeatDelay = 0.5f;

        [Tooltip("Time between repeated navigation events when holding direction.")]
        [SerializeField] private float _repeatRate = 0.1f;

        [Tooltip("Threshold for axis input to register as navigation.")]
        [SerializeField] private float _inputThreshold = 0.5f;

        [Header("Auto-Selection")]
        [Tooltip("If true, automatically selects a UI element when keyboard/gamepad input is detected and nothing is selected.")]
        [SerializeField] private bool _autoSelectOnInput = true;

        [Tooltip("Time after mouse movement before allowing auto-selection again.")]
        [SerializeField] private float _mouseIdleTime = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Private Fields

        // Input state tracking
        private Vector2 _lastMousePosition;
        private float _lastMouseMoveTime;
        private float _lastNavigationTime;
        private Vector2 _lastInputDirection;
        private bool _isNavigatingWithKeyboard;

        // Repeat navigation timing
        private float _nextRepeatTime;
        private bool _inputHeld;

        // Active group tracking
        private readonly Stack<UISelectableGroup> _groupStack = new Stack<UISelectableGroup>();
        private UISelectableGroup _currentGroup;

        // Default selectable fallback
        private Selectable _defaultSelectable;

        // When true, the next programmatic selection should not trigger a focus sound.
        private bool _suppressNextFocusSound;

        // Cache EventSystem reference
        private EventSystem _eventSystem;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the user is currently using keyboard/gamepad for navigation.
        /// </summary>
        public bool IsNavigatingWithKeyboard => _isNavigatingWithKeyboard;

        /// <summary>
        /// The currently active UISelectableGroup.
        /// </summary>
        public UISelectableGroup CurrentGroup => _currentGroup;

        /// <summary>
        /// When true the next programmatic selection should not play a focus sound.
        /// Reading this property will consume the flag (it is cleared on get).
        /// </summary>
        public bool SuppressNextFocusSound
        {
            get
            {
                bool value = _suppressNextFocusSound;
                _suppressNextFocusSound = false;
                return value;
            }
            set
            {
                _suppressNextFocusSound = value;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                _eventSystem = EventSystem.current;
                DisableNavigationEvents();
                return;
            }

            // Singleton setup
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);

            _eventSystem = EventSystem.current;
            _lastMousePosition = GetSafeMousePosition();
        }

        private void OnEnable()
        {
            _eventSystem = EventSystem.current;

            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                DisableNavigationEvents();
            }
            // Listen for scene changes to reset navigation groups when scenes change
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                // Clear any stale groups from previous scene
                ClearGroups();

                // If we loaded the Home scene, try to find and activate the Home group
                //if (scene.name == GameConstants.HomeScene)
                //{
                //    var groups = FindObjectsOfType<UISelectableGroup>(true);
                //    foreach (var g in groups)
                //    {
                //        if (g != null && g.IsHomeScreen && g.gameObject.activeInHierarchy)
                //        {
                //            // Push the home group as the current group
                //            PushGroup(g);
                //            break;
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UINavigationManager] OnSceneLoaded error: " + ex);
            }
        }

        private void Update()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                if (_eventSystem == null)
                {
                    _eventSystem = EventSystem.current;
                }

                DisableNavigationEvents();
                return;
            }

            if (_eventSystem == null)
            {
                _eventSystem = EventSystem.current;
                if (_eventSystem == null) return;
            }

            DetectInputModeSwitch();
            HandleTabCycling();
            HandleCancelInput();
            HandleAutoSelection();
        }

        #endregion

        /// <summary>
        /// Disables Unity navigation events when the custom navigation stack is not supported.
        /// </summary>
        private void DisableNavigationEvents()
        {
            if (_eventSystem != null)
            {
                _eventSystem.sendNavigationEvents = false;
            }

            _isNavigatingWithKeyboard = false;
        }

        #region Input Detection

        /// <summary>
        /// Detects whether user switched between mouse and keyboard/gamepad input.
        /// </summary>
        private void DetectInputModeSwitch()
        {
            // Check for mouse movement
            Vector2 currentMousePos = GetSafeMousePosition();
            if (Vector2.Distance(currentMousePos, _lastMousePosition) > 1f)
            {
                _lastMousePosition = currentMousePos;
                _lastMouseMoveTime = Time.unscaledTime;

                // Mouse moved - switch to mouse mode but keep selection if user prefers
                if (_isNavigatingWithKeyboard)
                {
                    _isNavigatingWithKeyboard = false;
                    OnSwitchedToMouseMode?.Invoke();
                    if (_debugMode)
                    {
                        Debug.Log("[UINavigationManager] Switched to mouse mode.");
                    }
                }
            }

            // Check for keyboard/gamepad navigation input
            Vector2 inputDir = GetNavigationInput();
            bool hasDirectionalInput = inputDir.sqrMagnitude > _inputThreshold * _inputThreshold;
            bool hasSubmitInput = GetSubmitInput();
            bool hasCancelInput = GetCancelInput();

            if (hasDirectionalInput || hasSubmitInput || hasCancelInput)
            {
                if (!_isNavigatingWithKeyboard)
                {
                    _isNavigatingWithKeyboard = true;
                    OnSwitchedToKeyboardMode?.Invoke();
                    if (_debugMode)
                    {
                        Debug.Log("[UINavigationManager] Switched to keyboard/gamepad mode.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current navigation input vector (supports WASD, arrows, and gamepad).
        /// </summary>
        private Vector2 GetNavigationInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            // Try legacy input axes
            try
            {
                horizontal = Input.GetAxisRaw(_horizontalAxis);
                vertical = Input.GetAxisRaw(_verticalAxis);
            }
            catch
            {
                // Axis not defined, use direct key checks
            }

            // Direct key checks for WASD/Arrows (works regardless of axis setup)
            if (Mathf.Approximately(horizontal, 0f))
            {
                if (SafeGetKey(KeyCode.LeftArrow) || SafeGetKey(KeyCode.A))
                    horizontal = -1f;
                else if (SafeGetKey(KeyCode.RightArrow) || SafeGetKey(KeyCode.D))
                    horizontal = 1f;
            }

            if (Mathf.Approximately(vertical, 0f))
            {
                if (SafeGetKey(KeyCode.DownArrow) || SafeGetKey(KeyCode.S))
                    vertical = -1f;
                else if (SafeGetKey(KeyCode.UpArrow) || SafeGetKey(KeyCode.W))
                    vertical = 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        /// <summary>
        /// Checks if submit button is pressed (Enter/Space/Gamepad A).
        /// </summary>
        private bool GetSubmitInput()
        {
            // Direct key checks
            // Direct key checks (safe across input systems)
            if (SafeGetKeyDown(KeyCode.Return) || SafeGetKeyDown(KeyCode.KeypadEnter) || SafeGetKeyDown(KeyCode.Space))
                return true;

            // Gamepad submit (A / South)
#if ENABLE_INPUT_SYSTEM
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && gp.buttonSouth.wasPressedThisFrame) return true;
#endif

            // Legacy input buttons
            foreach (var button in _submitButtons)
            {
                try
                {
                    if (SafeGetButtonDown(button))
                        return true;
                }
                catch
                {
                    // Button not defined
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if cancel button is pressed (Escape/Gamepad B).
        /// </summary>
        private bool GetCancelInput()
        {
            if (SafeGetKeyDown(KeyCode.Escape))
                return true;

#if ENABLE_INPUT_SYSTEM
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && gp.buttonEast.wasPressedThisFrame) return true; // B on gamepad
#endif

            foreach (var button in _cancelButtons)
            {
                try
                {
                    if (SafeGetButtonDown(button))
                        return true;
                }
                catch
                {
                    // Button not defined
                }
            }

            return false;
        }

        /// <summary>
        /// Handles cancel (Escape) input to close panels or request exit confirmation.
        /// </summary>
        private void HandleCancelInput()
        {
            if (!GetCancelInput()) return;
            // Dump stack for debugging every time Escape is pressed
            DumpGroupStack();
            // Prefer to close the top-most push popup if present
            var topPush = FindTopMostPushGroup();
            if (topPush != null)
            {
                // Try custom back handler on the popup first
                if (!TryInvokeBackHandler(topPush))
                {
                    // No handler handled it — deactivate the popup; its OnDisable should pop it if it auto-registered
                    topPush.gameObject.SetActive(false);
                }
                if (_debugMode)
                    Debug.Log($"[UINavigationManager] Closed top push group: {topPush.name}");
                return;
            }

            // If there is an active current group, pop it (closes panels in stack order)
            if (_currentGroup != null)
            {
                // If we're on the home screen with no previous group, request exit confirmation
                if (_currentGroup.IsHomeScreen && _groupStack.Count == 0)
                {
                    OnRequestExitConfirmation?.Invoke();
                    TryShowExitConfirmation();
                    if (_debugMode)
                        Debug.Log("[UINavigationManager] Requested exit confirmation.");
                    return;
                }

                // Try custom back handler on the current group first
                if (!TryInvokeBackHandler(_currentGroup))
                {
                    PopGroup();
                }
                if (_debugMode)
                    Debug.Log("[UINavigationManager] Closed current group via Escape.");
                return;
            }

            // Nothing to close: request exit confirmation
            OnRequestExitConfirmation?.Invoke();
            TryShowExitConfirmation();
            if (_debugMode)
                Debug.Log("[UINavigationManager] Requested exit confirmation (no active group).");
        }

        /// <summary>
        /// Safely attempts to show the global confirmation popup when the player requests exit.
        /// </summary>
        private void TryShowExitConfirmation()
        {
            try
            {
                // Call the popup if available. Using try/catch in case the type or instance is not present.
    //            if (ConfirmationPopup.Instance != null)
    //            {
    //                ConfirmationPopup.Instance.Show("Quit Game", "Leaving so soon?",
    //                    () => {  
    //#if UNITY_EDITOR
    //                        UnityEditor.EditorApplication.isPlaying = false;
    //#else
    //                        Application.Quit();
    //#endif
    //                    },
    //                    () => { 
    //                        // Optional cancel action
    //                    });
    //            }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UINavigationManager] Couldn't show exit confirmation popup: " + ex);
            }
        }

        /// <summary>
        /// Prints the current navigation stack and current group to the log for debugging.
        /// </summary>
        private void DumpGroupStack()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("[UINavigationManager] === Group Stack Dump ===");
                sb.AppendLine($"CurrentGroup: {(_currentGroup != null ? _currentGroup.name : "<none>")}");
                sb.AppendLine("Stack (top -> bottom):");
                int idx = 0;
                foreach (var g in _groupStack)
                {
                    idx++;
                    if (g == null)
                        sb.AppendLine($" {idx}: <null>");
                    else
                        sb.AppendLine($" {idx}: {g.name} (IsPush:{g.IsPushGroup}, IsHome:{g.IsHomeScreen})");
                }
                if (idx == 0) sb.AppendLine(" <empty>");
                Debug.Log(sb.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UINavigationManager] Failed to dump group stack: " + ex);
            }
        }

        /// <summary>
        /// Finds the top-most active UISelectableGroup marked as a push (popup).
        /// Uses Canvas.sortingOrder first, then scene root sibling index as tiebreaker.
        /// </summary>
        private UISelectableGroup FindTopMostPushGroup()
        {
            UISelectableGroup best = null;
            int bestOrder = int.MinValue;
            int bestRootIndex = int.MinValue;

            // FindObjectsOfType with 'true' returns inactive on newer Unity; filter activeInHierarchy.
            var groups = FindObjectsOfType<UISelectableGroup>(true);
            foreach (var g in groups)
            {
                if (g == null) continue;
                if (!g.gameObject.activeInHierarchy) continue;
                if (!g.IsPushGroup) continue;

                int order = GetCanvasSortingOrder(g);
                int rootIndex = g.transform.root.GetSiblingIndex();

                // prefer higher sorting order, then higher root sibling index
                if (order > bestOrder || (order == bestOrder && rootIndex > bestRootIndex))
                {
                    best = g;
                    bestOrder = order;
                    bestRootIndex = rootIndex;
                }
            }

            return best;
        }

        private int GetCanvasSortingOrder(UISelectableGroup g)
        {
            if (g == null) return 0;
            var canvas = g.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.sortingOrder : 0;
        }

        /// <summary>
        /// Attempts to invoke a back handler on the provided group.
        /// Returns true if a handler was found and it handled the back action.
        /// </summary>
        private bool TryInvokeBackHandler(UISelectableGroup group)
        {
            if (group == null) return false;

            // Search components on the group and its children first
            var childBehaviours = group.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true);
            foreach (var mb in childBehaviours)
            {
                if (mb is IBackHandler bh)
                {
                    try
                    {
                        if (bh.OnBackHandled()) return true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[UINavigationManager] Back handler threw: {ex}");
                    }
                }
            }

            // Next, search parents (useful when manager/owner lives above the group)
            var parentBehaviours = group.GetComponentsInParent<UnityEngine.MonoBehaviour>(true);
            foreach (var mb in parentBehaviours)
            {
                if (mb is IBackHandler bh)
                {
                    try
                    {
                        if (bh.OnBackHandled()) return true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[UINavigationManager] Back handler threw: {ex}");
                    }
                }
            }

            return false;
        }

        #endregion

        #region Auto Selection

        /// <summary>
        /// Handles automatic selection of UI elements when using keyboard/gamepad.
        /// </summary>
        private void HandleAutoSelection()
        {
            if (!_autoSelectOnInput) return;
            if (!_isNavigatingWithKeyboard) return;

            // Check if we need to select something
            GameObject currentSelected = _eventSystem.currentSelectedGameObject;
            
            // If something is selected, verify it's still valid
            if (currentSelected != null)
            {
                var selectable = currentSelected.GetComponent<Selectable>();
                if (selectable != null && selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
                {
                    return; // Valid selection exists
                }
            }

            // Need to select something - use current group's default or global default
            Selectable toSelect = null;

            if (_currentGroup != null)
            {
                toSelect = _currentGroup.GetDefaultSelectable();
            }

            if (toSelect == null)
            {
                toSelect = _defaultSelectable;
            }

            if (toSelect == null)
            {
                // Find any valid selectable as last resort
                toSelect = FindFirstValidSelectable();
            }

            if (toSelect != null && toSelect.IsInteractable() && toSelect.gameObject.activeInHierarchy)
            {
                SelectUIElement(toSelect);
            }
        }

        /// <summary>
        /// Finds the first valid interactable selectable in the scene.
        /// </summary>
        private Selectable FindFirstValidSelectable()
        {
            var allSelectables = Selectable.allSelectablesArray;
            foreach (var selectable in allSelectables)
            {
                if (selectable != null && 
                    selectable.IsInteractable() && 
                    selectable.gameObject.activeInHierarchy &&
                    selectable.navigation.mode != UnityEngine.UI.Navigation.Mode.None)
                {
                    return selectable;
                }
            }
            return null;
        }

        #endregion

        #region Tab Cycling

        /// <summary>
        /// Handles Tab key to cycle between sub-groups within the current group.
        /// </summary>
        private void HandleTabCycling()
        {
            // Check for Tab key press
            bool tabPressed = CustomUI.Navigation.InputUtils.SafeGetKeyDown(KeyCode.Tab);
            bool shiftHeld = CustomUI.Navigation.InputUtils.SafeGetKey(KeyCode.LeftShift) || CustomUI.Navigation.InputUtils.SafeGetKey(KeyCode.RightShift);

            // Also check for gamepad shoulder buttons (LB/RB) for sub-group cycling
            bool lbPressed = CustomUI.Navigation.InputUtils.SafeGetKeyDown(KeyCode.JoystickButton4);
            bool rbPressed = CustomUI.Navigation.InputUtils.SafeGetKeyDown(KeyCode.JoystickButton5);

            if (!tabPressed && !lbPressed && !rbPressed) return;

            // Ensure we're in keyboard mode
            if (!_isNavigatingWithKeyboard)
            {
                _isNavigatingWithKeyboard = true;
            }

            // Check if current group has sub-groups
            if (_currentGroup == null || !_currentGroup.HasSubGroups) return;

            // Determine direction: Shift+Tab or LB = reverse, Tab or RB = forward
            bool reverse = (tabPressed && shiftHeld) || lbPressed;

            // Cycle to next/previous sub-group
            Selectable newSelection = _currentGroup.CycleToNextSubGroup(reverse);

            if (newSelection != null)
            {
                // This selection comes from explicit user input (Tab/LB/RB). Play focus sound.
                SelectUIElement(newSelection);

                if (_debugMode)
                {
                    Debug.Log($"[UINavigationManager] Tab cycled to sub-group {_currentGroup.CurrentSubGroupIndex}, selected: {newSelection.name}");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the global default selectable to fall back to when no group is active.
        /// </summary>
        /// <param name="selectable">The selectable to use as default.</param>
        public void SetDefaultSelectable(Selectable selectable)
        {
            _defaultSelectable = selectable;
        }

        /// <summary>
        /// Pushes a UISelectableGroup onto the stack and makes it active.
        /// Use this when opening a popup or new menu.
        /// </summary>
        /// <param name="group">The group to activate.</param>
        public void PushGroup(UISelectableGroup group)
        {
            if (group == null) return;

            // If this group is already the current group, do nothing.
            if (_currentGroup == group) return;

            if (_currentGroup != null)
            {
                _groupStack.Push(_currentGroup);
            }

            _currentGroup = group;

            if (_isNavigatingWithKeyboard)
            {
                var defaultSelectable = group.GetDefaultSelectable();
                if (defaultSelectable != null)
                {
                    SelectUIElement(defaultSelectable);
                }
            }

            if (_debugMode)
            {
                Debug.Log($"[UINavigationManager] Pushed group: {group.name}");
            }
        }

        /// <summary>
        /// Pops the current group and restores the previous one.
        /// Use this when closing a popup or returning to previous menu.
        /// </summary>
        public void PopGroup()
        {
            if (_groupStack.Count > 0)
            {
                _currentGroup = _groupStack.Pop();

                if (_isNavigatingWithKeyboard && _currentGroup != null)
                {
                    var defaultSelectable = _currentGroup.GetDefaultSelectable();
                    if (defaultSelectable != null)
                    {
                        SelectUIElement(defaultSelectable);
                    }
                }

                if (_debugMode)
                {
                    Debug.Log($"[UINavigationManager] Popped to group: {(_currentGroup != null ? _currentGroup.name : "none")}");
                }
            }
            else
            {
                _currentGroup = null;
            }
        }

        /// <summary>
        /// Clears the group stack and current group.
        /// </summary>
        public void ClearGroups()
        {
            _groupStack.Clear();
            _currentGroup = null;

            if (_debugMode)
            {
                Debug.Log("[UINavigationManager] Cleared all groups.");
            }
        }

        /// <summary>
        /// Programmatically selects a UI element.
        /// </summary>
        /// <param name="selectable">The selectable to select.</param>
        public void SelectUIElement(Selectable selectable, bool suppressFocusSound = false)
        {
            if (selectable == null) return;
            if (_eventSystem == null) return;

            // If caller requested suppression, mark it so UIFocusVisualizer can consume it.
            if (suppressFocusSound)
            {
                _suppressNextFocusSound = true;
            }

            _eventSystem.SetSelectedGameObject(selectable.gameObject);

            if (_debugMode)
            {
                Debug.Log($"[UINavigationManager] Selected: {selectable.name}");
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearSelection()
        {
            if (_eventSystem != null)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// Forces the navigation manager into keyboard mode.
        /// Useful when you want to ensure keyboard navigation is active.
        /// </summary>
        public void ForceKeyboardMode()
        {
            _isNavigatingWithKeyboard = true;
        }

        /// <summary>
        /// Forces the navigation manager into mouse mode.
        /// </summary>
        public void ForceMouseMode()
        {
            _isNavigatingWithKeyboard = false;
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!_debugMode) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Input Mode: {(_isNavigatingWithKeyboard ? "Keyboard/Gamepad" : "Mouse")}");
            GUILayout.Label($"Current Selection: {(_eventSystem?.currentSelectedGameObject?.name ?? "None")}");
            GUILayout.Label($"Current Group: {(_currentGroup?.name ?? "None")}");
            GUILayout.Label($"Group Stack Count: {_groupStack.Count}");
            GUILayout.EndArea();
        }

        #endregion
    }
}
