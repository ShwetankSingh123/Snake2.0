using UnityEngine;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Optional component to customize how UISelectionHighlighter highlights a specific element.
    /// Attach this to UI elements that need custom padding, size, or behavior.
    /// 
    /// If not present, the highlighter uses its default settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISelectableTarget : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Custom Padding")]
        [Tooltip("If true, uses custom padding instead of the highlighter's default.")]
        [SerializeField] private bool _useCustomPadding = false;

        [Tooltip("Custom padding around this element (left, bottom, right, top).")]
        [SerializeField] private Vector4 _customPadding = new Vector4(8f, 8f, 8f, 8f);

        [Header("Custom Minimum Size")]
        [Tooltip("If true, uses custom minimum size instead of the highlighter's default.")]
        [SerializeField] private bool _useCustomMinSize = false;

        [Tooltip("Custom minimum size for the highlight when on this element.")]
        [SerializeField] private Vector2 _customMinSize = new Vector2(50f, 30f);

        [Header("Target Override")]
        [Tooltip("If set, highlight will use this RectTransform's bounds instead of this object's.")]
        [SerializeField] private RectTransform _targetOverride;

        [Header("Behavior")]
        [Tooltip("If true, this element will be skipped by the highlighter (remains invisible).")]
        [SerializeField] private bool _skipHighlight = false;

        [Tooltip("If true, pulse animation is disabled for this element.")]
        [SerializeField] private bool _disablePulse = false;

        [Header("Animation Override")]
        [Tooltip("If true, uses custom animation speeds for this element.")]
        [SerializeField] private bool _useCustomAnimation = false;

        [Tooltip("Custom move duration for this element.")]
        [SerializeField] private float _customMoveDuration = 0.15f;

        [Tooltip("Custom resize duration for this element.")]
        [SerializeField] private float _customResizeDuration = 0.12f;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this element has custom padding defined.
        /// </summary>
        public bool HasCustomPadding => _useCustomPadding;

        /// <summary>
        /// Custom padding for this element (left, bottom, right, top).
        /// </summary>
        public Vector4 Padding => _customPadding;

        /// <summary>
        /// Whether this element has a custom minimum size defined.
        /// </summary>
        public bool HasCustomMinSize => _useCustomMinSize;

        /// <summary>
        /// Custom minimum size for the highlight on this element.
        /// </summary>
        public Vector2 MinimumSize => _customMinSize;

        /// <summary>
        /// Alternative RectTransform to use for bounds calculation.
        /// </summary>
        public RectTransform TargetOverride => _targetOverride;

        /// <summary>
        /// If true, this element should not be highlighted.
        /// </summary>
        public bool SkipHighlight => _skipHighlight;

        /// <summary>
        /// If true, pulse animation should be disabled for this element.
        /// </summary>
        public bool DisablePulse => _disablePulse;

        /// <summary>
        /// Whether this element has custom animation settings.
        /// </summary>
        public bool HasCustomAnimation => _useCustomAnimation;

        /// <summary>
        /// Custom move duration for this element.
        /// </summary>
        public float MoveDuration => _customMoveDuration;

        /// <summary>
        /// Custom resize duration for this element.
        /// </summary>
        public float ResizeDuration => _customResizeDuration;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets custom padding at runtime.
        /// </summary>
        /// <param name="padding">Padding values (left, bottom, right, top).</param>
        public void SetPadding(Vector4 padding)
        {
            _useCustomPadding = true;
            _customPadding = padding;

            // Notify highlighter to refresh if this is currently selected
            RefreshHighlighterIfSelected();
        }

        /// <summary>
        /// Sets custom padding at runtime using uniform value.
        /// </summary>
        /// <param name="uniformPadding">Padding value for all sides.</param>
        public void SetPadding(float uniformPadding)
        {
            SetPadding(new Vector4(uniformPadding, uniformPadding, uniformPadding, uniformPadding));
        }

        /// <summary>
        /// Sets custom minimum size at runtime.
        /// </summary>
        /// <param name="minSize">Minimum size for the highlight.</param>
        public void SetMinimumSize(Vector2 minSize)
        {
            _useCustomMinSize = true;
            _customMinSize = minSize;

            RefreshHighlighterIfSelected();
        }

        /// <summary>
        /// Clears custom padding, reverting to highlighter defaults.
        /// </summary>
        public void ClearCustomPadding()
        {
            _useCustomPadding = false;
            RefreshHighlighterIfSelected();
        }

        /// <summary>
        /// Clears custom minimum size, reverting to highlighter defaults.
        /// </summary>
        public void ClearCustomMinSize()
        {
            _useCustomMinSize = false;
            RefreshHighlighterIfSelected();
        }

        /// <summary>
        /// Sets the target override at runtime.
        /// </summary>
        /// <param name="target">RectTransform to use for bounds, or null to use this object.</param>
        public void SetTargetOverride(RectTransform target)
        {
            _targetOverride = target;
            RefreshHighlighterIfSelected();
        }

        /// <summary>
        /// Sets whether to skip highlighting this element.
        /// </summary>
        /// <param name="skip">True to skip, false to allow highlighting.</param>
        public void SetSkipHighlight(bool skip)
        {
            _skipHighlight = skip;

            if (skip)
            {
                // If currently selected, hide highlight
                if (UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject == gameObject)
                {
                    UISelectionHighlighter.Instance?.RefreshHighlight();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Tells the highlighter to refresh if this element is currently selected.
        /// </summary>
        private void RefreshHighlighterIfSelected()
        {
            if (UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject == gameObject)
            {
                UISelectionHighlighter.Instance?.RefreshHighlight();
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp padding to non-negative values
            _customPadding.x = Mathf.Max(0f, _customPadding.x);
            _customPadding.y = Mathf.Max(0f, _customPadding.y);
            _customPadding.z = Mathf.Max(0f, _customPadding.z);
            _customPadding.w = Mathf.Max(0f, _customPadding.w);

            // Clamp minimum size
            _customMinSize.x = Mathf.Max(0f, _customMinSize.x);
            _customMinSize.y = Mathf.Max(0f, _customMinSize.y);

            // Clamp animation durations
            _customMoveDuration = Mathf.Max(0.01f, _customMoveDuration);
            _customResizeDuration = Mathf.Max(0.01f, _customResizeDuration);
        }
#endif

        #endregion
    }
}
