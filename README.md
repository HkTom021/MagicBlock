# Magic Block

Block puzzle game for mobile, made in Unity 6 (6000.0.75f1, URP, Input System).
Inspired by Block Blast, with three extra mechanics:

- **End Turn**: end the turn early. Blocks you didn't use go into Storage.
- **Storage**: 5 slots (expandable). Drag a stored block onto the board in a later turn.
- **Buffs**: every 5 turns, pick 1 of 3 random buffs.
  - Consumable: Block Destroyer, Single Line Clear, Block Reroll, Storage Cleanup.
  - Permanent: Expanded Storage, Score Multiplier, Combo Bonus, Lucky Blocks, Clear Bonus.

You lose when no block from your hand or Storage fits, you can't End Turn, and you have no consumable buff left.

## Run it

1. Open the project in Unity 6000.0.75f1.
2. Open `Assets/MagicBlock/Scenes/Game.unity` and press Play.
   If the scene is missing or broken, use the menu **MagicBlock → Build Game Scene**.

## Code layout

| Path | What's there |
| --- | --- |
| `Assets/MagicBlock/Scripts/Core` | Board logic and block shapes (no UI) |
| `Assets/MagicBlock/Scripts/GameManager.cs` | Game rules: hand, Storage, End Turn, score, turns, game over |
| `Assets/MagicBlock/Scripts/Buffs` | Buff data and buff rules |
| `Assets/MagicBlock/Scripts/UI` | uGUI built at runtime, drag and drop, buff panel |
| `Assets/MagicBlock/Editor` | Scene builder menu and sprite import rule |

## Note on assets

The art, audio and font in `Assets/MagicBlock/Art`, `Audio` and `Fonts` are placeholders taken from another game for this class prototype. They are not original work and must be replaced before any release.
