#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomUI.Navigation
{
    /// <summary>
    /// Evaluates whether the custom UI navigation stack should run on the current target.
    /// </summary>
    public static class UINavigationPlatformUtility
    {
        /// <summary>
        /// Returns true for Windows player builds and for Editor play sessions targeting Windows.
        /// </summary>
        public static bool IsNavigationSupported
        {
            get
            {
#if UNITY_EDITOR
                BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                return activeBuildTarget == BuildTarget.StandaloneWindows ||
                    activeBuildTarget == BuildTarget.StandaloneWindows64 ||
                    activeBuildTarget == BuildTarget.StandaloneOSX;
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                return true;
#else
                return false;
#endif
            }
        }
    }
}