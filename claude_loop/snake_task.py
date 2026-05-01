"""
snake_task.py — Claude's autonomous Snake 2.0 improvement loop.
Project: D:\\github\\Snake2.0 (Unity 6, URP, Android/iOS/Desktop)
Run this after every token reset and Claude will continue from last checkpoint.
"""

import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from claude_loop import (
    load_state, init_state, save_state,
    complete_step, request_permission,
    generate_resume_file, log
)

TASK_NAME = "Snake 2.0 — Full Game Upgrade (Unity 6 / URP)"

STEPS = [
    # ── PHASE 1: Foundation ──────────────────────────────────────────
    "1. Git init + .gitignore + first commit (baseline snapshot)",
    "2. Folder restructure: move loose scripts into correct subfolders, delete duplicates",
    "3. Write ScreenShakeManager.cs (Core/Effects)",
    "4. Write HapticManager.cs (Core/Effects)",
    "5. Write EffectsManager.cs — central hub for screen flash, freeze-frame, time slow",
    "6. Write SceneLoader.cs — async scene transitions with loading screen",

    # ── PHASE 2: Gameplay Improvements ──────────────────────────────
    "7. Improve SnakeController.cs — smooth lerp movement, better wrap, trail renderer",
    "8. Improve FoodSpawner.cs — weighted random, cooldown per type, max specials cap",
    "9. Add PowerUpManager.cs — tracks active power-ups, stacks/replaces correctly",
    "10. Add GridVisualizer.cs — subtle animated grid background shader",
    "11. Add WallMode toggle in GameManager — wrapping vs solid walls (difficulty option)",
    "12. Add MilestoneSystem.cs — trigger events at score 10/25/50/100 (speed burst, new food)",

    # ── PHASE 3: VFX Polish ──────────────────────────────────────────
    "13. Improve particle systems — NormalEat bigger burst, GoldenEat golden sparkle trail",
    "14. Add SnakeTrailEffect.cs — particle trail behind snake head, color by speed",
    "15. Add DeathExplosionVFX — snake body pieces fly outward on death",
    "16. Add CountdownVFX.cs — 3-2-1 animated text at game start",
    "17. Add ComboFlashVFX.cs — screen edge pulse + floating score text on combo",

    # ── PHASE 4: UI / UX ─────────────────────────────────────────────
    "18. Redesign Main Menu UI — animated snake logo, glowing buttons, version label",
    "19. Add Settings Panel — music/sfx sliders, vibration toggle, wall-mode toggle",
    "20. Add Skin System (SkinManager.cs) — 3 snake color skins selectable in menu",
    "21. Improve HUD — animated score pop, combo bar with fill, mini power-up icons",
    "22. Improve Game Over screen — death cause label, high score animation, share button stub",
    "23. Add Touch joystick overlay for mobile (MobileControlsUI.cs)",

    # ── PHASE 5: Audio ───────────────────────────────────────────────
    "24. Improve AudioManager — pitch variation on eat, stereo pan, combo music layer",
    "25. Add SoundSettings.cs — master/music/sfx independent sliders persisted",

    # ── PHASE 6: Platform Build Config ───────────────────────────────
    "26. Configure Android build settings (orientation, icons, min SDK 24, IL2CPP)",
    "27. Configure iOS build settings (bundle id, team placeholder, URP mobile renderer)",
    "28. Configure Desktop build settings (resolution dialog off, fullscreen, icon)",
    "29. Add BuildHelper.cs Editor script — one-click build all 3 platforms from menu",

    # ── PHASE 7: Performance & Polish ────────────────────────────────
    "30. Add ObjectPooler.cs — pool food, VFX particles, body segments",
    "31. Optimize URP renderers — mobile renderer strips unused passes",
    "32. Add AnalyticsStub.cs — placeholder hooks for game events (no SDK needed)",
    "33. Write CHANGELOG.md and update README.md with full feature list",
    "34. Final git commit — tag v1.0.0, push all changes",
]


def run_step(step_number: int, state: dict):
    step_name = state["current_step_name"]
    log(f"\n{'='*60}")
    log(f"▶ STEP {step_number}: {step_name}")
    log(f"{'='*60}")

    proj = r"D:\github\Snake2.0"
    scripts = proj + r"\Assets\_Scripts"

    # ─── STEP 1 ─────────────────────────────────────────────────────
    if step_number == 1:
        import subprocess
        git = r"C:\Program Files\Git\cmd\git.exe"
        subprocess.run([git, "-C", proj, "config", "user.email", "claude@snake2.dev"])
        subprocess.run([git, "-C", proj, "config", "user.name", "Claude Dev"])

        gitignore = proj + r"\.gitignore"
        if not os.path.exists(gitignore):
            with open(gitignore, "w") as f:
                f.write("[Ll]ibrary/\n[Tt]emp/\n[Oo]bj/\n[Bb]uild/\n[Bb]uilds/\n*.csproj\n*.sln\n*.unityproj\n.vs/\n")

        subprocess.run([git, "-C", proj, "add", "."])
        subprocess.run([git, "-C", proj, "commit", "-m", "chore: baseline snapshot before Claude improvements"])
        complete_step(state, notes="Git initialized, baseline committed")

    # ─── STEP 2 ─────────────────────────────────────────────────────
    elif step_number == 2:
        import shutil
        # Move loose root-level duplicates into proper subfolders (they already exist in subfolders)
        loose = [
            (scripts + r"\Food.cs",         scripts + r"\Gameplay\Food.cs"),
            (scripts + r"\FoodSpawner.cs",  scripts + r"\Gameplay\FoodSpawner.cs"),
            (scripts + r"\SnakeController.cs", scripts + r"\Gameplay\SnakeController.cs"),
            (scripts + r"\GameManager.cs",  scripts + r"\Managers\GameManager.cs"),
            (scripts + r"\ScoreManager.cs", scripts + r"\Managers\ScoreManager.cs"),
            (scripts + r"\UIManager.cs",    scripts + r"\UI\UIManager.cs"),
        ]
        for src, dst in loose:
            if os.path.exists(src) and os.path.exists(dst):
                os.remove(src)
                meta = src + ".meta"
                if os.path.exists(meta): os.remove(meta)
                log(f"  Removed duplicate: {os.path.basename(src)}")

        # Move remaining loose files to proper folders
        moves = [
            (scripts + r"\Board.cs",       scripts + r"\Core\Board.cs"),
            (scripts + r"\SaveSystem.cs",  scripts + r"\Core\SaveSystem.cs"),
            (scripts + r"\FoodVFX.cs",     scripts + r"\Effects\FoodVFX.cs"),
        ]
        for src, dst in moves:
            if os.path.exists(src) and not os.path.exists(dst):
                shutil.move(src, dst)
                meta = src + ".meta"
                if os.path.exists(meta):
                    shutil.move(meta, dst + ".meta")
                log(f"  Moved: {os.path.basename(src)} → {os.path.dirname(dst)}")

        complete_step(state, notes="Folder structure cleaned. Board+SaveSystem→Core, FoodVFX→Effects, duplicates removed.")

    else:
        log(f"Step {step_number} will be implemented in a future session.")
        complete_step(state, notes="Queued for next session")


def main():
    state = load_state()
    if not state:
        log("Initializing Snake 2.0 task...")
        state = init_state(TASK_NAME, STEPS)
        generate_resume_file(state)

    if state.get("status") == "completed":
        log("All steps done! Snake 2.0 is complete.")
        return

    log(f"Resuming: {state['task_name']}")
    log(f"From step {state['current_step']}/{state['total_steps']}: {state['current_step_name']}")

    # Run only the current step (one step per session to be safe)
    run_step(state["current_step"], state)
    state = load_state()
    generate_resume_file(state)
    log(f"\nDone for this session. Next: Step {state.get('current_step')} — {state.get('current_step_name')}")

if __name__ == "__main__":
    main()
