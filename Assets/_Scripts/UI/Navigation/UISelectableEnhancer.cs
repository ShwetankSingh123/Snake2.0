using CustomUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Enhances a Selectable with keyboard/gamepad submit handling.
    /// This ensures that Enter/Space triggers the button's click event.
    /// Attach to selectables that need explicit submit handling or have custom click logic.
    /// Works with CustomButton and standard Unity Button/Toggle/etc.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UISelectableEnhancer : MonoBehaviour, ISubmitHandler, ICancelHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields

        [Header("Submit Handling")]
        [Tooltip("If true, Enter/Space will trigger the button's OnClick event.")]
        [SerializeField] private bool _handleSubmit = true;

        [Header("Cancel Handling")]
        [Tooltip("If true, Escape/B button will invoke the cancel action.")]
        [SerializeField] private bool _handleCancel = false;

        [Tooltip("The button to click when cancel is pressed (optional, e.g., a close/back button).")]
        [SerializeField] private Button _cancelTarget;

        [Header("Mouse Interaction")]
        [Tooltip("If true, mouse hover will select this element (for hybrid input).")]
        [SerializeField] private bool _selectOnHover = false;

        [Tooltip("Delay before selecting on hover (to prevent accidental selections).")]
        [SerializeField] private float _hoverSelectDelay = 0.1f;

        [Header("Sound")]
        [Tooltip("If true, plays a sound on submit.")]
        [SerializeField] private bool _playSubmitSound = true;

        #endregion

        #region Private Fields

        // Cached components
        private Selectable _selectable;
        private Button _button;
        private Toggle _toggle;
        private CustomButton _customButton;

        // Hover state
        private float _hoverStartTime;
        private bool _isHovering;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _button = GetComponent<Button>();
            _toggle = GetComponent<Toggle>();
            _customButton = GetComponent<CustomButton>();
        }

        private void OnEnable()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                _isHovering = false;
                enabled = false;
            }
        }

        private void Update()
        {
            // Handle delayed hover selection
            if (_selectOnHover && _isHovering)
            {
                if (Time.unscaledTime - _hoverStartTime >= _hoverSelectDelay)
                {
                    SelectThis();
                    _isHovering = false;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when submit action is pressed (Enter/Space/Gamepad A).
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            if (!_handleSubmit) return;
            if (!_selectable.IsInteractable()) return;

            // Handle CustomButton (which has custom click logic)
            if (_customButton != null)
            {
                // CustomButton uses onButtonClick, invoke it directly
                _customButton.onButtonClick?.Invoke();
                _customButton.onRelease?.Invoke();

                if (_playSubmitSound)
                {
                    // Preferred: use modular provider
                    try { CustomUI.Navigation.Providers.UIProviderLocator.Audio.PlayUIButtonClick(); }
                    catch { /* swallow provider errors */ }

                    //AudioManager.Instance.PlayUIButtonSound();
                }
                return;
            }

            // Handle standard Button
            if (_button != null)
            {
                _button.onClick?.Invoke();

                if (_playSubmitSound)
                {
                    try { CustomUI.Navigation.Providers.UIProviderLocator.Audio.PlayUIButtonClick(); }
                    catch { }
                    //AudioManager.Instance.PlayUIButtonSound();
                }
                return;
            }

            // Handle Toggle
            if (_toggle != null)
            {
                _toggle.isOn = !_toggle.isOn;

                if (_playSubmitSound)
                {
                    try { CustomUI.Navigation.Providers.UIProviderLocator.Audio.PlayUIButtonClick(); }
                    catch { }
                    //AudioManager.Instance.PlayUIButtonSound();
                }
                return;
            }

            // For other selectables (Slider, Dropdown, InputField), the default behavior is usually fine
        }

        /// <summary>
        /// Called when cancel action is pressed (Escape/Gamepad B).
        /// </summary>
        public void OnCancel(BaseEventData eventData)
        {
            if (!_handleCancel) return;

            if (_cancelTarget != null)
            {
                _cancelTarget.onClick?.Invoke();
            }
        }

        /// <summary>
        /// Called when pointer enters this element.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectOnHover)
            {
                _isHovering = true;
                _hoverStartTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Called when pointer exits this element.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Selects this element via EventSystem.
        /// </summary>
        private void SelectThis()
        {
            if (_selectable != null && _selectable.IsInteractable())
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        #endregion

        #region Static Utility Methods

        /// <summary>
        /// Adds UISelectableEnhancer to all selectables in a hierarchy that don't already have it.
        /// Call this from a manager script to auto-enhance UI elements.
        /// </summary>
        /// <param name="root">The root transform to search from.</param>
        /// <param name="handleSubmit">Whether to handle submit events.</param>
        /// <returns>Number of enhancers added.</returns>
        public static int EnhanceAllSelectables(Transform root, bool handleSubmit = true)
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                return 0;
            }

            int count = 0;
            var selectables = root.GetComponentsInChildren<Selectable>(true);

            foreach (var selectable in selectables)
            {
                if (selectable.GetComponent<UISelectableEnhancer>() == null)
                {
                    var enhancer = selectable.gameObject.AddComponent<UISelectableEnhancer>();
                    enhancer._handleSubmit = handleSubmit;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Adds UIFocusVisualizer to all selectables in a hierarchy that don't already have it.
        /// </summary>
        /// <param name="root">The root transform to search from.</param>
        /// <param name="effectType">The focus effect type to use.</param>
        /// <returns>Number of visualizers added.</returns>
        public static int AddFocusVisualizersToAll(Transform root)
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                return 0;
            }

            int count = 0;
            var selectables = root.GetComponentsInChildren<Selectable>(true);

            foreach (var selectable in selectables)
            {
                if (selectable.GetComponent<UIFocusVisualizer>() == null)
                {
                    selectable.gameObject.AddComponent<UIFocusVisualizer>();
                    count++;
                }
            }

            return count;
        }

        #endregion
    }
}
