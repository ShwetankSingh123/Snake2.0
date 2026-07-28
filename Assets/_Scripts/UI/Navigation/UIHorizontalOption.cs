using CustomUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Attach this to any settings row selectable (e.g. Resolution, Shadow, VSync) to give it
    /// Slider-like Left / Right behaviour.
    ///
    /// <list type="bullet">
    ///   <item><description>Left Arrow  → invokes <see cref="_previousButton"/> and keeps focus on this row.</description></item>
    ///   <item><description>Right Arrow → invokes <see cref="_nextButton"/>   and keeps focus on this row.</description></item>
    ///   <item><description>Up / Down   → Unity's built-in navigation moves focus between rows as normal.</description></item>
    /// </list>
    ///
    /// Input is handled via two complementary paths so the component works regardless of
    /// whether the EventSystem's <c>sendNavigationEvents</c> flag is enabled:
    /// <list type="bullet">
    ///   <item><description><b>Update()</b> — polls <c>Input.GetKeyDown</c> for keyboard arrow keys while this row is selected.</description></item>
    ///   <item><description><b>IMoveHandler</b> — receives EventSystem move events for gamepad / other input devices.</description></item>
    /// </list>
    /// A per-direction frame counter prevents both paths from firing in the same frame.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIHorizontalOption : MonoBehaviour, IMoveHandler, ISelectHandler, IDeselectHandler
    {
        /// <summary>
        /// The Selectable that represents this row (typically the Selectable on this same GameObject).
        /// Focus is always restored to this object after a Left or Right press.
        /// </summary>
        [Tooltip("The Selectable that represents this row. Focus is kept here after Left/Right presses.")]
        [SerializeField] private Selectable _ownerSelectable;

        /// <summary>Invoked when the user presses Left Arrow or D-pad Left (e.g. BackBtn).</summary>
        [Tooltip("Button invoked on Left Arrow / D-pad Left (e.g. BackBtn).")]
        [SerializeField] private CustomButton _previousButton;

        /// <summary>Invoked when the user presses Right Arrow or D-pad Right (e.g. NextBtn).</summary>
        [Tooltip("Button invoked on Right Arrow / D-pad Right (e.g. NextBtn).")]
        [SerializeField] private CustomButton _nextButton;

        // True while this row is the EventSystem's currently selected object.
        private bool _isSelected;

        // Per-direction frame counters used to prevent double-invocation when both
        // Update() and IMoveHandler fire in the same frame.
        private int _lastPreviousFrame = -1;
        private int _lastNextFrame     = -1;

        #region Unity Lifecycle

#if UNITY_EDITOR
        /// <summary>
        /// Auto-populates <see cref="_ownerSelectable"/> when the component is added in the Editor.
        /// </summary>
        private void Reset()
        {
            _ownerSelectable = GetComponent<Selectable>();
        }
#endif

        #endregion

        #region ISelectHandler / IDeselectHandler

        /// <summary>Marks this row as selected so Update() starts polling keyboard input.</summary>
        public void OnSelect(BaseEventData eventData) => _isSelected = true;

        /// <summary>Marks this row as deselected so Update() stops polling keyboard input.</summary>
        public void OnDeselect(BaseEventData eventData) => _isSelected = false;

        #endregion

        #region Update – Keyboard polling

        /// <summary>
        /// Polls arrow-key input each frame while this row is selected.
        /// Handles cases where the EventSystem's <c>sendNavigationEvents</c> is disabled
        /// and <see cref="OnMove"/> therefore never fires.
        /// </summary>
        private void Update()
        {
            if (!_isSelected) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                InvokePrevious();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                InvokeNext();
            }
        }

        #endregion

        #region IMoveHandler – Gamepad / EventSystem navigation

        /// <summary>
        /// Called by Unity's EventSystem when a directional navigation event fires on this object.
        /// Left / Right invoke the corresponding button and consume the event so Unity does not
        /// move focus. Up / Down are intentionally not consumed so normal row-to-row navigation works.
        /// </summary>
        /// <param name="eventData">Navigation event data provided by the EventSystem.</param>
        public void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (_previousButton != null)
                    {
                        InvokePrevious();
                        eventData.Use(); // Prevent Unity from navigating away
                    }
                    break;

                case MoveDirection.Right:
                    if (_nextButton != null)
                    {
                        InvokeNext();
                        eventData.Use(); // Prevent Unity from navigating away
                    }
                    break;

                // MoveDirection.Up / MoveDirection.Down:
                // Do NOT call eventData.Use() — Unity handles row-to-row navigation automatically.
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Invokes the previous button click at most once per frame.
        /// The frame guard prevents double-firing when both <see cref="Update"/> and
        /// <see cref="OnMove"/> detect the same left-press in the same frame.
        /// </summary>
        private void InvokePrevious()
        {
            Debug.Log("InvokePrevious");
            if (_previousButton == null) return;
            if (Time.frameCount == _lastPreviousFrame) return;

            _lastPreviousFrame = Time.frameCount;
            _previousButton.onButtonClick.Invoke();
            ReselectOwner();
        }

        /// <summary>
        /// Invokes the next button click at most once per frame.
        /// The frame guard prevents double-firing when both <see cref="Update"/> and
        /// <see cref="OnMove"/> detect the same right-press in the same frame.
        /// </summary>
        private void InvokeNext()
        {
            Debug.Log("InvokeNext");
            if (_nextButton == null) return;
            if (Time.frameCount == _lastNextFrame) return;

            _lastNextFrame = Time.frameCount;
            _nextButton.onButtonClick.Invoke();
            ReselectOwner();
        }

        /// <summary>
        /// Immediately restores EventSystem selection to the owner selectable so that focus
        /// never drifts to BackBtn, NextBtn, or any adjacent row after a Left/Right press.
        /// </summary>
        private void ReselectOwner()
        {
            if (_ownerSelectable == null) return;
            if (EventSystem.current == null) return;

            EventSystem.current.SetSelectedGameObject(_ownerSelectable.gameObject);
        }

        #endregion
    }
}
