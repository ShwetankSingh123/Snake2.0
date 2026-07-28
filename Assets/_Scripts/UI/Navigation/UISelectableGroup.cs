using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomUI.Navigation
{
    /// <summary>
    /// Defines a group of UI Selectables that can be navigated as a unit.
    /// Supports manual ordered lists and sub-groups for complex layouts.
    /// Attach this to a panel/menu parent to manage navigation within that panel.
    /// </summary>
    public class UISelectableGroup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Navigation Mode")]
        [Tooltip("How navigation between selectables should be configured.")]
        [SerializeField] private NavigationMode _navigationMode = NavigationMode.Automatic;

        [Tooltip("Direction for explicit navigation (Vertical = up/down, Horizontal = left/right, Grid = four-way spatial navigation).")]
        [SerializeField] private NavigationDirection _primaryDirection = NavigationDirection.Vertical;

        [Tooltip("If true, navigation wraps from last to first element and vice versa.")]
        [SerializeField] private bool _wrapNavigation = true;

        [Header("Manual Selectable List")]
        [Tooltip("Enable to use a manually ordered list instead of auto-detecting selectables.")]
        [SerializeField] private bool _useManualList = false;

        [Tooltip("Manually ordered list of selectables. Order determines navigation sequence.")]
        [SerializeField] private List<Selectable> _manualSelectableList = new List<Selectable>();

        [Header("Sub-Groups (for complex layouts)")]
        [Tooltip("Enable sub-groups for panels with multiple button clusters.")]
        [SerializeField] private bool _useSubGroups = false;

        [Tooltip("List of sub-groups. Tab cycles between these. Each has its own navigation.")]
        [SerializeField] private List<NavigationSubGroup> _subGroups = new List<NavigationSubGroup>();

        [Header("Default Selection")]
        [Tooltip("The selectable to select by default when this group becomes active.")]
        [SerializeField] private Selectable _defaultSelectable;

        [Tooltip("If true, remembers the last selected item and returns to it.")]
        [SerializeField] private bool _rememberLastSelection = true;

        [Header("Auto-Registration")]
        [Tooltip("If true, automatically registers this group with UINavigationManager when enabled.")]
        [SerializeField] private bool _autoRegister = true;

        [Tooltip("If true, auto-registers as a pushed group (for popups).")]
        [SerializeField] private bool _isPushGroup = false;

        [Header("Special Roles")]
        [Tooltip("Mark this group as the home/main screen. Pressing Escape on the home screen will request an exit confirmation.")]
        [SerializeField] private bool _isHomeScreen = false;

        [Header("Auto-Enhance")]
        [Tooltip("If true, automatically adds UISelectableEnhancer to selectables added to this group.")]
        [SerializeField] private bool _autoEnhanceSelectables = true;

        [Tooltip("If true, automatically adds UIFocusVisualizer to selectables added to this group.")]
        [SerializeField] private bool _autoAddFocusVisualizer = true;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Private Fields

        // Cached list of selectables (auto-detected or manual)
        private List<Selectable> _selectables = new List<Selectable>();

        // Last selected item (for remember feature)
        private Selectable _lastSelected;

        // Current active sub-group index
        private int _currentSubGroupIndex = 0;
        
        // Tracks whether the navigation log file has been initialized for this app session.
        private static bool _hasInitializedNavigationLogFile = false;
        
        // Prevents repeated console spam if the file log cannot be written.
        private static bool _hasReportedNavigationLogWriteFailure = false;

        #endregion

        #region Enums

        /// <summary>
        /// Defines how navigation is configured.
        /// </summary>
        public enum NavigationMode
        {
            /// <summary>Unity's automatic navigation (based on spatial proximity).</summary>
            Automatic,
            /// <summary>Sequential navigation through elements in list order.</summary>
            Sequential,
            /// <summary>Keep whatever navigation is already set on selectables.</summary>
            Manual
        }

        /// <summary>
        /// Primary direction for explicit navigation.
        /// </summary>
        public enum NavigationDirection
        {
            Vertical,
            Horizontal,
            Grid
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Defines a sub-group of selectables within a panel.
        /// Useful for panels with multiple button clusters at different positions.
        /// </summary>
        [Serializable]
        public class NavigationSubGroup
        {
            [Tooltip("Name for identification in inspector.")]
            public string groupName = "SubGroup";

            [Tooltip("If false, this sub-group is skipped during tab cycling.")]
            public bool isEnabled = true;

            [Tooltip("Ordered list of selectables in this sub-group.")]
            public List<Selectable> selectables = new List<Selectable>();

            [Tooltip("Default selectable when this sub-group is activated.")]
            public Selectable defaultSelectable;

            [Tooltip("Navigation direction within this sub-group.")]
            public NavigationDirection direction = NavigationDirection.Vertical;

            [Tooltip("Wrap navigation within this sub-group.")]
            public bool wrapNavigation = true;

            // Runtime: last selected in this sub-group
            [NonSerialized] public Selectable lastSelected;

            /// <summary>
            /// Gets the best selectable to focus when entering this sub-group.
            /// </summary>
            public Selectable GetDefaultSelectable()
            {
                // Try last selected
                if (lastSelected != null && lastSelected.IsInteractable() && lastSelected.gameObject.activeInHierarchy)
                {
                    return lastSelected;
                }

                // Try configured default
                if (defaultSelectable != null && defaultSelectable.IsInteractable() && defaultSelectable.gameObject.activeInHierarchy)
                {
                    return defaultSelectable;
                }

                // Fall back to first valid
                foreach (var s in selectables)
                {
                    if (s != null && s.IsInteractable() && s.gameObject.activeInHierarchy)
                    {
                        return s;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current active sub-group index.
        /// </summary>
        public int CurrentSubGroupIndex => _currentSubGroupIndex;

        /// <summary>
        /// Number of sub-groups.
        /// </summary>
        public int SubGroupCount => _useSubGroups ? _subGroups.Count : 0;

        /// <summary>
        /// Whether sub-groups are enabled.
        /// </summary>
        public bool HasSubGroups => _useSubGroups && _subGroups.Count > 0;

        /// <summary>
        /// True if this group should be treated as a pushed popup group.
        /// </summary>
        public bool IsPushGroup => _isPushGroup;

        /// <summary>
        /// True if this group represents the home/main screen.
        /// Pressing Escape on this group should request an exit confirmation.
        /// </summary>
        public bool IsHomeScreen => _isHomeScreen;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            RefreshSelectables();

            if (_autoRegister && UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.PushGroup(this);
            }
        }

        private void OnDisable()
        {
            if (_autoRegister && UINavigationManager.Instance != null)
            {
                if (UINavigationManager.Instance.CurrentGroup == this)
                {
                    UINavigationManager.Instance.PopGroup();
                }
            }
        }

        private void OnTransformChildrenChanged()
        {
            if (Application.isPlaying && !_useManualList)
            {
                RefreshSelectables();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the list of selectables and reconfigures navigation.
        /// </summary>
        public void RefreshSelectables()
        {
            _selectables.Clear();

            if (_useSubGroups && _subGroups.Count > 0)
            {
                // Configure each sub-group's navigation
                ConfigureSubGroupsNavigation();

                // Collect all selectables from all sub-groups
                foreach (var subGroup in _subGroups)
                {
                    if (!subGroup.isEnabled)
                        continue;
                        
                    foreach (var s in subGroup.selectables)
                    {
                        if (s != null && !_selectables.Contains(s))
                        {
                            _selectables.Add(s);
                        }
                    }
                }
            }
            else if (_useManualList)
            {
                // Use manual list
                foreach (var s in _manualSelectableList)
                {
                    if (s != null)
                    {
                        _selectables.Add(s);
                    }
                }
                ConfigureNavigation();
            }
            else
            {
                // Auto-detect selectables in children
                var allSelectables = GetComponentsInChildren<Selectable>(false);
                foreach (var s in allSelectables)
                {
                    if (s != null && s.gameObject.activeInHierarchy)
                    {
                        _selectables.Add(s);
                    }
                }
                ConfigureNavigation();
            }

            LogInfo($"[UISelectableGroup] {name}: Refreshed {_selectables.Count} selectables, SubGroups: {(_useSubGroups ? _subGroups.Count : 0)}");
        }

        /// <summary>
        /// Gets the default selectable for this group.
        /// </summary>
        public Selectable GetDefaultSelectable()
        {
            // If using sub-groups, get default from current sub-group
            if (_useSubGroups && _subGroups.Count > 0)
            {
                var subGroup = _subGroups[_currentSubGroupIndex];
                return subGroup.GetDefaultSelectable();
            }

            // Try remembered selection
            if (_rememberLastSelection && _lastSelected != null &&
                _lastSelected.IsInteractable() && _lastSelected.gameObject.activeInHierarchy)
            {
                return _lastSelected;
            }

            // Try configured default
            if (_defaultSelectable != null &&
                _defaultSelectable.IsInteractable() && _defaultSelectable.gameObject.activeInHierarchy)
            {
                return _defaultSelectable;
            }

            // Fall back to first valid selectable
            foreach (var s in _selectables)
            {
                if (s != null && s.IsInteractable() && s.gameObject.activeInHierarchy)
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>
        /// Cycles to the next sub-group (called on Tab press).
        /// </summary>
        /// <param name="reverse">If true, cycles to previous sub-group (Shift+Tab).</param>
        /// <returns>The default selectable of the new sub-group.</returns>
        public Selectable CycleToNextSubGroup(bool reverse = false)
        {
            if (!_useSubGroups || _subGroups.Count <= 1) return null;

            // Count enabled sub-groups
            int enabledCount = 0;
            foreach (var sg in _subGroups)
            {
                if (sg.isEnabled) enabledCount++;
            }

            // Need at least 2 enabled sub-groups to cycle
            if (enabledCount <= 1) return null;

            // Save current selection to current sub-group
            SaveCurrentSelectionToSubGroup();

            // Find the next enabled sub-group
            int startIndex = _currentSubGroupIndex;
            int attempts = 0;
            do
            {
                if (reverse)
                {
                    _currentSubGroupIndex--;
                    if (_currentSubGroupIndex < 0)
                        _currentSubGroupIndex = _subGroups.Count - 1;
                }
                else
                {
                    _currentSubGroupIndex++;
                    if (_currentSubGroupIndex >= _subGroups.Count)
                        _currentSubGroupIndex = 0;
                }

                attempts++;

                // Safety check to prevent infinite loop
                if (attempts > _subGroups.Count)
                {
                    _currentSubGroupIndex = startIndex;
                    return null;
                }

            } while (!_subGroups[_currentSubGroupIndex].isEnabled);

            var newSubGroup = _subGroups[_currentSubGroupIndex];

            LogInfo($"[UISelectableGroup] Cycled to sub-group: {newSubGroup.groupName} (index {_currentSubGroupIndex})");

            return newSubGroup.GetDefaultSelectable();
        }

        /// <summary>
        /// Sets the active sub-group by index.
        /// </summary>
        public Selectable SetActiveSubGroup(int index)
        {
            if (!_useSubGroups || index < 0 || index >= _subGroups.Count) return null;

            SaveCurrentSelectionToSubGroup();
            _currentSubGroupIndex = index;

            return _subGroups[_currentSubGroupIndex].GetDefaultSelectable();
        }

        /// <summary>
        /// Sets the last selected element.
        /// </summary>
        public void SetLastSelected(Selectable selectable)
        {
            if (!_rememberLastSelection) return;

            _lastSelected = selectable;

            // Also update the sub-group's last selected
            if (_useSubGroups)
            {
                foreach (var subGroup in _subGroups)
                {
                    if (subGroup.selectables.Contains(selectable))
                    {
                        subGroup.lastSelected = selectable;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Clears the remembered selection.
        /// </summary>
        public void ClearLastSelected()
        {
            _lastSelected = null;
            foreach (var subGroup in _subGroups)
            {
                subGroup.lastSelected = null;
            }
        }

        /// <summary>
        /// Gets all selectables in this group.
        /// </summary>
        public IReadOnlyList<Selectable> GetSelectables()
        {
            return _selectables;
        }

        /// <summary>
        /// Gets selectables in a specific sub-group.
        /// </summary>
        public IReadOnlyList<Selectable> GetSubGroupSelectables(int index)
        {
            if (!_useSubGroups || index < 0 || index >= _subGroups.Count)
                return _selectables;

            return _subGroups[index].selectables;
        }

        /// <summary>
        /// Finds which sub-group contains a selectable.
        /// </summary>
        public int FindSubGroupIndex(Selectable selectable)
        {
            if (!_useSubGroups) return -1;

            for (int i = 0; i < _subGroups.Count; i++)
            {
                if (_subGroups[i].selectables.Contains(selectable))
                    return i;
            }
            return -1;
        }

        #endregion

        #region Dynamic Sub-Group Management

        /// <summary>
        /// Adds a selectable to a sub-group at runtime.
        /// Use this when instantiating UI items dynamically.
        /// </summary>
        /// <param name="subGroupIndex">Index of the sub-group to add to.</param>
        /// <param name="selectable">The selectable to add.</param>
        /// <param name="reconfigureNavigation">If true, reconfigures navigation links after adding.</param>
        public void AddToSubGroup(int subGroupIndex, Selectable selectable, bool reconfigureNavigation = true)
        {
            if (!_useSubGroups || subGroupIndex < 0 || subGroupIndex >= _subGroups.Count)
            {
                LogWarning($"[UISelectableGroup] Invalid sub-group index: {subGroupIndex}");
                return;
            }

            if (selectable == null) return;

            var subGroup = _subGroups[subGroupIndex];

            if (!subGroup.selectables.Contains(selectable))
            {
                // Auto-enhance the selectable for keyboard/gamepad support
                EnsureSelectableEnhanced(selectable);

                subGroup.selectables.Add(selectable);

                // Also add to main selectables cache
                if (!_selectables.Contains(selectable))
                {
                    _selectables.Add(selectable);
                }

                if (reconfigureNavigation)
                {
                    ConfigureExplicitNavigation(subGroup.selectables, subGroup.direction, subGroup.wrapNavigation);
                }

                LogInfo($"[UISelectableGroup] Added {selectable.name} to sub-group '{subGroup.groupName}'");
            }
        }

        /// <summary>
        /// Adds a selectable to a sub-group by name at runtime.
        /// </summary>
        /// <param name="subGroupName">Name of the sub-group.</param>
        /// <param name="selectable">The selectable to add.</param>
        /// <param name="reconfigureNavigation">If true, reconfigures navigation links after adding.</param>
        public void AddToSubGroup(string subGroupName, Selectable selectable, bool reconfigureNavigation = true)
        {
            int index = GetSubGroupIndexByName(subGroupName);
            if (index >= 0)
            {
                AddToSubGroup(index, selectable, reconfigureNavigation);
            }
            else
            {
                LogWarning($"[UISelectableGroup] Sub-group '{subGroupName}' not found.");
            }
        }

        /// <summary>
        /// Adds multiple selectables to a sub-group at runtime.
        /// More efficient than calling AddToSubGroup multiple times.
        /// </summary>
        /// <param name="subGroupIndex">Index of the sub-group.</param>
        /// <param name="selectables">The selectables to add.</param>
        public void AddRangeToSubGroup(int subGroupIndex, IEnumerable<Selectable> selectables)
        {
            if (!_useSubGroups || subGroupIndex < 0 || subGroupIndex >= _subGroups.Count) return;

            var subGroup = _subGroups[subGroupIndex];

            foreach (var selectable in selectables)
            {
                if (selectable != null && !subGroup.selectables.Contains(selectable))
                {
                    // Auto-enhance the selectable for keyboard/gamepad support
                    EnsureSelectableEnhanced(selectable);

                    subGroup.selectables.Add(selectable);

                    if (!_selectables.Contains(selectable))
                    {
                        _selectables.Add(selectable);
                    }
                }
            }

            // Reconfigure navigation once after all items added
            ConfigureExplicitNavigation(subGroup.selectables, subGroup.direction, subGroup.wrapNavigation);

            LogInfo($"[UISelectableGroup] Added multiple items to sub-group '{subGroup.groupName}'. Total: {subGroup.selectables.Count}");
        }

        /// <summary>
        /// Removes a selectable from its sub-group at runtime.
        /// Use this when destroying dynamically created UI items.
        /// </summary>
        /// <param name="selectable">The selectable to remove.</param>
        /// <param name="reconfigureNavigation">If true, reconfigures navigation links after removing.</param>
        public void RemoveFromSubGroup(Selectable selectable, bool reconfigureNavigation = true)
        {
            if (!_useSubGroups || selectable == null) return;

            foreach (var subGroup in _subGroups)
            {
                if (subGroup.selectables.Remove(selectable))
                {
                    _selectables.Remove(selectable);

                    // Clear last selected if it was this item
                    if (subGroup.lastSelected == selectable)
                    {
                        subGroup.lastSelected = null;
                    }

                    if (reconfigureNavigation)
                    {
                        ConfigureExplicitNavigation(subGroup.selectables, subGroup.direction, subGroup.wrapNavigation);
                    }

                    LogInfo($"[UISelectableGroup] Removed {selectable.name} from sub-group '{subGroup.groupName}'");

                    break;
                }
            }
        }

        /// <summary>
        /// Clears all selectables from a sub-group.
        /// Use this before repopulating with new items.
        /// </summary>
        /// <param name="subGroupIndex">Index of the sub-group to clear.</param>
        public void ClearSubGroup(int subGroupIndex)
        {
            if (!_useSubGroups || subGroupIndex < 0 || subGroupIndex >= _subGroups.Count) return;

            var subGroup = _subGroups[subGroupIndex];

            // Remove from main cache
            foreach (var s in subGroup.selectables)
            {
                _selectables.Remove(s);
            }

            subGroup.selectables.Clear();
            subGroup.lastSelected = null;

            LogInfo($"[UISelectableGroup] Cleared sub-group '{subGroup.groupName}'");
        }

        /// <summary>
        /// Clears a sub-group by name.
        /// </summary>
        /// <param name="subGroupName">Name of the sub-group to clear.</param>
        public void ClearSubGroup(string subGroupName)
        {
            int index = GetSubGroupIndexByName(subGroupName);
            if (index >= 0)
            {
                ClearSubGroup(index);
            }
        }

        /// <summary>
        /// Enables or disables a sub-group for tab cycling.
        /// Disabled sub-groups are skipped when pressing Tab.
        /// Use this to show/hide content-specific navigation based on which tab is active.
        /// </summary>
        /// <param name="subGroupIndex">Index of the sub-group.</param>
        /// <param name="enabled">True to enable, false to disable.</param>
        public void SetSubGroupEnabled(int subGroupIndex, bool enabled)
        {
            if (!_useSubGroups || subGroupIndex < 0 || subGroupIndex >= _subGroups.Count) return;

            _subGroups[subGroupIndex].isEnabled = enabled;

            LogInfo($"[UISelectableGroup] Sub-group '{_subGroups[subGroupIndex].groupName}' {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Enables or disables a sub-group by name.
        /// </summary>
        /// <param name="subGroupName">Name of the sub-group.</param>
        /// <param name="enabled">True to enable, false to disable.</param>
        public void SetSubGroupEnabled(string subGroupName, bool enabled)
        {
            int index = GetSubGroupIndexByName(subGroupName);
            if (index >= 0)
            {
                SetSubGroupEnabled(index, enabled);
            }
            else
            {
                LogWarning($"[UISelectableGroup] Sub-group '{subGroupName}' not found for enable/disable.");
            }
        }

        /// <summary>
        /// Enables multiple sub-groups and disables all others.
        /// Useful when switching tabs to show only relevant sub-groups.
        /// </summary>
        /// <param name="enabledSubGroupNames">Names of sub-groups to enable. All others will be disabled.</param>
        public void SetOnlySubGroupsEnabled(params string[] enabledSubGroupNames)
        {
            if (!_useSubGroups) return;

            var enabledSet = new HashSet<string>(enabledSubGroupNames);

            for (int i = 0; i < _subGroups.Count; i++)
            {
                _subGroups[i].isEnabled = enabledSet.Contains(_subGroups[i].groupName);
                LogInfo($"[UISelectableGroup] Sub-group '{_subGroups[i].groupName}' {(_subGroups[i].isEnabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Gets the index of a sub-group by name.
        /// </summary>
        /// <param name="subGroupName">Name of the sub-group.</param>
        /// <returns>Index of the sub-group, or -1 if not found.</returns>
        public int GetSubGroupIndexByName(string subGroupName)
        {
            if (!_useSubGroups) return -1;

            for (int i = 0; i < _subGroups.Count; i++)
            {
                if (_subGroups[i].groupName == subGroupName)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Sets the default selectable for a sub-group at runtime.
        /// </summary>
        /// <param name="subGroupIndex">Index of the sub-group.</param>
        /// <param name="selectable">The selectable to set as default.</param>
        public void SetSubGroupDefault(int subGroupIndex, Selectable selectable)
        {
            if (!_useSubGroups || subGroupIndex < 0 || subGroupIndex >= _subGroups.Count) return;

            _subGroups[subGroupIndex].defaultSelectable = selectable;
        }

        /// <summary>
        /// Reconfigures navigation for a specific sub-group.
        /// Call after batch modifications.
        /// </summary>
        /// <param name="subGroupIndex">Index of the sub-group to reconfigure.</param>
        public void ReconfigureSubGroupNavigation(int subGroupIndex)
        {
            if (!_useSubGroups || subGroupIndex < 0 || subGroupIndex >= _subGroups.Count) return;

            var subGroup = _subGroups[subGroupIndex];
            ConfigureExplicitNavigation(subGroup.selectables, subGroup.direction, subGroup.wrapNavigation);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures a selectable has UISelectableEnhancer and UIFocusVisualizer if auto-enhance is enabled.
        /// This allows Enter/Space to work on buttons added dynamically at runtime.
        /// </summary>
        /// <param name="selectable">The selectable to enhance.</param>
        private void EnsureSelectableEnhanced(Selectable selectable)
        {
            if (selectable == null) return;
            if (!UINavigationPlatformUtility.IsNavigationSupported) return;

            // Add UISelectableEnhancer for keyboard/gamepad submit handling
            if (_autoEnhanceSelectables)
            {
                if (selectable.GetComponent<UISelectableEnhancer>() == null)
                {
                    var enhancer = selectable.gameObject.AddComponent<UISelectableEnhancer>();
                    
                    LogInfo($"[UISelectableGroup] Auto-added UISelectableEnhancer to {selectable.name}");
                }
            }

            // Add UIFocusVisualizer for visual feedback when focused
            if (_autoAddFocusVisualizer)
            {
                if (selectable.GetComponent<UIFocusVisualizer>() == null)
                {
                    selectable.gameObject.AddComponent<UIFocusVisualizer>();
                    
                    LogInfo($"[UISelectableGroup] Auto-added UIFocusVisualizer to {selectable.name}");
                }
            }
        }

        /// <summary>
        /// Saves current EventSystem selection to the appropriate sub-group.
        /// </summary>
        private void SaveCurrentSelectionToSubGroup()
        {
            if (!_useSubGroups) return;

            var currentSelected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (currentSelected == null) return;

            var selectable = currentSelected.GetComponent<Selectable>();
            if (selectable == null) return;

            // Find and update the sub-group containing this selectable
            foreach (var subGroup in _subGroups)
            {
                if (subGroup.selectables.Contains(selectable))
                {
                    subGroup.lastSelected = selectable;
                    break;
                }
            }
        }

        /// <summary>
        /// Configures navigation for the main selectables list.
        /// </summary>
        private void ConfigureNavigation()
        {
            if (_selectables.Count == 0) return;

            switch (_navigationMode)
            {
                case NavigationMode.Automatic:
                    ConfigureAutomaticNavigation(_selectables);
                    break;
                case NavigationMode.Sequential:
                    ConfigureExplicitNavigation(_selectables, _primaryDirection, _wrapNavigation);
                    break;
                case NavigationMode.Manual:
                    // Don't modify navigation
                    break;
            }
        }

        /// <summary>
        /// Configures navigation for all sub-groups.
        /// </summary>
        private void ConfigureSubGroupsNavigation()
        {
            foreach (var subGroup in _subGroups)
            {
                if (!subGroup.isEnabled)
                    continue;

                if (subGroup.selectables.Count > 0)
                {
                    ConfigureExplicitNavigation(subGroup.selectables, subGroup.direction, subGroup.wrapNavigation);
                }
            }
        }

        private Selectable GetNextInteractableSelectable(
            List<Selectable> selectables,
            int startIndex,
            int direction,
            bool wrap)
        {
            int count = selectables.Count;

            for (int i = 0; i < count; i++)
            {
                startIndex += direction;

                if (wrap)
                {
                    if (startIndex < 0)
                        startIndex = count - 1;

                    if (startIndex >= count)
                        startIndex = 0;
                }
                else
                {
                    if (startIndex < 0 || startIndex >= count)
                        return null;
                }

                var candidate = selectables[startIndex];

                if (candidate != null &&
                    candidate.gameObject.activeInHierarchy &&
                    candidate.IsInteractable())
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Configures Unity's automatic spatial navigation.
        /// </summary>
        private void ConfigureAutomaticNavigation(List<Selectable> selectables)
        {
            foreach (var selectable in selectables)
            {
                if (selectable == null) continue;
                var nav = selectable.navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.Automatic;
                selectable.navigation = nav;
            }
        }

        /// <summary>
        /// Configures explicit navigation based on list order or grid position.
        /// </summary>
        private void ConfigureExplicitNavigation(List<Selectable> selectables, NavigationDirection direction, bool wrap)
        {
            if (direction == NavigationDirection.Grid)
            {
                ConfigureGridNavigation(selectables, wrap);
                return;
            }

            //Debug.Log("================================");
            //Debug.Log($"Grid Group Count = {selectables.Count}");

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;

                var nav = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit
                };

                // Calculate previous and next indices
                int prevIndex = i - 1;
                int nextIndex = i + 1;

                if (wrap)
                {
                    if (prevIndex < 0) prevIndex = selectables.Count - 1;
                    if (nextIndex >= selectables.Count) nextIndex = 0;
                }

                // Assign based on direction
                if (direction == NavigationDirection.Vertical)
                {
                    nav.selectOnUp =
                        GetNextInteractableSelectable(
                            selectables,
                            i,
                            -1,
                            wrap);

                    nav.selectOnDown =
                        GetNextInteractableSelectable(
                            selectables,
                            i,
                            +1,
                            wrap);
                }
                else
                {
                    nav.selectOnLeft =
                        GetNextInteractableSelectable(
                            selectables,
                            i,
                            -1,
                            wrap);

                    nav.selectOnRight =
                        GetNextInteractableSelectable(
                            selectables,
                            i,
                            +1,
                            wrap);
                }

                selectable.navigation = nav;
            }
        }

        /// <summary>
        /// Configures four-way navigation using GridLayoutGroup slots when available,
        /// falling back to spatial proximity otherwise.
        /// </summary>
        private void ConfigureGridNavigation(List<Selectable> selectables, bool wrap)
        {
            RefreshLayoutForGridNavigation(selectables);

            if (ConfigureGridLayoutNavigation(selectables, wrap))
            {
                return;
            }

            Debug.Log("================================");
            Debug.Log($"Grid Group Count = {selectables.Count}");

            LogInfo($"[UISelectableGroup] {name}: No shared GridLayoutGroup found for grid navigation. Falling back to spatial neighbor search.");

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;
                 Debug.Log($"{selectable.name} Active:{selectable.gameObject.activeInHierarchy} Interactable:{selectable.IsInteractable()}");

                var nav = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = FindDirectionalSelectable(selectable, selectables, Vector2.up, wrap),
                    selectOnDown = FindDirectionalSelectable(selectable, selectables, Vector2.down, wrap),
                    selectOnLeft = FindDirectionalSelectable(selectable, selectables, Vector2.left, wrap),
                    selectOnRight = FindDirectionalSelectable(selectable, selectables, Vector2.right, wrap)
                };

                selectable.navigation = nav;
                LogGridNavigationLinks(selectable, nav, "spatial", string.Empty);
            }
        }

        /// <summary>
        /// Configures grid navigation from the shared GridLayoutGroup row and column order.
        /// </summary>
        private bool ConfigureGridLayoutNavigation(List<Selectable> selectables, bool wrap)
        {
            if (!TryGetSharedGridLayout(selectables, out GridLayoutGroup gridLayoutGroup, out RectTransform gridLayoutRoot))
            {
                return false;
            }

            int activeChildCount = GetActiveGridChildCount(gridLayoutRoot);
            if (!TryGetGridDimensions(gridLayoutGroup, activeChildCount, out int rowCount, out int columnCount))
            {
                LogInfo($"[UISelectableGroup] {name}: GridLayoutGroup on {gridLayoutRoot.name} uses Flexible constraint. Falling back to spatial navigation.");

                return false;
            }

            Dictionary<Vector2Int, Selectable> selectablesByGridPosition = new Dictionary<Vector2Int, Selectable>();
            Dictionary<Selectable, Vector2Int> gridPositionBySelectable = new Dictionary<Selectable, Vector2Int>();

            foreach (var selectable in selectables)
            {
                if (selectable == null || !selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                {
                    continue;
                }

                RectTransform layoutChild = GetGridLayoutChild(selectable, gridLayoutRoot);
                if (layoutChild == null)
                {
                    continue;
                }

                int activeSiblingIndex = GetActiveGridSiblingIndex(gridLayoutRoot, layoutChild);
                if (activeSiblingIndex < 0)
                {
                    continue;
                }

                Vector2Int gridPosition = GetGridPositionFromSiblingIndex(activeSiblingIndex, rowCount, columnCount, gridLayoutGroup);
                selectablesByGridPosition[gridPosition] = selectable;
                gridPositionBySelectable[selectable] = gridPosition;

                LogInfo($"[UISelectableGroup] {name}: Grid slot for {selectable.name} -> row {gridPosition.y}, col {gridPosition.x}, activeIndex {activeSiblingIndex}, siblingIndex {layoutChild.GetSiblingIndex()}");
            }

            if (gridPositionBySelectable.Count == 0)
            {
                return false;
            }

            foreach (var pair in gridPositionBySelectable)
            {
                Selectable origin = pair.Key;
                Vector2Int originPosition = pair.Value;

                var nav = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = FindSelectableInGridDirection(origin, originPosition, selectablesByGridPosition, new Vector2Int(0, -1), rowCount, columnCount, wrap),
                    selectOnDown = FindSelectableInGridDirection(origin, originPosition, selectablesByGridPosition, new Vector2Int(0, 1), rowCount, columnCount, wrap),
                    selectOnLeft = FindSelectableInGridDirection(origin, originPosition, selectablesByGridPosition, new Vector2Int(-1, 0), rowCount, columnCount, wrap),
                    selectOnRight = FindSelectableInGridDirection(origin, originPosition, selectablesByGridPosition, new Vector2Int(1, 0), rowCount, columnCount, wrap)
                };

                origin.navigation = nav;
                LogGridNavigationLinks(origin, nav, "layout", $" [row {originPosition.y}, col {originPosition.x}]");
            }

            LogInfo($"[UISelectableGroup] {name}: Configured GridLayoutGroup-based navigation on {gridLayoutRoot.name} with {activeChildCount} active children, {rowCount} rows and {columnCount} columns.");

            return true;
        }

        /// <summary>
        /// Forces the UI layout system to place tiles before spatial neighbors are calculated.
        /// </summary>
        private void RefreshLayoutForGridNavigation(List<Selectable> selectables)
        {
            Canvas.ForceUpdateCanvases();

            if (transform is RectTransform groupRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(groupRectTransform);
            }

            HashSet<RectTransform> rebuiltParents = new HashSet<RectTransform>();
            foreach (var selectable in selectables)
            {
                if (selectable == null) continue;

                RectTransform parentRectTransform = selectable.transform.parent as RectTransform;
                if (parentRectTransform != null && rebuiltParents.Add(parentRectTransform))
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
                }
            }
        }

        /// <summary>
        /// Finds a shared GridLayoutGroup ancestor for all selectables.
        /// </summary>
        private bool TryGetSharedGridLayout(List<Selectable> selectables, out GridLayoutGroup gridLayoutGroup, out RectTransform gridLayoutRoot)
        {
            gridLayoutGroup = null;
            gridLayoutRoot = null;

            Selectable firstSelectable = null;
            foreach (var selectable in selectables)
            {
                if (selectable != null)
                {
                    firstSelectable = selectable;
                    break;
                }
            }

            if (firstSelectable == null)
            {
                return false;
            }

            Transform current = firstSelectable.transform;
            while (current != null)
            {
                if (current is RectTransform rectTransform)
                {
                    GridLayoutGroup candidate = current.GetComponent<GridLayoutGroup>();
                    if (candidate != null)
                    {
                        bool allShareCandidate = true;
                        foreach (var selectable in selectables)
                        {
                            if (selectable == null) continue;

                            if (GetGridLayoutChild(selectable, rectTransform) == null)
                            {
                                allShareCandidate = false;
                                break;
                            }
                        }

                        if (allShareCandidate)
                        {
                            gridLayoutGroup = candidate;
                            gridLayoutRoot = rectTransform;
                            return true;
                        }
                    }
                }

                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Gets the direct GridLayoutGroup child that owns a selectable.
        /// </summary>
        private RectTransform GetGridLayoutChild(Selectable selectable, RectTransform gridLayoutRoot)
        {
            if (selectable == null || gridLayoutRoot == null)
            {
                return null;
            }

            Transform current = selectable.transform;
            while (current != null)
            {
                if (current.parent == gridLayoutRoot)
                {
                    return current as RectTransform;
                }

                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// Counts the active direct children under a GridLayoutGroup root.
        /// </summary>
        private int GetActiveGridChildCount(RectTransform gridLayoutRoot)
        {
            if (gridLayoutRoot == null)
            {
                return 0;
            }

            int activeChildCount = 0;
            for (int i = 0; i < gridLayoutRoot.childCount; i++)
            {
                if (gridLayoutRoot.GetChild(i).gameObject.activeInHierarchy)
                {
                    activeChildCount++;
                }
            }

            return activeChildCount;
        }

        /// <summary>
        /// Gets the position of a child among active GridLayoutGroup children only.
        /// </summary>
        private int GetActiveGridSiblingIndex(RectTransform gridLayoutRoot, RectTransform layoutChild)
        {
            if (gridLayoutRoot == null || layoutChild == null)
            {
                return -1;
            }

            int activeIndex = 0;
            for (int i = 0; i < gridLayoutRoot.childCount; i++)
            {
                Transform child = gridLayoutRoot.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child == layoutChild)
                {
                    return activeIndex;
                }

                activeIndex++;
            }

            return -1;
        }

        /// <summary>
        /// Resolves the logical grid dimensions from a GridLayoutGroup.
        /// </summary>
        private bool TryGetGridDimensions(GridLayoutGroup gridLayoutGroup, int childCount, out int rowCount, out int columnCount)
        {
            rowCount = 0;
            columnCount = 0;

            if (gridLayoutGroup == null || childCount <= 0)
            {
                return false;
            }

            switch (gridLayoutGroup.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    columnCount = Mathf.Max(1, gridLayoutGroup.constraintCount);
                    rowCount = Mathf.CeilToInt((float)childCount / columnCount);
                    return true;

                case GridLayoutGroup.Constraint.FixedRowCount:
                    rowCount = Mathf.Max(1, gridLayoutGroup.constraintCount);
                    columnCount = Mathf.CeilToInt((float)childCount / rowCount);
                    return true;

                case GridLayoutGroup.Constraint.Flexible:
                
                    RectTransform rect = gridLayoutGroup.GetComponent<RectTransform>();

                    float availableWidth =
                        rect.rect.width -
                        gridLayoutGroup.padding.left -
                        gridLayoutGroup.padding.right;

                    float cellWidth =
                        gridLayoutGroup.cellSize.x +
                        gridLayoutGroup.spacing.x;

                    columnCount = Mathf.Max(
                        1,
                        Mathf.FloorToInt(
                            (availableWidth + gridLayoutGroup.spacing.x) /
                            cellWidth));

                    rowCount = Mathf.CeilToInt((float)childCount / columnCount);

                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Maps a GridLayoutGroup child sibling index to a visible row and column.
        /// </summary>
        private Vector2Int GetGridPositionFromSiblingIndex(int siblingIndex, int rowCount, int columnCount, GridLayoutGroup gridLayoutGroup)
        {
            int row;
            int column;

            if (gridLayoutGroup.startAxis == GridLayoutGroup.Axis.Horizontal)
            {
                row = siblingIndex / columnCount;
                column = siblingIndex % columnCount;
            }
            else
            {
                column = siblingIndex / rowCount;
                row = siblingIndex % rowCount;
            }

            if (gridLayoutGroup.startCorner == GridLayoutGroup.Corner.UpperRight ||
                gridLayoutGroup.startCorner == GridLayoutGroup.Corner.LowerRight)
            {
                column = columnCount - 1 - column;
            }

            if (gridLayoutGroup.startCorner == GridLayoutGroup.Corner.LowerLeft ||
                gridLayoutGroup.startCorner == GridLayoutGroup.Corner.LowerRight)
            {
                row = rowCount - 1 - row;
            }

            return new Vector2Int(column, row);
        }

        /// <summary>
        /// Finds the next selectable in a logical grid direction, skipping empty or non-selectable slots.
        /// </summary>
        private Selectable FindSelectableInGridDirection(Selectable origin, Vector2Int originPosition, Dictionary<Vector2Int, Selectable> selectablesByGridPosition, Vector2Int step, int rowCount, int columnCount, bool wrap)
        {
            int maxSteps = step.x != 0 ? columnCount - 1 : rowCount - 1;

            for (int i = 1; i <= maxSteps; i++)
            {
                Vector2Int candidatePosition = originPosition + (step * i);
                if (!IsGridPositionInBounds(candidatePosition, rowCount, columnCount))
                {
                    break;
                }

                if (selectablesByGridPosition.TryGetValue(candidatePosition, out Selectable candidate) && candidate != origin)
                {
                    return candidate;
                }
            }

            if (!wrap)
            {
                return null;
            }

            for (int i = 1; i <= maxSteps; i++)
            {
                Vector2Int wrappedPosition = new Vector2Int(
                    WrapIndex(originPosition.x + (step.x * i), columnCount),
                    WrapIndex(originPosition.y + (step.y * i), rowCount));

                if (wrappedPosition == originPosition)
                {
                    continue;
                }

                if (selectablesByGridPosition.TryGetValue(wrappedPosition, out Selectable candidate) && candidate != origin)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns whether a logical grid position is inside the grid bounds.
        /// </summary>
        private bool IsGridPositionInBounds(Vector2Int gridPosition, int rowCount, int columnCount)
        {
            return gridPosition.x >= 0 && gridPosition.x < columnCount &&
                   gridPosition.y >= 0 && gridPosition.y < rowCount;
        }

        /// <summary>
        /// Wraps a grid index into the valid range.
        /// </summary>
        private int WrapIndex(int index, int size)
        {
            if (size <= 0)
            {
                return 0;
            }

            int wrapped = index % size;
            return wrapped < 0 ? wrapped + size : wrapped;
        }

        /// <summary>
        /// Logs the resolved neighbors for a grid selectable when debug mode is enabled.
        /// </summary>
        private void LogGridNavigationLinks(Selectable selectable, UnityEngine.UI.Navigation navigation, string mode, string extraContext)
        {
            if (!_debugMode || selectable == null)
            {
                return;
            }

            string up = navigation.selectOnUp != null ? navigation.selectOnUp.name : "none";
            string down = navigation.selectOnDown != null ? navigation.selectOnDown.name : "none";
            string left = navigation.selectOnLeft != null ? navigation.selectOnLeft.name : "none";
            string right = navigation.selectOnRight != null ? navigation.selectOnRight.name : "none";

            LogInfo($"[UISelectableGroup] {name}: Grid({mode}) nav for {selectable.name}{extraContext} -> Up:{up}, Down:{down}, Left:{left}, Right:{right}");
        }

        /// <summary>
        /// Logs an informational navigation message to the console and the persistent log file.
        /// </summary>
        private void LogInfo(string message)
        {
            //Debug.Log(message);
            AppendNavigationLog("INFO", message);
        }

        /// <summary>
        /// Logs a warning navigation message to the console and the persistent log file.
        /// </summary>
        private void LogWarning(string message)
        {
            Debug.LogWarning(message);
            AppendNavigationLog("WARN", message);
        }

        /// <summary>
        /// Appends a navigation log line to the persistent log file.
        /// </summary>
        private void AppendNavigationLog(string level, string message)
        {
            try
            {
                EnsureNavigationLogFileInitialized();

                string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(GetNavigationLogFilePath(), logLine);
            }
            catch (Exception ex)
            {
                if (_hasReportedNavigationLogWriteFailure)
                {
                    return;
                }

                _hasReportedNavigationLogWriteFailure = true;
                Debug.LogWarning($"[UISelectableGroup] Failed to write navigation log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates the navigation log file and writes a session header when needed.
        /// </summary>
        private void EnsureNavigationLogFileInitialized()
        {
            if (_hasInitializedNavigationLogFile)
            {
                return;
            }

            string logFilePath = GetNavigationLogFilePath();
            string logDirectory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string header = $"===== UISelectableGroup Navigation Session {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | File: {logFilePath} ====={Environment.NewLine}";
            File.AppendAllText(logFilePath, header);

            _hasInitializedNavigationLogFile = true;
            Debug.Log($"[UISelectableGroup] Writing navigation logs to: {logFilePath}");
        }

        /// <summary>
        /// Gets the persistent path used for UISelectableGroup navigation logs.
        /// </summary>
        private string GetNavigationLogFilePath()
        {
            return Path.Combine(Application.persistentDataPath, "DebugLogs", "UISelectableGroupNavigation.log");
        }

        /// <summary>
        /// Finds the best selectable in the requested direction while keeping movement aligned.
        /// </summary>
        private Selectable FindDirectionalSelectable(Selectable origin, List<Selectable> selectables, Vector2 direction, bool wrap)
        {
            if (!TryGetSelectablePosition(origin, out Vector2 originPosition))
            {
                return null;
            }

            Selectable bestForward = null;
            float bestForwardPerpendicularOffset = float.MaxValue;
            float bestForwardDistance = float.MaxValue;

            Selectable bestWrap = null;
            float bestWrapPerpendicularOffset = float.MaxValue;
            float bestWrapDistance = float.MinValue;

            foreach (var candidate in selectables)
            {
                if (candidate == null || candidate == origin) continue;
                if (!candidate.gameObject.activeInHierarchy || !candidate.IsInteractable()) continue;
                if (!TryGetSelectablePosition(candidate, out Vector2 candidatePosition)) continue;

                Vector2 offset = candidatePosition - originPosition;
                float forwardAmount = Vector2.Dot(offset, direction);
                if (Mathf.Approximately(forwardAmount, 0f)) continue;

                Vector2 perpendicularDirection = new Vector2(-direction.y, direction.x);
                float perpendicularOffset = Mathf.Abs(Vector2.Dot(offset, perpendicularDirection));
                float directionalDistance = Mathf.Abs(forwardAmount);

                if (forwardAmount > 0f)
                {
                    if (perpendicularOffset < bestForwardPerpendicularOffset ||
                        (Mathf.Approximately(perpendicularOffset, bestForwardPerpendicularOffset) && directionalDistance < bestForwardDistance))
                    {
                        bestForward = candidate;
                        bestForwardPerpendicularOffset = perpendicularOffset;
                        bestForwardDistance = directionalDistance;
                    }
                }
                else if (wrap)
                {
                    if (perpendicularOffset < bestWrapPerpendicularOffset ||
                        (Mathf.Approximately(perpendicularOffset, bestWrapPerpendicularOffset) && directionalDistance > bestWrapDistance))
                    {
                        bestWrap = candidate;
                        bestWrapPerpendicularOffset = perpendicularOffset;
                        bestWrapDistance = directionalDistance;
                    }
                }
            }

            return bestForward != null ? bestForward : bestWrap;
        }

        /// <summary>
        /// Gets the world-space center position for a selectable.
        /// </summary>
        private bool TryGetSelectablePosition(Selectable selectable, out Vector2 position)
        {
            position = Vector2.zero;
            if (selectable == null) return false;

            if (selectable.transform is RectTransform rectTransform)
            {
                position = rectTransform.TransformPoint(rectTransform.rect.center);
                return true;
            }

            position = selectable.transform.position;
            return true;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Refresh Selectables")]
        private void EditorRefreshSelectables()
        {
            RefreshSelectables();
            LogInfo($"[UISelectableGroup] {name}: Found {_selectables.Count} selectables, {_subGroups.Count} sub-groups.");
        }

        [ContextMenu("Auto-Populate Manual List from Children")]
        private void EditorAutoPopulateManualList()
        {
            _manualSelectableList.Clear();
            var allSelectables = GetComponentsInChildren<Selectable>(true);
            foreach (var s in allSelectables)
            {
                if (s != null)
                {
                    _manualSelectableList.Add(s);
                }
            }
            LogInfo($"[UISelectableGroup] Added {_manualSelectableList.Count} selectables to manual list. Reorder as needed.");
        }
#endif

        #endregion
    }
}
