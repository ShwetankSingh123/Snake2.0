# UI Navigation System - Setup Guide

This document explains how to set up and use the keyboard/gamepad navigation system for Unity UI.

## Overview

The navigation system adds full keyboard and controller support to your existing UI while preserving all mouse and touch functionality. It works with Unity's EventSystem and supports both the Legacy Input Manager and new Input System.

## Scripts Created

| Script | Purpose |
|--------|---------|
| `UINavigationManager` | Singleton that manages input mode detection, group stacking, and auto-selection |
| `UISelectableGroup` | Defines a group of UI elements (panel/menu) with automatic navigation setup |
| `UIFocusVisualizer` | Provides per-element visual feedback (scale, outline, glow) when focused |
| `UISelectableEnhancer` | Adds submit/cancel handling to selectables (especially CustomButton) |
| `UIInputModuleSetup` | Configures EventSystem's InputModule for proper keyboard/gamepad support |
| `UISelectionHighlighter` | **Global highlight system** - single highlight object that follows selection |
| `UISelectableTarget` | Optional per-element overrides for the global highlight (padding, size, etc.) |

---

## Two Highlight Approaches

The system provides **two different approaches** for selection highlighting:

### Approach 1: Per-Element (UIFocusVisualizer)
- Add `UIFocusVisualizer` component to each button
- Each element has its own highlight effect
- Good for: Custom effects per button, complex animations

### Approach 2: Global Highlight (UISelectionHighlighter) ⭐ Recommended
- Single highlight object moves between selections
- More efficient (one object vs hundreds)
- Smoother transitions between elements
- Works automatically with any selectable
- Good for: Consistent highlight style, performance, less setup

---

## Quick Setup (5 Steps)

### Step 1: EventSystem Configuration

Ensure your scene has an **EventSystem** with the correct InputModule:

1. Go to your scene's **EventSystem** GameObject
2. If using **Legacy Input Manager**:
   - Ensure it has `StandaloneInputModule` component
   - Settings should be:
     - Horizontal Axis: `Horizontal`
     - Vertical Axis: `Vertical`
     - Submit Button: `Submit`
     - Cancel Button: `Cancel`
     
3. If using **New Input System**:
   - Use `InputSystemUIInputModule` instead
   - Configure your Input Actions for navigation

**Alternative:** Add `UIInputModuleSetup` component to EventSystem for automatic configuration.

### Step 2: Add UINavigationManager

1. Create an empty GameObject named `UINavigationManager`
2. Add the `UINavigationManager` component
3. Configure settings:
   - **Auto Select On Input**: ✓ Enabled (selects a UI element when keyboard input detected)
   - **Debug Mode**: Enable during setup to see input mode in corner

The manager is a singleton and persists across scenes via DontDestroyOnLoad.

### Step 3: Add UISelectableGroup to Panels

For each menu/panel that should be independently navigable:

1. Select the panel's parent GameObject (e.g., `SettingsPanel`, `MainMenuPanel`)
2. Add `UISelectableGroup` component
3. Configure:
   - **Navigation Mode**: `Automatic` (Unity handles spatial navigation) or `Sequential` (list-style)
    - **Direction**: `Vertical` for up/down menus, `Horizontal` for left/right, `Grid` for four-way movement inside the configured group
   - **Wrap Navigation**: ✓ Enables wraparound from last to first element
   - **Default Selectable**: (Optional) Drag the button to select by default
   - **Remember Last Selection**: ✓ Returns to last selected item when reopening
   - **Auto Register**: ✓ Automatically registers with manager on enable
   - **Is Push Group**: ✓ for popups (pushes onto stack), uncheck for main menus

### Step 4: Add Visual Feedback (Optional but Recommended)

For each selectable that needs visual feedback when focused:

1. Select the Button/Toggle/etc.
2. Add `UIFocusVisualizer` component
3. Configure:
   - **Effect Type**: 
     - `ScalePulse` - Subtle scale up when focused (recommended)
     - `Outline` - Shows outline image
     - `Glow` - Shows glow via CanvasGroup
     - `ScaleAndOutline` - Combines effects
   - **Focus Scale**: 1.08 (8% larger when focused)
   - **Play Focus Sound**: ✓ (uses existing AudioManager.PlayUIButtonSound)

### Step 5: Add Submit Handling for CustomButtons

Since `CustomButton` has custom click logic, add `UISelectableEnhancer` to ensure Enter/Space triggers clicks:

1. Select your CustomButton
2. Add `UISelectableEnhancer` component
3. Settings:
   - **Handle Submit**: ✓ (Enter/Space triggers onButtonClick)
   - **Handle Cancel**: ✓ and assign Cancel Target button for back/close
   - **Play Submit Sound**: ✓

---

## Input Actions

### Legacy Input Manager (Default)

The system reads from these axes/buttons defined in **Edit > Project Settings > Input Manager**:

| Action | Input |
|--------|-------|
| Navigate Up | Up Arrow, W, Gamepad DPad Up, Left Stick Up |
| Navigate Down | Down Arrow, S, Gamepad DPad Down, Left Stick Down |
| Navigate Left | Left Arrow, A, Gamepad DPad Left, Left Stick Left |
| Navigate Right | Right Arrow, D, Gamepad DPad Right, Left Stick Right |
| Submit | Enter, Space, Gamepad South (A/Cross) |
| Cancel | Escape, Gamepad East (B/Circle) |

### New Input System

If using the new Input System:
1. Replace `StandaloneInputModule` with `InputSystemUIInputModule`
2. Assign your UI Input Actions to the module
3. The UINavigationManager will still detect keyboard vs mouse mode

---

## Programmatic Usage

### Selecting an Element

```csharp
// Select a specific button
UINavigationManager.Instance.SelectUIElement(myButton);

// Clear selection
UINavigationManager.Instance.ClearSelection();
```

### Managing Groups (for Popups)

```csharp
// When opening a popup
UINavigationManager.Instance.PushGroup(popupGroup);

// When closing a popup
UINavigationManager.Instance.PopGroup();

// Clear all groups (e.g., returning to main menu)
UINavigationManager.Instance.ClearGroups();
```

### Checking Input Mode

```csharp
if (UINavigationManager.Instance.IsNavigatingWithKeyboard)
{
    // User is using keyboard/gamepad
}
```

### Setting Global Default

```csharp
// Set fallback selection when no group is active
UINavigationManager.Instance.SetDefaultSelectable(playButton);
```

### Dynamically Refreshing Navigation

```csharp
// After adding/removing UI elements dynamically
myGroup.RefreshSelectables();
```

### Bulk Enhancement

```csharp
// Add enhancers to all selectables in a hierarchy
int added = UISelectableEnhancer.EnhanceAllSelectables(panelTransform);

// Add focus visualizers to all selectables
UISelectableEnhancer.AddFocusVisualizersToAll(panelTransform, UIFocusVisualizer.FocusEffect.ScalePulse);
```

---

## Scene Hierarchy Example

```
Canvas
├── MainMenuPanel                     <- UISelectableGroup (Auto Register, not Push)
│   ├── PlayButton                    <- UISelectableEnhancer + UIFocusVisualizer
│   ├── SettingsButton                <- UISelectableEnhancer + UIFocusVisualizer
│   └── QuitButton                    <- UISelectableEnhancer + UIFocusVisualizer
│
├── SettingsPanel                     <- UISelectableGroup (Is Push Group)
│   ├── MusicSlider                   <- UIFocusVisualizer
│   ├── SFXSlider                     <- UIFocusVisualizer
│   ├── VibrationToggle               <- UIFocusVisualizer
│   └── CloseButton                   <- UISelectableEnhancer (Cancel Target)
│
└── PopupPanel                        <- UISelectableGroup (Is Push Group)
    ├── ConfirmButton                 <- UISelectableEnhancer + UIFocusVisualizer
    └── CancelButton                  <- UISelectableEnhancer (Cancel Target)

EventSystem
├── StandaloneInputModule             <- Configured for navigation
└── UIInputModuleSetup                <- (Optional) Auto-configures module

UINavigationManager                   <- Singleton, DontDestroyOnLoad
```

---

## Complete Inspector Reference

### UINavigationManager

The singleton manager that controls input mode detection and group management.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Auto Select On Input** | bool | true | Automatically selects a UI element when keyboard/gamepad input is detected |
| **Default Selectable** | Selectable | null | Fallback element to select when no group provides a default |
| **Tab Cycles Sub Groups** | bool | true | If true, Tab key cycles through sub-groups within the current group |
| **Debug Mode** | bool | false | Shows input mode indicator in corner and logs to console |

---

### UISelectableGroup

Defines a navigable group of UI elements. Attach to panel parent GameObjects.

#### Navigation Mode Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Navigation Mode** | enum | Automatic | How navigation is configured: `Automatic` (spatial), `Sequential` (ordered list), `Manual` (keep existing) |
| **Primary Direction** | enum | Vertical | Direction for explicit navigation: `Vertical` (up/down), `Horizontal` (left/right), or `Grid` (four-way spatial navigation inside the group) |
| **Wrap Navigation** | bool | true | Allows wraparound from last to first element |

#### Manual Selectable List Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Use Manual List** | bool | false | If true, uses manually ordered list instead of auto-detecting selectables |
| **Manual Selectable List** | List | empty | Ordered list of selectables when using manual mode |

#### Sub-Groups Section (for Complex Layouts)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Use Sub Groups** | bool | false | Enable sub-groups for panels with multiple button clusters |
| **Sub Groups** | List | empty | List of NavigationSubGroup objects |

**NavigationSubGroup Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Group Name** | string | "SubGroup" | Name for identification (used in code and debugging) |
| **Is Enabled** | bool | true | If false, this sub-group is skipped during Tab cycling |
| **Selectables** | List | empty | Ordered list of selectables in this sub-group |
| **Default Selectable** | Selectable | null | Default selectable when this sub-group is activated |
| **Direction** | enum | Vertical | Navigation direction within this sub-group: `Vertical`, `Horizontal`, or `Grid` |
| **Wrap Navigation** | bool | true | Wrap navigation within this sub-group |

#### Default Selection Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Default Selectable** | Selectable | null | The selectable to select by default when this group becomes active |
| **Remember Last Selection** | bool | true | Remembers the last selected item and returns to it |

#### Auto-Registration Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Auto Register** | bool | true | Automatically registers with UINavigationManager when enabled |
| **Is Push Group** | bool | false | If true, registers as a pushed group (for popups that stack) |

#### Auto-Enhance Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Auto Enhance Selectables** | bool | true | Automatically adds UISelectableEnhancer to selectables added to this group |
| **Auto Add Focus Visualizer** | bool | true | Automatically adds UIFocusVisualizer to selectables added to this group |

#### Debug Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Debug Mode** | bool | false | Logs navigation events to console |

---

### UIFocusVisualizer

Provides visual feedback when an element is focused/selected.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Effect Type** | enum | ScalePulse | Type of visual effect: `ScalePulse`, `Outline`, `Glow`, `ScaleAndOutline` |
| **Focus Scale** | float | 1.08 | Scale multiplier when focused (for ScalePulse effects) |
| **Animation Duration** | float | 0.15 | Duration of focus/unfocus animation in seconds |
| **Play Focus Sound** | bool | false | Plays sound when element receives focus |
| **Only Show In Keyboard Mode** | bool | true | Only shows visual feedback when using keyboard/gamepad |
| **Outline Image** | Image | null | Image component to show as outline (for Outline effect) |
| **Glow Canvas Group** | CanvasGroup | null | CanvasGroup to fade in/out (for Glow effect) |

---

### UISelectableEnhancer

Adds submit/cancel handling for selectables (required for CustomButton).

#### Submit Handling Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Handle Submit** | bool | true | If true, Enter/Space triggers the button's OnClick event |

#### Cancel Handling Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Handle Cancel** | bool | false | If true, Escape/B button invokes the cancel action |
| **Cancel Target** | Button | null | The button to click when cancel is pressed (e.g., close/back button) |

#### Mouse Interaction Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Select On Hover** | bool | false | If true, mouse hover selects this element (for hybrid input) |
| **Hover Select Delay** | float | 0.1 | Delay before selecting on hover to prevent accidental selections |

#### Sound Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Play Submit Sound** | bool | true | Plays a sound on submit action |

---

### UIInputModuleSetup

Auto-configures EventSystem's InputModule (optional utility script).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Configure On Awake** | bool | true | Automatically configures InputModule when script awakens |
| **Horizontal Axis** | string | "Horizontal" | Input axis for horizontal navigation |
| **Vertical Axis** | string | "Vertical" | Input axis for vertical navigation |
| **Submit Button** | string | "Submit" | Input button for submit action |
| **Cancel Button** | string | "Cancel" | Input button for cancel action |

---

## Sub-Groups: Advanced Navigation for Complex Layouts

Sub-groups are essential when your panel has multiple clusters of buttons that should be navigated separately. For example, a shop panel with:
- Close button (top corner)
- Tab header buttons (Coins, Diamonds, Cars)
- Shop item buttons (dynamically generated)

### Setting Up Sub-Groups

1. On your `UISelectableGroup`, enable **Use Sub Groups**
2. Add sub-groups to the list:

```
Sub-Groups:
├── [0] Close
│   ├── Group Name: "Close"
│   ├── Is Enabled: ✓
│   └── Selectables: [CloseButton]
│
├── [1] Header  
│   ├── Group Name: "Header"
│   ├── Is Enabled: ✓
│   ├── Direction: Horizontal
│   └── Selectables: [CoinsTabBtn, DiamondsTabBtn, CarsTabBtn]
│
├── [2] CoinItems
│   ├── Group Name: "CoinItems"
│   ├── Is Enabled: ✓
│   ├── Direction: Horizontal
│   └── Selectables: [] (populated at runtime)
│
├── [3] DiamondItems
│   ├── Group Name: "DiamondItems"
│   ├── Is Enabled: false (disabled by default)
│   └── Selectables: [] (populated at runtime)
│
└── [4] CarItems
    ├── Group Name: "CarItems"
    ├── Is Enabled: false (disabled by default)
    └── Selectables: [] (populated at runtime)
```

### Tab Cycling Between Sub-Groups

When **Tab Cycles Sub Groups** is enabled on UINavigationManager:
- **Tab**: Moves to next enabled sub-group
- **Shift+Tab**: Moves to previous enabled sub-group
- Arrow keys: Navigate within the current sub-group

### Enabling/Disabling Sub-Groups at Runtime

Use this when certain sub-groups should only be navigable based on context (e.g., active tab):

```csharp
// Enable only specific sub-groups (all others disabled)
_shopNavGroup.SetOnlySubGroupsEnabled("Close", "Header", "CoinItems");

// Enable/disable individual sub-groups
_shopNavGroup.SetSubGroupEnabled("DiamondItems", true);
_shopNavGroup.SetSubGroupEnabled("CoinItems", false);

// By index
_shopNavGroup.SetSubGroupEnabled(2, true);
```

---

## Dynamic Population: Adding Selectables at Runtime

When UI elements are instantiated at runtime (shop items, inventory slots, etc.), use these methods to add them to navigation.

### Adding to Sub-Groups

```csharp
// Get reference to the navigation group
[SerializeField] private UISelectableGroup _navGroup;
[SerializeField] private string _subGroupName = "ShopItems";

// After instantiating a prefab with a button:
GameObject cardObject = Instantiate(cardPrefab, container);
CustomButton purchaseButton = cardObject.GetComponent<CardUI>().GetPurchaseButton();

// Add single selectable
_navGroup.AddToSubGroup(_subGroupName, purchaseButton);

// Or add multiple at once (more efficient)
List<Selectable> buttons = new List<Selectable>();
foreach (var item in items)
{
    var card = Instantiate(cardPrefab, container);
    buttons.Add(card.GetComponent<CardUI>().GetPurchaseButton());
}
_navGroup.AddRangeToSubGroup(_subGroupName, buttons);

// Set the default for the sub-group
int index = _navGroup.GetSubGroupIndexByName(_subGroupName);
_navGroup.SetSubGroupDefault(index, buttons[0]);
```

### Clearing Sub-Groups Before Repopulating

```csharp
// Clear existing items before refreshing
_navGroup.ClearSubGroup(_subGroupName);

// Then repopulate...
```

### Removing Individual Items

```csharp
// When destroying a dynamically created item
_navGroup.RemoveFromSubGroup(selectableToRemove);
```

### Auto-Enhancement

When selectables are added via `AddToSubGroup` or `AddRangeToSubGroup`:
- **UISelectableEnhancer** is automatically added (if Auto Enhance Selectables is true)
- **UIFocusVisualizer** is automatically added (if Auto Add Focus Visualizer is true)

This means Enter/Space will work on dynamically added buttons without manual setup!

---

## Integration Example: Shop Panel

Here's a complete example showing how to integrate navigation with a shop that has tabs and dynamically generated items.

### ShopPremiumMasterPanel.cs (Tab Switching)

```csharp
using Almace.CustomUI.Navigation;

public class ShopPremiumMasterPanel : MonoBehaviour
{
    [Header("Keyboard Navigation")]
    [SerializeField] private UISelectableGroup _shopNavGroup;
    [SerializeField] private string _closeSubGroupName = "Close";
    [SerializeField] private string _headerSubGroupName = "Header";
    [SerializeField] private string _coinSubGroupName = "CoinItems";
    [SerializeField] private string _diamondSubGroupName = "DiamondItems";
    [SerializeField] private string _carSubGroupName = "CarItems";

    public void SwitchToTab(Tab tab)
    {
        _activeTab = tab;
        UpdateTabVisuals();
        UpdateNavigationSubGroups(tab);
        
        // ... rest of tab switch logic
    }

    private void UpdateNavigationSubGroups(Tab tab)
    {
        if (_shopNavGroup == null) return;

        switch (tab)
        {
            case Tab.Coins:
                _shopNavGroup.SetOnlySubGroupsEnabled(
                    _closeSubGroupName, _headerSubGroupName, _coinSubGroupName);
                break;

            case Tab.Diamonds:
                _shopNavGroup.SetOnlySubGroupsEnabled(
                    _closeSubGroupName, _headerSubGroupName, _diamondSubGroupName);
                break;

            case Tab.PremiumCar:
                _shopNavGroup.SetOnlySubGroupsEnabled(
                    _closeSubGroupName, _headerSubGroupName, _carSubGroupName);
                break;
        }
    }
}
```

### ShopBundleHandler.cs (Dynamic Item Population)

```csharp
using Almace.CustomUI.Navigation;

public class ShopBundleHandler : MonoBehaviour
{
    [Header("Keyboard Navigation")]
    [SerializeField] private UISelectableGroup _shopNavGroup;
    [SerializeField] private string _coinSubGroupName = "CoinItems";
    [SerializeField] private string _diamondSubGroupName = "DiamondItems";

    private void PopulateShop()
    {
        // Clear existing navigation entries
        if (_shopNavGroup != null)
        {
            _shopNavGroup.ClearSubGroup(_coinSubGroupName);
            _shopNavGroup.ClearSubGroup(_diamondSubGroupName);
        }

        List<Selectable> coinSelectables = new List<Selectable>();
        List<Selectable> diamondSelectables = new List<Selectable>();

        // Instantiate coin items
        foreach (var item in coinItems)
        {
            var cardObject = CreateShopCard(item, _coinsContent);
            var cardUI = cardObject.GetComponent<ShopItemCardUI>();
            if (cardUI != null)
            {
                var purchaseButton = cardUI.GetPurchaseButton();
                if (purchaseButton != null)
                {
                    coinSelectables.Add(purchaseButton);
                }
            }
        }

        // Add to navigation
        if (_shopNavGroup != null && coinSelectables.Count > 0)
        {
            int coinIndex = _shopNavGroup.GetSubGroupIndexByName(_coinSubGroupName);
            if (coinIndex >= 0)
            {
                _shopNavGroup.AddRangeToSubGroup(coinIndex, coinSelectables);
                _shopNavGroup.SetSubGroupDefault(coinIndex, coinSelectables[0]);
            }
        }

        // Repeat for diamond items...
    }
}
```

### Card UI Script (Exposing Purchase Button)

```csharp
public class ShopItemCardUI : MonoBehaviour
{
    [SerializeField] private CustomButton purchaseButton;

    /// <summary>
    /// Returns the purchase button for keyboard/gamepad navigation setup.
    /// </summary>
    public CustomButton GetPurchaseButton() => purchaseButton;
}
```

---

## Troubleshooting

### Navigation Not Working

1. **Check EventSystem**: Ensure scene has EventSystem with InputModule
2. **Check Navigation Mode**: Selectables should not have Navigation Mode = None
3. **Check Interactable**: Buttons must be interactable
4. **Check Canvas Raycast**: Ensure Canvas has GraphicRaycaster

### Submit Not Triggering CustomButton

1. Add `UISelectableEnhancer` component with Handle Submit enabled
2. Or enable **Auto Enhance Selectables** on UISelectableGroup (enabled by default)

### Focus Visual Not Showing

1. Check `Only Show In Keyboard Mode` - may be hiding in mouse mode
2. Verify element is actually selected in EventSystem
3. Enable Debug Mode on UINavigationManager to see selection state

### Tab Not Cycling Through Expected Sub-Groups

1. Check **Is Enabled** on each sub-group
2. Use `SetOnlySubGroupsEnabled()` when switching contexts
3. Enable Debug Mode to see which sub-groups are enabled

### Dynamically Added Buttons Don't Respond to Enter

1. Ensure **Auto Enhance Selectables** is true on UISelectableGroup
2. Or manually add UISelectableEnhancer to buttons before adding to group
3. Check that the button's `onButtonClick` event has listeners

### Mouse Breaks After Using Keyboard

This is by design - the system seamlessly switches modes. If you want mouse clicks to work while navigating with keyboard, ensure buttons still have OnClick listeners (they do by default).

### Gamepad Not Working

1. Verify controller is connected
2. Check Input Manager has Joystick axes configured
3. Try adding explicit joystick button mappings

---

## Best Practices

1. **Don't Remove Existing Listeners**: The system adds navigation on top of existing functionality
2. **Use Auto Navigation**: For most panels, `Automatic` navigation mode works well
3. **Set Default Selectables**: Always set a default for each group to ensure something is selected
4. **Test Both Modes**: Test with both mouse and keyboard to ensure seamless switching
5. **Group Popups Correctly**: Mark popup panels as "Is Push Group" so they stack properly
6. **Keep Focus Subtle**: Use 1.05-1.1x scale for focus feedback - too large feels jarring
7. **Use Sub-Groups for Complex Layouts**: Don't try to navigate everything in one flat list
8. **Clear Before Repopulating**: Always clear sub-groups before adding new dynamic items
9. **Match Sub-Group Names**: Keep names consistent between handler scripts and master panel
10. **Enable Debug Mode During Development**: Helps visualize navigation flow
11. **Use Global Highlighter**: Prefer UISelectionHighlighter over UIFocusVisualizer for consistent, efficient highlights

---

## Global Selection Highlight System

The `UISelectionHighlighter` provides a more efficient alternative to per-element highlighting. Instead of adding components to every button, a single highlight object follows the currently selected element.

### Setting Up the Global Highlighter

#### Step 1: Create the Highlight Object

1. In your root Canvas, create a new child: **Create > UI > Image**
2. Name it `SelectionHighlight`
3. Configure the Image:
   - **Source Image**: Use a 9-sliced rounded rectangle sprite
   - **Image Type**: Sliced (for proper corner handling)
   - **Color**: Your highlight color with some transparency (e.g., `#FFD700` at 80%)
   - **Raycast Target**: ❌ Disabled (so it doesn't block clicks)
4. Add a **CanvasGroup** component (for fading)
5. **Important**: Set the highlight's **Sorting Order** to render on top:
   - Either put it at the bottom of the Canvas hierarchy (renders last)
   - Or use a second Canvas with higher Sort Order

#### Step 2: Add the Highlighter Component

1. Add `UISelectionHighlighter` component to the highlight GameObject
2. Assign references:
   - **Highlight Rect**: The RectTransform of the highlight
   - **Highlight Image**: The Image component
   - **Highlight Canvas Group**: The CanvasGroup component

#### Step 3: Configure Settings

| Property | Recommended Value | Description |
|----------|-------------------|-------------|
| **Move Duration** | 0.12 - 0.18 | Faster = snappier, Slower = smoother |
| **Resize Duration** | 0.10 - 0.15 | How fast the highlight resizes |
| **Fade Duration** | 0.08 - 0.12 | Show/hide fade speed |
| **Default Padding** | (8, 8, 8, 8) | Extra space around elements |
| **Minimum Size** | (50, 30) | Prevents highlight from being too small |
| **Enable Pulse** | ✓ | Subtle breathing animation |
| **Pulse Scale** | 1.02 - 1.05 | How much the pulse enlarges |
| **Hide On Mouse Input** | ✓ | Hides when using mouse |

### UISelectionHighlighter Inspector Reference

#### Highlight Visual Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Highlight Rect** | RectTransform | required | The highlight's RectTransform |
| **Highlight Image** | Image | optional | Image component for color fading |
| **Highlight Canvas Group** | CanvasGroup | optional | CanvasGroup for alpha fading (preferred) |

#### Animation Settings Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Move Duration** | float | 0.15 | Duration for highlight to move to new target |
| **Resize Duration** | float | 0.12 | Duration for highlight to resize |
| **Fade Duration** | float | 0.1 | Duration for fade in/out |
| **Move Ease** | Ease | OutQuad | DOTween easing for movement |
| **Resize Ease** | Ease | OutQuad | DOTween easing for resizing |

#### Padding & Sizing Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Default Padding** | Vector4 | (8,8,8,8) | Extra space around elements (L,B,R,T) |
| **Minimum Size** | Vector2 | (50,30) | Minimum highlight dimensions |

#### Pulse Animation Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Enable Pulse** | bool | true | Enable breathing animation |
| **Pulse Scale** | float | 1.03 | Scale multiplier for pulse |
| **Pulse Duration** | float | 0.8 | Duration of one pulse cycle |

#### Visibility Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Hide On Mouse Input** | bool | true | Hide highlight when using mouse/touch |
| **Mouse Hide Delay** | float | 0.1 | Delay before hiding after mouse detected |

#### Scroll View Support Section

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Support Scroll Views** | bool | true | Track elements inside scroll views |
| **Scroll View Update Interval** | float | 0.05 | Position update frequency in scroll views |

---

### UISelectableTarget: Per-Element Overrides

Add `UISelectableTarget` to specific elements that need custom highlight behavior.

```
Button (with special padding)
└── UISelectableTarget
    ├── Use Custom Padding: ✓
    └── Custom Padding: (12, 12, 12, 12)
```

#### UISelectableTarget Inspector Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| **Use Custom Padding** | bool | false | Use custom padding for this element |
| **Custom Padding** | Vector4 | (8,8,8,8) | Custom padding (L,B,R,T) |
| **Use Custom Min Size** | bool | false | Use custom minimum size |
| **Custom Min Size** | Vector2 | (50,30) | Custom minimum highlight size |
| **Target Override** | RectTransform | null | Use different element's bounds |
| **Skip Highlight** | bool | false | Don't highlight this element |
| **Disable Pulse** | bool | false | Disable pulse for this element |
| **Use Custom Animation** | bool | false | Use custom animation speeds |
| **Custom Move Duration** | float | 0.15 | Custom move duration |
| **Custom Resize Duration** | float | 0.12 | Custom resize duration |

### Programmatic API

```csharp
// Force highlight to a specific element
UISelectionHighlighter.Instance.ForceHighlight(myButton.gameObject, animate: true);

// Refresh highlight position (after element resize)
UISelectionHighlighter.Instance.RefreshHighlight();

// Temporarily disable/enable
UISelectionHighlighter.Instance.Disable();
UISelectionHighlighter.Instance.Enable();

// Change settings at runtime
UISelectionHighlighter.Instance.SetPadding(new Vector4(10, 10, 10, 10));
UISelectionHighlighter.Instance.SetPulseEnabled(false);
UISelectionHighlighter.Instance.SetKeyboardMode(true);
```

### Creating a 9-Sliced Highlight Sprite

For proper rounded corners that scale correctly:

1. Create a rounded rectangle image (e.g., 64x64 with 16px corner radius)
2. Import into Unity
3. Open Sprite Editor
4. Set **Border** values to define the corners (e.g., L:16, B:16, R:16, T:16)
5. Apply changes
6. On the Image component, set **Image Type** to **Sliced**

### Example Hierarchy

```
Canvas
├── MainMenuPanel
│   ├── PlayButton         <- Automatically highlighted
│   ├── SettingsButton     <- Automatically highlighted
│   └── QuitButton         <- Automatically highlighted
│
├── ShopPanel
│   ├── CloseButton        <- Automatically highlighted
│   ├── TabButtons
│   │   ├── CoinsTab       <- Automatically highlighted
│   │   └── DiamondTab     <- Automatically highlighted
│   └── ItemGrid
│       ├── Item1          <- Automatically highlighted
│       ├── Item2          <- Automatically highlighted
│       └── Item3          <- Automatically highlighted
│
└── SelectionHighlight     <- Single highlight object (UISelectionHighlighter)
    ├── RectTransform
    ├── Image (9-sliced rounded rect)
    └── CanvasGroup

EventSystem
└── StandaloneInputModule
```

---

## Compatibility

- **Unity Version**: 2019.4+ (uses preprocessor directives for 2023+ API changes)
- **Input System**: Supports both Legacy Input Manager and new Input System
- **Render Pipeline**: Works with Built-in, URP, and HDRP
- **Platforms**: PC, Console, Mobile (with connected controllers)

---

## Files Location

All scripts are located in:
```
Assets/_Scripts/CustomUI/Navigation/
├── UINavigationManager.cs      <- Singleton, manages input mode & groups
├── UISelectableGroup.cs        <- Groups selectables, sub-groups, dynamic population
├── UIFocusVisualizer.cs        <- Per-element visual feedback (alternative)
├── UISelectableEnhancer.cs     <- Submit/cancel handling for CustomButton
├── UIInputModuleSetup.cs       <- Auto-configures EventSystem InputModule
├── UISelectionHighlighter.cs   <- Global highlight system (recommended)
├── UISelectableTarget.cs       <- Per-element highlight overrides
└── README_UINavigation.md      <- This documentation
```
