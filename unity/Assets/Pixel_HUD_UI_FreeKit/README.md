# Pixel UI HUD: Fantasy RPG Kit (Free Sample)

A handcrafted pixel art HUD/UI starter kit for fantasy RPG games — **free sample edition**.
Drop it into your Unity project and explore a retro-style game interface in minutes.

> **This package is a free sample reconstructed from a small selection of assets in the full version.**
> If you need more color variations, icons, and components for production use,
> we recommend the **full version** — see [Full Version](#full-version) below.

---

## What's Included

| Category | Count | Description |
|----------|-------|-------------|
| **Sprites** | 9 | Pixel art UI elements (PNG, lossless) |
| **Prefabs** | 1 | Ready-to-use HUD prefab |
| **Fonts** | 3 weights | Noto Serif (Regular / Medium / Bold) with SDF assets |
| **Animation** | 1 | Open animation with Animator Controller |
| **Demo Scene** | 1 | Assembled HUD preview scene |
| **Preview Images** | 2 | Reference images |

---

## Contents

### Icons (2)
- `Icon_Consumable_Apple` — Apple (Consumable)
- `Icon_Tool_RepairHammer` — Repair Hammer (Tool)

### UI Elements (7)
- `UI_Image_Bg` — Tiling background image
- `UI_Indicator_Heart` — Heart indicator
- `UI_Progress_Style2_Bg` / `UI_Progress_Style2_Fill_Red` — Progress bar (background + red fill)
- `UI_StatusBar_Bg` / `UI_StatusBar_Fill_HP` — HP status bar (background + HP fill)
- `UI_Slot_Selected` — Selected slot

### Prefab
- `HUD.prefab` — A sample HUD layout. Drop it onto a Canvas to get started.

### Animation
- `OpenAnimation.anim` + `HUD.controller` — Animator Controller and open animation clip.

### Fonts
- Noto Serif — 3 weights (Regular / Medium / Bold)
- TTF + TextMesh Pro SDF assets included
- Licensed under the SIL Open Font License v1.1 (see `Font/LICENSE_NotoSerif.txt`)

---

## Quick Start

1. Import the package into your Unity project.
2. Open `Demo/DemoScene.unity` to see the HUD in action.
3. Drag `Prefabs/HUD.prefab` into your scene's Canvas to use the layout.

---

## Folder Structure

```
Assets/Pixel_HUD_UI_FreeKit/
├── Animation/              Animator Controller + animation clip
├── Demo/                   Demo scene (DemoScene.unity)
├── Font/                   TTF + SDF font assets & license
├── Prefabs/                HUD.prefab
├── Preview/                Reference images
└── Sprites/
    ├── Icons/              Item icons
    └── UI Elements/        Background, Bars, Indicator, Slot
```

---

## Requirements

- **Unity version**: 2021.3 LTS or newer
- **Render pipeline**: Compatible with Built-in, URP, and HDRP (UI-only)
- **TextMesh Pro** — auto-imported by Unity on first use
- **Input System Package** (`com.unity.inputsystem`) — required by the demo scene
  - Active Input Handling: `Input System Package (New)` or `Both`
- **Sprite format**: PNG (lossless)

---

## Pixel-Perfect Tips

- Keep the Canvas reference resolution as a clean multiple of the source sprite size to preserve crisp pixel edges.
- If pixel edges look soft, enable **Pixel Perfect** on the Canvas (Screen Space - Camera) or use the **2D Pixel Perfect** package.
- The bundled font SDF assets are generated at standard settings. Re-bake at a higher resolution if you intend to display the font at very large sizes.

---

## Full Version

This free sample kit is a **reconstructed subset** of the full version. If you need the complete asset set for production use, we recommend the full version.

**Full version — Pixel UI HUD: Fantasy RPG Kit** includes:
- **82 sprites** (about 9× the volume of this free kit)
- **2 progress bar styles** × 9 color variations (Blue, BlueGreen, Green, Orange, Pink, Purple, Red, SkyBlue, Yellow)
- **HP / MP / Stamina status bars** in 9 colors with matching potion icons
- **18 icons** (consumables, tools, system, materials, and more)
- **Full set of panels, slots, buttons, frames, thumbnails, and indicators**
- **2 prefabs** — complete HUD layout + component prefab for custom assembly

> The full version is also available as part of the **Pixel RPG UI MegaPack** bundle.

---

## Third-Party Notices

This package includes **Noto Serif** by Google Fonts (Noto Project Authors).
Licensed under the **SIL Open Font License, Version 1.1**.
Full license text is located at `Font/LICENSE_NotoSerif.txt`.

---

## Support

If you have any questions, issues, or feature requests, please reach out:

**Email**: lattemongling@gmail.com

When reporting an issue, please include:
- Unity version
- Render pipeline (Built-in / URP / HDRP)
- Steps to reproduce

Enjoy creating your retro RPG UI!
