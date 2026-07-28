using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
//using Coffee.UIEffects;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Provides visual feedback when a UI element is focused via keyboard/gamepad or mouse hover.
    /// Attach this to any Selectable (Button, Toggle, Slider, etc.) for custom highlight effects.
    /// Works alongside Unity's built-in highlight colors.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIFocusVisualizer : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields

        [Header("Visual Effects")]

        [SerializeField] private bool _useScale = true;

        [SerializeField] private bool _useOutlineImage = true;

        [SerializeField] private bool _useUIEffectOutline = false;

        [SerializeField] private bool _useColorTint = false;

        [SerializeField] private bool _useGlow = false;


        [Header("Scale Effect Settings")]
        [Tooltip("Scale multiplier when focused (1.0 = no change).")]
        [SerializeField] private float _focusScale = 1.08f;

        [Tooltip("Duration of the scale animation.")]
        [SerializeField] private float _scaleDuration = 0.15f;

        [Header("Outline Effect Settings")]
        [Tooltip("Optional outline image to show when focused.")]
        [SerializeField] private Image _outlineImage;

        [Tooltip("Color of the outline when focused.")]
        [SerializeField] private Color _outlineColor = Color.white;

        [Tooltip("Outline fade duration.")]
        [SerializeField] private float _outlineFadeDuration = 0.1f;

        [Header("Outline Animation")]
        [Tooltip("If true, pulses the outline alpha for a breathing effect.")]
        [SerializeField]
        private bool _pulseOutlineAlpha = true;

        [Tooltip("If true, pulses the outline color for a breathing effect.")]
        [SerializeField]
        private bool _pulseOutlineColor = false;
        [SerializeField] private float _outlineHoldDuration = 0.3f;

        [SerializeField] private float _outlineFadeOutDuration = 0.1f;

        [SerializeField] private float _outlineFadeInDuration = 0.6f;

        [Tooltip("Minimum alpha for outline when pulsing.")]
        [SerializeField]
        private float _outlineMinAlpha = 0.5f;

        [Tooltip("First color for outline pulsing.")]
        [SerializeField]
        private Color _outlinePulseColorA = Color.white;

        [Tooltip("Second color for outline pulsing.")]
        [SerializeField]
        private Color _outlinePulseColorB = Color.yellow;

        [Header("Outline Effect with UIEffect")]
        [Tooltip("If true, uses a UIEffect component for outline instead of an Image.")]
        [SerializeField] private bool _useUIEffectForOutline = false;

        [Tooltip("Optional image that contains the UIEffect component. If empty, the UIEffect on this GameObject is used.")]
        [SerializeField] private Graphic _uiEffectTargetGraphic;
        
        //[SerializeField] private UIEffect _uiEffectOutline; // Only used if _useUIEffectForOutline is true

        [Header("Color Tint Effect Settings")]
        [SerializeField] private Color _tintColorA = Color.white;

        [SerializeField] private Color _tintColorB = Color.yellow;

        [SerializeField] private float _colorTintDuration = 0.5f;

        [Header("Glow Effect Settings")]
        [Tooltip("Optional CanvasGroup for glow effect.")]
        [SerializeField] private CanvasGroup _glowCanvasGroup;

        [Tooltip("Alpha value for glow when focused.")]
        [SerializeField] private float _glowAlpha = 1f;

        [Header("Audio")]
        [Tooltip("If true, plays a sound when focused.")]
        [SerializeField] private bool _playFocusSound = true;

        [Header("Mouse Hover")]
        [Tooltip("If true, shows focus effect when mouse hovers over this element.")]
        [SerializeField] private bool _showOnMouseHover = true;

        [Header("Advanced")]
        [Tooltip("If true, the focus effect only shows in keyboard/gamepad mode (ignored if Show On Mouse Hover is enabled).")]
        [SerializeField] private bool _onlyShowInKeyboardMode = false;

        [Tooltip("Tween ease type for animations.")]
        [SerializeField] private Ease _easeType = Ease.OutQuad;

        #endregion

        #region Private Fields

        // Cached components
        private Selectable _selectable;
        private RectTransform _rectTransform;

        // Original state
        private Vector3 _originalScale;
        private Color _originalOutlineColor;

        // Tween management
        private Tweener _scaleTween;
        private Tweener _outlineTween;
        private Tweener _glowTween;

        // State tracking
        private bool _isFocused;
        private bool _isHovered;

        private Color _originalGraphicColor;
        private Tweener _colorTintTween;
        private Graphic _targetGraphic;

        private Tween _outlinePulseTween;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;

            if (_outlineImage != null)
            {
                _originalOutlineColor = _outlineImage.color;
                _outlineImage.gameObject.SetActive(false);
            }

            //if (_uiEffectOutline != null)
            //{
            //    _uiEffectOutline.enabled = false;
            //}

            if (_glowCanvasGroup != null)
            {
                _glowCanvasGroup.alpha = 0f;
            }

            _targetGraphic = _selectable.targetGraphic;

            if (_targetGraphic != null)
            {
                _originalGraphicColor = _targetGraphic.color;
            }

            //if (_uiEffectOutline == null)
            //{
            //    if (_uiEffectTargetGraphic != null)
            //    {
            //        _uiEffectOutline =
            //            _uiEffectTargetGraphic.GetComponent<UIEffect>();
            //    }
            //    else
            //    {
            //        _uiEffectOutline =
            //            GetComponent<UIEffect>();
            //    }
            //}
        }

        private void OnEnable()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                KillTweens();
                ResetVisuals();
                enabled = false;
                return;
            }

            // Subscribe to input mode changes
            UINavigationManager.OnSwitchedToKeyboardMode += OnSwitchedToKeyboardMode;
            UINavigationManager.OnSwitchedToMouseMode += OnSwitchedToMouseMode;

            // Reset to original state
            transform.localScale = _originalScale;

            if (_outlineImage != null)
            {
                _outlineImage.gameObject.SetActive(false);
            }

            //if (_uiEffectOutline != null)
            //{
            //    _uiEffectOutline.enabled = false;
            //}

            if (_glowCanvasGroup != null)
            {
                _glowCanvasGroup.alpha = 0f;
            }

            _isFocused = false;
            _isHovered = false;
        }

        private void OnDisable()
        {
            // Unsubscribe from input mode changes
            UINavigationManager.OnSwitchedToKeyboardMode -= OnSwitchedToKeyboardMode;
            UINavigationManager.OnSwitchedToMouseMode -= OnSwitchedToMouseMode;

            KillTweens();
            ResetVisuals();
        }

        private void OnDestroy()
        {
            // Ensure unsubscribe on destroy as well (for static event safety)
            UINavigationManager.OnSwitchedToKeyboardMode -= OnSwitchedToKeyboardMode;
            UINavigationManager.OnSwitchedToMouseMode -= OnSwitchedToMouseMode;
        }

        /// <summary>
        /// Called when input mode switches to keyboard/gamepad.
        /// Clears hover state to prevent multiple highlights.
        /// </summary>
        private void OnSwitchedToKeyboardMode()
        {
            // Clear hover highlight when switching to keyboard mode
            if (_isHovered)
            {
                _isHovered = false;
                
                // Only hide if this element is not the one being keyboard-selected
                if (!_isFocused)
                {
                    HideFocusEffect();
                }
            }
        }

        /// <summary>
        /// Called when input mode switches to mouse.
        /// Clears keyboard focus highlight to prevent multiple highlights.
        /// </summary>
        private void OnSwitchedToMouseMode()
        {
            // Clear keyboard focus highlight when switching to mouse mode
            if (_isFocused && !_isHovered)
            {
                HideFocusEffect();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when this element is selected via EventSystem.
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {

            if (!_selectable.IsInteractable())
                return;
                
            _isFocused = true;

            // Only show keyboard focus highlight if in keyboard mode
            bool isKeyboardMode = UINavigationManager.Instance == null || UINavigationManager.Instance.IsNavigatingWithKeyboard;
            
            if (!isKeyboardMode)
            {
                return; // Don't show keyboard focus highlight in mouse mode
            }

            ShowFocusEffect();

            // Notify group about selection
            var group = GetComponentInParent<UISelectableGroup>();
            if (group != null)
            {
                group.SetLastSelected(_selectable);
            }
        }

        /// <summary>
        /// Called when this element is deselected.
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            _isFocused = false;
            
            // Don't hide if still hovered (and in mouse mode)
            if (_isHovered && _showOnMouseHover)
            {
                return;
            }
            
            HideFocusEffect();
        }

        /// <summary>
        /// Called when mouse enters this element.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_showOnMouseHover) return;
            if (!_selectable.IsInteractable()) return;

            _isHovered = true;
            
            // Only show hover highlight if in mouse mode
            bool isKeyboardMode = UINavigationManager.Instance != null && UINavigationManager.Instance.IsNavigatingWithKeyboard;
            
            if (isKeyboardMode)
            {
                return; // Don't show hover highlight in keyboard mode
            }

            ShowFocusEffect();
        }

        /// <summary>
        /// Called when mouse exits this element.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_showOnMouseHover) return;

            _isHovered = false;
            
            // Don't hide if still keyboard-focused (and in keyboard mode)
            bool isKeyboardMode = UINavigationManager.Instance != null && UINavigationManager.Instance.IsNavigatingWithKeyboard;
            
            if (_isFocused && isKeyboardMode)
            {
                return;
            }
            
            HideFocusEffect();
        }

        #endregion

        #region Visual Effect Methods

        /// <summary>
        /// Shows the appropriate focus effect based on settings.
        /// </summary>
        private void ShowFocusEffect()
        {
            KillTweens();

            if (_useScale)
                AnimateScale(true);

            if (_useOutlineImage)
                AnimateOutline(true);

            if (_useUIEffectOutline)
                AnimateUIEffectOutline(true);

            if (_useColorTint)
                AnimateColorTint(true);

            if (_useGlow)
                AnimateGlow(true);

            if (_playFocusSound)
                PlayFocusSound();
        }

        /// <summary>
        /// Hides the focus effect.
        /// </summary>
        private void HideFocusEffect()
        {
            KillTweens();

            if (_useScale)
                AnimateScale(false);

            if (_useOutlineImage)
                AnimateOutline(false);

            if (_useUIEffectOutline)
                AnimateUIEffectOutline(false);

            if (_useColorTint)
                AnimateColorTint(false);

            if (_useGlow)
                AnimateGlow(false);
        }

        /// <summary>
        /// Animates the scale effect.
        /// </summary>
        private void AnimateScale(bool show)
        {
            Vector3 targetScale = show ? _originalScale * _focusScale : _originalScale;
            _scaleTween = transform.DOScale(targetScale, _scaleDuration)
                .SetEase(_easeType)
                .SetUpdate(true);
        }

        /// <summary>
        /// Animates the outline effect.
        /// </summary>
        private void AnimateOutline(bool show)
        {
            if (_outlineImage == null)
                return;

            _outlineTween?.Kill();
            _outlinePulseTween?.Kill();

            if (show)
            {
                _outlineImage.gameObject.SetActive(true);

                //--------------------------------------------------
                // Alpha Pulse
                //--------------------------------------------------
                if (_pulseOutlineAlpha)
                {
                    _outlineImage.color = _outlineColor;

                    Sequence seq = DOTween.Sequence();

                    seq.AppendInterval(_outlineHoldDuration);

                    seq.Append(
                        _outlineImage.DOFade(
                            _outlineMinAlpha,
                            _outlineFadeOutDuration));

                    seq.Append(
                        _outlineImage.DOFade(
                            _outlineColor.a,
                            _outlineFadeInDuration));

                    seq.SetLoops(-1);
                    seq.SetEase(Ease.Linear);
                    seq.SetUpdate(true);

                    _outlinePulseTween = seq;
                    return;
                }

                //--------------------------------------------------
                // Color Pulse
                //--------------------------------------------------
                if (_pulseOutlineColor)
                {
                    _outlineImage.color = _outlinePulseColorA;

                    Sequence seq = DOTween.Sequence();

                    seq.AppendInterval(_outlineHoldDuration);

                    seq.Append(
                        _outlineImage.DOColor(
                            _outlinePulseColorB,
                            _outlineFadeOutDuration));

                    seq.Append(
                        _outlineImage.DOColor(
                            _outlinePulseColorA,
                            _outlineFadeInDuration));

                    seq.SetLoops(-1);
                    seq.SetEase(Ease.Linear);
                    seq.SetUpdate(true);

                    _outlinePulseTween = seq;
                    return;
                }

                //--------------------------------------------------
                // Default Fade In
                //--------------------------------------------------
                _outlineImage.color = new Color(
                    _outlineColor.r,
                    _outlineColor.g,
                    _outlineColor.b,
                    0f);

                _outlineTween = _outlineImage
                    .DOFade(_outlineColor.a, _outlineFadeDuration)
                    .SetEase(_easeType)
                    .SetUpdate(true);
            }
            else
            {
                _outlineTween = _outlineImage
                    .DOFade(0f, _outlineFadeDuration)
                    .SetEase(_easeType)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _outlineImage.gameObject.SetActive(false);
                    });
            }
        }

        /// <summary>
        /// Animates the UIEffectOutline effect.
        /// </summary>
        private void AnimateUIEffectOutline(bool show)
        {
            //if (_uiEffectOutline == null) return;

            //_uiEffectOutline.enabled = show;
        }


        private void AnimateColorTint(bool show)
        {
            if (_targetGraphic == null)
                return;

            _colorTintTween?.Kill();

            if (show)
            {
                _targetGraphic.color = _tintColorA;

                _colorTintTween = _targetGraphic
                    .DOColor(_tintColorB, _colorTintDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }
            else
            {
                _colorTintTween = _targetGraphic
                    .DOColor(_originalGraphicColor, _outlineFadeDuration)
                    .SetEase(_easeType)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// Animates the glow effect.
        /// </summary>
        private void AnimateGlow(bool show)
        {
            if (_glowCanvasGroup == null) return;

            float targetAlpha = show ? _glowAlpha : 0f;
            _glowTween = _glowCanvasGroup.DOFade(targetAlpha, _outlineFadeDuration)
                .SetEase(_easeType)
                .SetUpdate(true);
        }

        /// <summary>
        /// Plays the focus sound effect.
        /// </summary>
        private void PlayFocusSound()
        {
            // Use AudioManager if available (integrates with existing audio system)
            // Uses the existing PlayUINavigationSound() method for consistent audio
            if (AudioManager.Instance == null) return;

            // If the navigation manager flagged suppression for the next programmatic
            // selection, consume the flag and do not play focus sound.
            if (UINavigationManager.Instance != null && UINavigationManager.Instance.SuppressNextFocusSound)
            {
                return;
            }

            //AudioManager.Instance.PlayUINavigationSound();
        }

        /// <summary>
        /// Kills all active tweens.
        /// </summary>
        private void KillTweens()
        {
            _scaleTween?.Kill();
            _outlineTween?.Kill();
            _glowTween?.Kill();
            _colorTintTween?.Kill();
            _outlinePulseTween?.Kill();

            //if (_uiEffectOutline != null)
            //{
            //    _uiEffectOutline.enabled = false;
            //}
        }

        /// <summary>
        /// Resets visuals to original state instantly.
        /// </summary>
        private void ResetVisuals()
        {
            transform.localScale = _originalScale;

            if (_outlineImage != null)
            {
                _outlineImage.gameObject.SetActive(false);
            }

            if (_glowCanvasGroup != null)
            {
                _glowCanvasGroup.alpha = 0f;
            }

            //if (_uiEffectOutline != null)
            //{
            //    _uiEffectOutline.enabled = false;
            //}

            if (_targetGraphic != null)
            {
                _targetGraphic.color = _originalGraphicColor;
            }

            //_outlinePulseTween?.Kill();
            if (_outlineImage != null)
            {
                _outlineImage.color = _originalOutlineColor;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually triggers the focus effect (useful for programmatic selection).
        /// </summary>
        public void TriggerFocusEffect()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                return;
            }

            ShowFocusEffect();
        }

        /// <summary>
        /// Manually clears the focus effect.
        /// </summary>
        public void ClearFocusEffect()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                return;
            }

            HideFocusEffect();
        }

        /// <summary>
        /// Starts the color tint animation (exposed for external callers).
        /// </summary>
        public void StartColorTint()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                return;
            }

            AnimateColorTint(true);
        }

        /// <summary>
        /// Stops the color tint animation and restores original color.
        /// </summary>
        public void StopColorTint()
        {
            if (!UINavigationPlatformUtility.IsNavigationSupported)
            {
                return;
            }

            AnimateColorTint(false);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// Adds required components and sets up defaults in editor.
        /// </summary>
        private void Reset()
        {
            // Try to find a child named "Outline" or "FocusOutline" for the outline image
            var outlineTransform = transform.Find("Outline") ?? transform.Find("FocusOutline");
            if (outlineTransform != null)
            {
                _outlineImage = outlineTransform.GetComponent<Image>();
            }

            // Try to find a child named "Glow" for the glow effect
            var glowTransform = transform.Find("Glow") ?? transform.Find("FocusGlow");
            if (glowTransform != null)
            {
                _glowCanvasGroup = glowTransform.GetComponent<CanvasGroup>();
            }

            // Try to find a child named "UIEffectOutline" for the UIEffectOutline
            // var uiEffectOutlineTransform = transform.Find("UIEffectOutline");
            // if (uiEffectOutlineTransform != null)
            // {
            //     _uiEffectOutline = uiEffectOutlineTransform.GetComponent<UIEffectOutline>();
            // }
        }
#endif

        #endregion
    }
}
