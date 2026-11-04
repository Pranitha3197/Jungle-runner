# Jungle Rush — Fresh Unity Setup

This is a NEW, self-contained 2D endless runner prototype. It does not use the old project scripts or require third-party packages.

## Install into the correct project
1. Open `C:\Users\prani\endless runner` in Unity.
2. Close Play mode if it is active.
3. Extract this ZIP into the **project root** (the folder containing `Assets`, `Packages`, and `ProjectSettings`). Allow files to merge into `Assets`.
4. Wait for Unity to compile. The files should appear at:
   - `Assets/JungleRush/Scripts/JungleRushGame.cs`
   - `Assets/Editor/JungleRushSetup.cs`
5. In Unity, select **Jungle Rush > Build New Playable Scene** from the top menu.
6. Click **Let's Play** in the dialog, then press the Unity **Play** button.

## Controls
- Space / W / Up Arrow: jump
- S / Down Arrow: duck
- 1 / 2 / 3 or the on-screen buttons: change character appearance
- Collect gold coins; avoid logs.
- Speed rises as survival time increases. Collision ends the run; Run Again restarts.

## Notes
- Built-in Unity APIs only; no asset-store downloads needed.
- Art is generated procedurally from sprites/shapes, so it runs immediately in a blank 2D project.
- This is a playable prototype with stylized generated art, not a photorealistic art pack.
- If Unity shows a compile error, copy the first red error line from the Console and share it.
