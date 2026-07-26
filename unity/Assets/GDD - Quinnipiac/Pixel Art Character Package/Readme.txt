QUGDD - Pixel Character Grid Sample
2D Pixel Sprite Animation Sample Scene - README

---------------------
1. Introduction
---------------------
This package includes a sample Unity scene that showcases multiple animated 2D pixel art characters. Each character comes with various animations and color variants. The included SpriteGridOrganizer script auto-arranges characters into a structured grid, perfect for previews, prototyping, or building sprite libraries.

---------------------
2. Package Contents
---------------------
- Pixel Art Characters (multiple styles/colors)
- Animator Controllers with prebuilt animations
- SpriteGridOrganizer.cs (auto layout script)
- SampleScene.unity (preconfigured demo)
- Ready-to-use GameObjects for customization

-----------------------------
3. Installation & Setup
-----------------------------
Step 1: Import the Package
- Open Unity
- Go to Assets > Import Package > Custom Package…
- Select Pixel_Art_Character_Package.unitypackage
- Click Import (ensure all files are selected)

Step 2: Open the Scene
- In the Project window, navigate to:
  Assets/QUGDD/Scenes/SampleScene.unity
- Double-click to open

---------------------
4. Usage Guide
---------------------
- Press Play in the Unity Editor
- Characters will appear in an animated grid
- The parent GameObject should contain the SpriteGridOrganizer.cs script
- All animated characters must be direct children of this parent

Animation Tree:
- Select any character to preview animation states
- Customize transitions in the Animator window

-------------------------------
5. Customization Options
-------------------------------
- Add your own sprites as children to auto-place in the grid
- Swap or modify animations in Animator Controllers
- Create color variants by editing sprite textures

---------------------
6. Troubleshooting
---------------------
Issue: Characters not appearing in grid
- Ensure the parent GameObject has SpriteGridOrganizer.cs
- Verify each child has a SpriteRenderer or Animator component or both

Issue: Animations not playing
- Confirm each Animator Controller is correctly assigned

---------------------
7. Contact & Support
---------------------
For support or feedback, contact:
Markbergsbaken@quinnipiac.edu

Thanks for using this package! 
