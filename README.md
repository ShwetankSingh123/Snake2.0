# Snake 2.0

A modern, extensible take on the classic Snake game built with Unity. This repository implements a gameplay-first architecture with a single-scene UI, modular managers (Audio, Haptics, Effects), and a small but feature-rich set of special foods and difficulty options.

This README summarizes the project's features, gameplay mechanics, how to run and develop locally, and where to look inside the codebase.

---

## Features

- Classic grid-based Snake gameplay with smooth, configurable movement rate scaling by difficulty.
- Four difficulty levels: Easy, Normal, Hard, Extreme (affects snake moveRate via GameManager).
  - Centralized Difficulty system: DifficultySettings (code presets) drive snake speed, per-food spawn weights, special food lifetimes, and power-up durations so all difficulty tuning is stored in one place.
- Save / Continue: progress is saved to persistent storage and a Continue button appears when a save exists.
- Pause, Resume, and Game Over flows with UI transitions and save handling.
- Special foods (spawned by FoodSpawner) with timers and visual indicators:
  - Normal: baseline food (growth +1 by default)
  - Golden: extra points / larger growth
  - Bomb: immediate penalty / game over if collected
  - Shrink: reduces snake length
  - Speed: temporarily increases snake speed
  - Slow: temporarily decreases snake speed
  - Ghost: temporary phase-through-self state
  - Shield: blocks one death
- Combo system and score multiplier: combo increases score multiplier and decays over time.
- Special food timers shown in the UI with color-coded fills.
- Visual & haptic feedback:
  - Particle VFX and spawn/eat effects (FoodVFX)
  - Screen flash / freeze-frame / time-slow via EffectsManager
  - Screen shake via ScreenShakeManager
  - Haptics abstraction via HapticManager (toggleable via UI)
- Audio management with persistent volume: music and SFX separated; in-game and menu music playback via AudioManager.
- Editor utility to create sample How To Play and Difficulty panels (Tools > Snake2.0 > Create Sample Menus)

---

# Phase 2 — Recent Navigation & Modularity Improvements

The project was updated to improve UI navigation and make integrations modular. Highlights:

- Input System compatibility: the custom UINavigation stack now supports both the legacy Input Manager and the Unity Input System package. A new InputUtils helper safely polls mouse, keyboard, and gamepad input without throwing when the active input handling is switched in Player Settings.
- UI input module detection: UIInputModuleSetup now detects the presence of the Input System UI module reliably and only configures the legacy StandaloneInputModule when appropriate (avoids false positives).
- Modular UI providers: audio and UI-effect integration points were converted to adapter interfaces (IUIAudioProvider, IUIEffectProvider) and a UIProviderLocator so projects can plug in their own audio/effect adapters without editing the navigation code.

See Assets/_Scripts/UI/Navigation/README-UINavigation.md for details and usage examples.

---

## Gameplay Overview

- The snake moves on a grid; the grid bounds are computed dynamically by the `Board` class based on the camera view.
- Movement is time-stepped by `SnakeController.moveRate`. The `GameManager.SetDifficulty` changes `moveRate` profile.
- Eating food increases score (see `Food.GetScore()`), grows/shrinks the snake (see `Food.GetGrowth()`), and may trigger effects.
- ScoreManager manages score and combo state and updates the UI via `UIManager`.

---

## Controls

- Keyboard: Arrow Keys or WASD for movement.
- Mobile / Touch: swipe to change direction (swipe threshold currently configured in SnakeController).
- Esc or the in-game Pause button to open the Pause UI.

---

## UI and Menu Flow

- Main Menu: Continue, New Game (opens difficulty selector), How To Play, Exit. Two icon toggles at bottom-right for Music and Haptics.
- Difficulty Panel: cycle difficulties with left/right arrows, read the description, Start or Back. Selecting a difficulty calls `GameManager.SetDifficulty(...)`. Start begins a new game using the selected difficulty.
- How To Play Panel: scrollable content with objective, controls, and a list of special foods and short descriptions.
- Pause Panel: Resume or return to Main Menu.
- Game Over Panel: shows final score and best score; the game returns to the main menu automatically after a short delay.

---

## Project structure (important files)

- Assets/_Scripts/
  - Managers/
	- GameManager.cs — central game flow and difficulty handling
	- ScoreManager.cs — score and combo logic
		- DifficultySettings.cs — central difficulty presets and tuning (snake speed, spawn weights, lifetimes)
  - Gameplay/
	- SnakeController.cs — movement, growth, input, save/restore
	- FoodSpawner.cs — spawning logic for normal and special foods
	- Food.cs — food metadata (type, lifetime, score, growth)
  - UI/
	- UIManager.cs — all UI panels, menu wiring, music/haptics toggles, timer UI
	- CustomButton.cs — enhanced button with animation and events
  - Audio/
	- AudioManager.cs — music & SFX management
  - Core/
	- Board.cs — grid calculation from camera frustum
	- SaveSystem.cs — simple JSON save/load helper
	- HapticManager.cs — haptics toggle & platform calls
	- EffectsManager.cs — flash, freeze-frame, slow time
	- ScreenShakeManager.cs — camera shake utility
  - Effects/
	- FoodVFX.cs — VFX prefabs for food events

---

## How to run locally

1. Install Unity (recommended stable version used when this was developed; any Unity 2020+ or newer should work but ensure TextMeshPro and DOTween are installed if used by assets).
2. Open Unity Hub and add the project folder `D:/github/Snake2.0`.
3. Open the scene used for the game (e.g., `SampleScene` or the main scene) from the `Assets/Scenes` folder.
4. In the Unity Editor, assign references in the Inspector for these manager components if not already assigned:
   - GameManager: snake, spawner, scoreManager, uiManager
   - UIManager: assign all menu buttons, panels, TMP_Text fields, and icons
   - AudioManager: audio sources and clips
   - Board: configure plane mode and cell size to match SnakeController.cellSize
   - FoodSpawner: assign food prefabs
5. Press Play in the Editor or build a standalone player via `File > Build Settings...`.

Notes: There is a small editor utility under `Tools > Snake2.0 > Create Sample Menus` to scaffold sample HowToPlay and Difficulty panel layouts.

---

## Development notes & where to look for common changes

- Change movement speeds per difficulty in `GameManager` (easySpeed, normalSpeed, hardSpeed, extremeSpeed).
- Adjust how often special foods appear by modifying `normalFoodsBeforeSpecial` or the `specialPool` order in `FoodSpawner`.
- Tweak UI visuals in `UIManager` and button hover sprite assets. The hover system uses `EventTrigger` entries and swaps Button Image sprites.
- Add or change special food behavior in `Food.cs` and respond in `FoodVFX` if you add new types.
- Save data format is `SnakeSaveData` (in `SnakeController.cs`). Save files are exported as JSON to `Application.persistentDataPath/saves/` by `SaveSystem`.

---

## Contributing

Contributions are welcome. Typical areas where maintainers accept PRs:
- Bug fixes for edge cases (board bounds, save/load robustness)
- Improved mobile/touch input handling and multi-platform support
- Additional UI polish (animations, sound on UI interactions)
- New food types, powerups, or modes

Please follow these steps:
1. Fork the repository.
2. Create a feature branch (e.g., `feature/new-food-type`).
3. Implement changes and run the project locally. Fix any build errors.
4. Submit a PR with a clear description and any screenshots or small GIFs demonstrating changes.

---

## License & Credits

- This project was created by the repository owner. Check `LICENSE` if one exists; otherwise ask the project owner for license terms before reusing code.
- Third-party libraries used (if any) should be listed and kept up to date (e.g., DOTween if present in the repo).

---

## A note about README contents

Many game projects include these sections and also add:
- Screenshots and GIFs (top of README) to show gameplay
- Quick start instructions (how to play) and available builds
- Changelog or Releases section
- Known issues and roadmap

If you want, I can:
- Add screenshots (if you provide images) or add placeholder paths for the `README.md`.
- Produce a shorter one-page summary for distribution platforms.
- Generate a CONTRIBUTING.md and a CODE_OF_CONDUCT.md to help open-source contributions.

---

If you want the README adjusted (different tone, shorter, or with screenshots/ badges), tell me what to include and I will update it.
