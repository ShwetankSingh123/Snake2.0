using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CustomUI.Navigation
{
    public static class InputUtils
    {
        public static Vector2 GetSafeMousePosition(Vector2 fallback)
        {
            try { return Input.mousePosition; }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
                return fallback;
            }
        }

        public static bool SafeGetKeyDown(KeyCode kc)
        {
            try { return Input.GetKeyDown(kc); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb == null) return false;
                switch (kc)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        return kb.enterKey.wasPressedThisFrame || (kb.numpadEnterKey != null && kb.numpadEnterKey.wasPressedThisFrame);
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
                    case KeyCode.JoystickButton4: // LB
                        var gp4 = Gamepad.current; return gp4 != null && gp4.leftShoulder.wasPressedThisFrame;
                    case KeyCode.JoystickButton5: // RB
                        var gp5 = Gamepad.current; return gp5 != null && gp5.rightShoulder.wasPressedThisFrame;
                    default:
                        return false;
                }
#else
                return false;
#endif
            }
        }

        public static bool SafeGetKey(KeyCode kc)
        {
            try { return Input.GetKey(kc); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb == null) return false;
                switch (kc)
                {
                    case KeyCode.LeftArrow: return kb.leftArrowKey.isPressed;
                    case KeyCode.RightArrow: return kb.rightArrowKey.isPressed;
                    case KeyCode.UpArrow: return kb.upArrowKey.isPressed;
                    case KeyCode.DownArrow: return kb.downArrowKey.isPressed;
                    case KeyCode.A: return kb.aKey.isPressed;
                    case KeyCode.D: return kb.dKey.isPressed;
                    case KeyCode.W: return kb.wKey.isPressed;
                    case KeyCode.S: return kb.sKey.isPressed;
                    default: return false;
                }
#else
                return false;
#endif
            }
        }

        public static float SafeGetAxis(string axisName, string horizontalAxis, string verticalAxis)
        {
            try { return Input.GetAxis(axisName); }
            catch
            {
#if ENABLE_INPUT_SYSTEM
                var gp = Gamepad.current;
                if (gp == null) return 0f;
                var v = gp.leftStick.ReadValue();
                if (axisName == horizontalAxis) return v.x;
                if (axisName == verticalAxis) return v.y;
                return 0f;
#else
                return 0f;
#endif
            }
        }

        public static bool SafeGetButtonDown(string buttonName)
        {
            try { return Input.GetButtonDown(buttonName); }
            catch { return false; }
        }
    }
}
