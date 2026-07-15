using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Almace.CustomUI
{
    [RequireComponent(typeof(RectTransform))]
    public class CustomButton : Button
    {
        [Header("Button Settings")]
        [SerializeField] private float _clickInterval = 0.15f;
        [SerializeField] private bool _hasSound = true;
        [SerializeField] private bool _useAnimation = true;
        [SerializeField] private bool _useHighlightWiggle = false;

        private float _lastClickTime;
        private static bool s_globalLocked = false;
        private bool _isLocked;
        private bool _isPressed;

        public UnityEvent onButtonClick = new();

        private string _pressTweenId;
        private string _wiggleTweenId;
        //private RandomUIWiggle _wiggleComponent;

        // Cache the RectTransform reference
        private RectTransform _rectTransform;

        [HideInInspector]
        public UnityEvent onRelease = new UnityEvent();

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Initialize the RectTransform reference
            _rectTransform = GetComponent<RectTransform>();

            //_wiggleComponent = GetComponent<RandomUIWiggle>();
            _pressTweenId = $"ButtonPress_{GetInstanceID()}";
            _wiggleTweenId = $"ButtonWiggle_{GetInstanceID()}";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTransform();
            DOTween.Kill(_pressTweenId);
            DOTween.Kill(_wiggleTweenId);

            if (_useAnimation && _useHighlightWiggle)
            {
                PlayHighlightWiggle();
            }

            _isLocked = false;
            _isPressed = false;
        }

        protected override void OnDisable()
        {
            DOTween.Kill(_pressTweenId);
            DOTween.Kill(_wiggleTweenId);

            ResetTransform();
            _isLocked = false;
            _isPressed = false;
            s_globalLocked = false;

            base.OnDisable();
        }

        #endregion

        #region Input Logic

        public override void OnPointerDown(PointerEventData eventData)
        {
            // --- FIX: Check Interactable State First ---
            if (!IsInteractable())
            {
                return;
            }

            base.OnPointerDown(eventData);

            if (_isLocked || s_globalLocked) // || UIInputBlocker.IsBusy
            {
                return;
            }

            if (Time.unscaledTime - _lastClickTime < _clickInterval)
            {
                return;
            }

            _isPressed = true;
            s_globalLocked = true;

            if (_useAnimation)
            {
                DOTween.Kill(_pressTweenId);
                DOTween.Kill(_wiggleTweenId);
                //_wiggleComponent?.PauseWiggle();
                ResetTransform();

                // Animate to pressed state and hold
                transform.DOScale(Vector3.one * 0.82f, 0.1f)
                    .SetEase(Ease.OutQuad)
                    .SetId(_pressTweenId)
                    .SetUpdate(true);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            // --- FIX: Check Interactable State First ---
            if (!IsInteractable())
            {
                // Ensure we clean up any state if interactability changed mid-press
                _isPressed = false;
                return;
            }

            base.OnPointerUp(eventData);

            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;
            _lastClickTime = Time.unscaledTime;

            // Check if the user is dragging (Scrolling). 
            if (eventData.dragging)
            {
                if (_useAnimation)
                {
                    ResetButtonInstant();
                }
                s_globalLocked = false;
                return;
            }

            // FIX: Compensate for scale animation when checking bounds.
            // Convert screen point to local space, then normalize by current scale
            // so the check uses the original unscaled rect dimensions.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localMousePos
            );

            // Normalize coordinates back to unscaled reference frame
            localMousePos.x *= _rectTransform.localScale.x;
            localMousePos.y *= _rectTransform.localScale.y;

            // Check against the original unscaled rect
            bool isInside = _rectTransform.rect.Contains(localMousePos);

            if (!isInside)
            {
                // User dragged off the button: Cancel everything
                if (_useAnimation)
                {
                    ResetButtonInstant();
                }
                s_globalLocked = false;
                return;
            }

            // User released correctly: Animate and Fire
            onRelease?.Invoke();

            // SOUND MOVED HERE: Plays on valid release
            if (_hasSound)
            {
                //AudioManager.Instance?.PlayUIButtonSound();
            }

            if (_useAnimation)
            {
                DOTween.Kill(_pressTweenId);

                Sequence seq = DOTween.Sequence().SetId(_pressTweenId).SetUpdate(true);

                // Fast bounce release
                seq.Append(transform.DOScale(Vector3.one * 1.05f, 0.06f).SetEase(Ease.OutSine));
                seq.Append(transform.DOScale(Vector3.one, 0.06f).SetEase(Ease.OutSine));

                seq.OnComplete(() =>
                {
                    _isLocked = false;
                    s_globalLocked = false;

                    onButtonClick?.Invoke();

                    //_wiggleComponent?.ResumeWiggle(0.3f);

                    if (_useHighlightWiggle)
                    {
                        WaitForInputThenWiggle();
                    }
                });
            }
            else
            {
                // No animation: Fire immediately
                _isLocked = false;
                s_globalLocked = false;
                onButtonClick?.Invoke();
            }
        }

        #endregion

        #region Wiggle / Visuals

        private void ResetTransform()
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void WaitForInputThenWiggle()
        {
            DOVirtual.DelayedCall(0.05f, () =>
            {
                //if (UIInputBlocker.IsBusy)
                //{
                //    WaitForInputThenWiggle();
                //}
                //else
                //{
                //    PlayHighlightWiggle();
                //}
            }).SetUpdate(true);
        }

        private void PlayHighlightWiggle()
        {
            DOTween.Kill(_wiggleTweenId);

            transform.localRotation = Quaternion.identity;

            Sequence wiggle = DOTween.Sequence().SetId(_wiggleTweenId).SetUpdate(true);
            wiggle.Append(transform.DOLocalRotate(new Vector3(0, 0, 8f), 0.12f).SetEase(Ease.InOutSine));
            wiggle.Append(transform.DOLocalRotate(new Vector3(0, 0, -8f), 0.12f).SetEase(Ease.InOutSine));
            wiggle.Append(transform.DOLocalRotate(Vector3.zero, 0.12f).SetEase(Ease.OutSine));
            wiggle.Join(transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 4, 0.7f));
        }

        #endregion

        #region Helpers

        public void ResetButtonInstant()
        {
            _isLocked = false;
            _isPressed = false;
            DOTween.Kill(_pressTweenId);
            DOTween.Kill(_wiggleTweenId);
            ResetTransform();

            if (_useAnimation && _useHighlightWiggle)
            {
                PlayHighlightWiggle();
            }
        }

        #endregion
    }
}