UINavigation — Keyboard & Gamepad UI Navigation (Modular)
======================================================

Overview
--------
This package provides a small, modular UI navigation stack that adds robust keyboard/gamepad navigation and a single global selection highlight to Unity UI. It is intentionally modular so you can drop the pieces you need into any project and wire them to your existing UI systems.

Key goals:
- Provide reliable keyboard/gamepad navigation for menus, settings, and dialogs.
- Use a single global highlighter object for visual selection (efficient and easy to style).
- Allow mouse/touch input to coexist with keyboard navigation.
- Keep the system modular: components are optional and easy to reuse.

Core components
---------------
- UINavigationManager
  - Core manager that tracks input state and active UISelectableGroups.
  - Publishes events: OnSwitchedToKeyboardMode, OnSwitchedToMouseMode, OnRequestExitConfirmation.
  - Handles navigation repeat delays and auto-selection.

- UISelectionHighlighter
  - Global highlight object that smoothly moves/resizes to the currently selected element.
  - Optional mouse-hover support, scroll-view tracking, pulse animation, and padding overrides.

- UIFocusVisualizer
  - Per-selectable visual effects (scale, outline, tint, glow) when an element receives focus.
  - Works alongside the global highlighter or independently.

- UISelectableEnhancer
  - Adds keyboard submit/cancel behavior and optional mouse hover selection.
  - Ensures Enter/Space will trigger CustomButton / Button / Toggle behavior consistently.

- UISelectableGroup
  - Groups and orders selectables for deterministic navigation within a panel.
  - Supports manual lists, sub-groups, wrapping, and default selection.

- UIInputModuleSetup
  - Helper to configure EventSystem / StandaloneInputModule settings for keyboard/gamepad input.

- UINavigationPlatformUtility
  - Platform gate: enables the navigation stack only on supported platforms (Windows / macOS by default)
  - If navigation is not supported, components fall back to default Unity behavior or disable gracefully.

Modularity & Commented Code
---------------------------
Some integrations (audio playback, third-party UI effects) are intentionally commented out in the scripts to keep the core package portable. These include:
- Audio focus sounds (AudioManager calls) in UIFocusVisualizer and UISelectableEnhancer.
- Optional Coffee/UIEffect references in UIFocusVisualizer.

Why commented out?
- Many projects use different audio systems or don't include external UI effect packages. The commented lines show where to hook into your game's systems without forcing a dependency.

How to integrate these optional features
----------------------------------------
- Audio: implement a small IAudioProvider or simply replace commented AudioManager calls with your project's audio singleton.
- UIEffect / third-party visual libs: re-enable references after adding the package or replace with your own Tween/Effect calls.

Quick setup (recommended)
-------------------------
1. Add EventSystem to your scene (or let UIInputModuleSetup create it).
2. Place UINavigationManager on a persistent GameObject (it can auto-create itself).
3. Add UISelectionHighlighter under your root Canvas and set the highlight Rect/Image.
4. Add UIFocusVisualizer and/or UISelectableEnhancer to Selectable UI elements you want animated/handled.
5. Optionally add UISelectableGroup to panels to control group-based navigation.
6. Configure UIInputModuleSetup to match your project's input axes or use the new Input System module.

API & Hooks
-----------
- UINavigationManager.Instance — main manager. Check IsNavigatingWithKeyboard and CurrentGroup.
- UINavigationManager.OnSwitchedToKeyboardMode / OnSwitchedToMouseMode — subscribe to change visuals or clear hover states.
- UISelectionHighlighter.Instance — move/resize is automatic when EventSystem selection changes; you can call Show/Hide or ForceRefresh.
- UISelectableTarget — attach to elements to customize padding, override target Rect, disable pulse, or skip highlighting.

Tips & Best Practices
---------------------
- Keep the highlight as a child of the root Canvas so it correctly overlays UI elements.
- For mobile-first games, the platform gate will disable navigation; still use UIFocusVisualizer or touch handlers as needed.
- Use UISelectableGroup to control logical navigation order for complex menus (subgroups recommended).
- When re-enabling audio or UIEffect integrations, create small adapter classes in your project rather than hard-coupling to external singletons.

Troubleshooting
---------------
- Highlight not appearing: ensure EventSystem.current exists and Canvas render mode / camera is set correctly (highlight must share canvas coordinate space).
- Navigation not responding: verify UIInputModuleSetup or EventSystem input module settings; in Editor run target platform must match UINavigationPlatformUtility gate for some features.

Extending the system
--------------------
- Add a small IHighlightProvider interface if you want to swap the visual implementation at runtime.
- Expose focus sound hooks via events instead of direct AudioManager calls for cleaner decoupling.

License & Credits
-----------------
This navigation package is part of the Snake2.0 project and intended to be modular. Reuse or extract it into other projects — attribute the original author where appropriate.

Contact
-------
For questions about wiring this into your UI or converting commented hooks into adapters, tell me which audio/effect system you use and I can provide concrete adapter code.
