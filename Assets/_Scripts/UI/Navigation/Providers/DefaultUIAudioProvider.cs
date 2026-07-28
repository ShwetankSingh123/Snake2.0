using CustomUI.Navigation.Providers;
using UnityEngine;

namespace CustomUI.Navigation.Providers
{
    /// <summary>
    /// Default audio provider that forwards to the project's AudioManager when available.
    /// If AudioManager is not present, methods are safe no-ops.
    /// </summary>
    public class DefaultUIAudioProvider : IUIAudioProvider
    {
        public void PlayUIButtonClick()
        {
            TryInvokeAudioManager("PlayUIButtonSound");
        }

        public void PlayNavigationFocus()
        {
            TryInvokeAudioManager("PlayUINavigationSound");
        }

        public void PlaySubmit()
        {
            PlayUIButtonClick();
        }

        // Uses reflection to call into the project's AudioManager if it exists to avoid a
        // compile-time dependency on a concrete AudioManager type/assembly.
        private void TryInvokeAudioManager(string methodName)
        {
            try
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    System.Type t = null;
                    try { t = asm.GetType("AudioManager") ?? asm.GetType("Audio.AudioManager") ?? asm.GetType("Managers.AudioManager"); } catch { }
                    if (t == null) continue;

                    var prop = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (prop == null) continue;
                    var instance = prop.GetValue(null);
                    if (instance == null) continue;

                    var method = t.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (method == null) continue;
                    method.Invoke(instance, null);
                    return;
                }
            }
            catch { /* swallow reflection errors */ }
        }
    }
}
