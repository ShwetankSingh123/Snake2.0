using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Helper component that configures the EventSystem's InputModule for proper keyboard/gamepad support.
    /// Attach to the EventSystem GameObject or let it auto-create the EventSystem if missing.
    /// Supports both Legacy Input Manager and new Input System.
    /// </summary>
    public class UIInputModuleSetup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Configuration")]
        [Tooltip("If true, auto-creates EventSystem if missing in scene.")]
        [SerializeField] private bool _autoCreateEventSystem = true;

        [Tooltip("Axes for horizontal navigation.")]
        [SerializeField] private string _horizontalAxis = "Horizontal";

        [Tooltip("Axes for vertical navigation.")]
        [SerializeField] private string _verticalAxis = "Vertical";

        [Tooltip("Button for submit action (Enter/Space/Gamepad A).")]
        [SerializeField] private string _submitButton = "Submit";

        [Tooltip("Button for cancel action (Escape/Gamepad B).")]
        [SerializeField] private string _cancelButton = "Cancel";

        [Header("Navigation Settings")]
        [Tooltip("Delay before input repeats when held.")]
        [SerializeField] private float _inputActionsPerSecond = 10f;

        [Tooltip("Delay before repeat starts.")]
        [SerializeField] private float _repeatDelay = 0.5f;

        [Tooltip("Allow scrolling with gamepad/arrow keys in scroll views.")]
        [SerializeField] private bool _sendNavigationEvents = true;

        [Header("Focus Settings")]
        [Tooltip("If true, allows UI to be deselected when clicking outside.")]
        [SerializeField] private bool _deselectOnBackgroundClick = false;

        #endregion

        #region Private Fields

        private EventSystem _eventSystem;
        private StandaloneInputModule _standaloneModule;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupEventSystem();
        }

        private void Start()
        {
            // Verify setup after all Awake calls
            VerifySetup();
        }

        #endregion

        #region Setup Methods

        /// <summary>
        /// Sets up the EventSystem and InputModule for keyboard/gamepad navigation.
        /// </summary>
        private void SetupEventSystem()
        {
            // Find or create EventSystem
            _eventSystem = EventSystem.current;

            if (_eventSystem == null)
            {
#if UNITY_2023_1_OR_NEWER
                _eventSystem = FindFirstObjectByType<EventSystem>();
#else
                _eventSystem = FindObjectOfType<EventSystem>();
#endif
            }

            if (_eventSystem == null && _autoCreateEventSystem)
            {
                Debug.Log("[UIInputModuleSetup] No EventSystem found. Creating one.");
                var eventSystemGO = new GameObject("EventSystem");
                _eventSystem = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            if (_eventSystem == null)
            {
                Debug.LogError("[UIInputModuleSetup] No EventSystem in scene! UI navigation will not work.");
                return;
            }

            if (!ApplyPlatformGate())
            {
                return;
            }

            // Configure the StandaloneInputModule
            _standaloneModule = _eventSystem.GetComponent<StandaloneInputModule>();

            if (_standaloneModule == null)
            {
                // Check for new Input System module
                var inputSystemModule = _eventSystem.GetComponent<BaseInputModule>();
                if (inputSystemModule != null)
                {
                    Debug.Log("[UIInputModuleSetup] Found InputSystem UI module. Configuration handled by Input System.");
                    return;
                }

                // Add StandaloneInputModule if missing
                _standaloneModule = _eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            ConfigureStandaloneModule();
        }

        /// <summary>
        /// Configures the StandaloneInputModule with proper settings for keyboard/gamepad.
        /// </summary>
        private void ConfigureStandaloneModule()
        {
            if (_standaloneModule == null) return;

            // Configure axes
            _standaloneModule.horizontalAxis = _horizontalAxis;
            _standaloneModule.verticalAxis = _verticalAxis;
            _standaloneModule.submitButton = _submitButton;
            _standaloneModule.cancelButton = _cancelButton;

            // Configure timing
            _standaloneModule.inputActionsPerSecond = _inputActionsPerSecond;
            _standaloneModule.repeatDelay = _repeatDelay;

            // Note: sendNavigationEvents is handled by EventSystem, not StandaloneInputModule
            // The EventSystem already handles navigation events by default
            _eventSystem.sendNavigationEvents = _sendNavigationEvents;

            Debug.Log("[UIInputModuleSetup] Configured StandaloneInputModule for keyboard/gamepad navigation.");
        }

        /// <summary>
        /// Verifies the setup is correct.
        /// </summary>
        private void VerifySetup()
        {
            if (_eventSystem == null)
            {
                Debug.LogError("[UIInputModuleSetup] EventSystem is missing! Add one to your scene.");
                return;
            }

            var inputModule = _eventSystem.currentInputModule;
            if (inputModule == null)
            {
                Debug.LogWarning("[UIInputModuleSetup] No active InputModule. Navigation may not work.");
                return;
            }

            Debug.Log($"[UIInputModuleSetup] Setup complete. Using: {inputModule.GetType().Name}");
        }

        #endregion

        /// <summary>
        /// Applies the Windows-only platform gate to the EventSystem navigation events.
        /// </summary>
        private bool ApplyPlatformGate()
        {
            bool navigationSupported = UINavigationPlatformUtility.IsNavigationSupported;
            _eventSystem.sendNavigationEvents = navigationSupported && _sendNavigationEvents;

            if (!navigationSupported)
            {
                Debug.Log("[UIInputModuleSetup] UI navigation disabled because the current platform/build target is not Windows.");
                enabled = false;
            }

            return navigationSupported;
        }

        #region Public API

        /// <summary>
        /// Forces reconfiguration of the input module.
        /// Call after changing input settings at runtime.
        /// </summary>
        public void Reconfigure()
        {
            SetupEventSystem();
        }

        /// <summary>
        /// Checks if the system is properly configured for keyboard/gamepad input.
        /// </summary>
        public bool IsConfigured()
        {
            return _eventSystem != null && _eventSystem.currentInputModule != null;
        }

        #endregion

        #region Static Utility

        /// <summary>
        /// Ensures an EventSystem exists in the scene.
        /// Call from any script that needs UI input.
        /// </summary>
        public static EventSystem EnsureEventSystemExists()
        {
            var eventSystem = EventSystem.current;
            
            if (eventSystem == null)
            {
#if UNITY_2023_1_OR_NEWER
                eventSystem = FindFirstObjectByType<EventSystem>();
#else
                eventSystem = Object.FindObjectOfType<EventSystem>();
#endif
            }

            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
                Debug.Log("[UIInputModuleSetup] Created EventSystem.");
            }

            eventSystem.sendNavigationEvents = UINavigationPlatformUtility.IsNavigationSupported;

            return eventSystem;
        }

        #endregion
    }
}
