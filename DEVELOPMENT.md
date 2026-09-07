# Gothic Tactics development

## Hex prototype

1. Open the project in Unity 6.3.23f1.
2. Wait for scripts and packages to finish importing.
3. Select **Gothic Tactics > Setup Hex Prototype Scene**.
4. Open `Assets/Scenes/HexPrototype.unity` if Unity does not open it automatically.
5. Enter Play mode.

### Controls

- Hover a tile to highlight it.
- Left-click a tile to select it.
- The active player unit is selected automatically and shows its reachable tiles.
- Left-click a green reachable tile to move along the calculated path; movement costs one AP per hex.
- Dark tiles with cubes are blocked and movement routes around them.
- Use the prototype HUD's **End Turn** button to advance from the Hunter to the Penitent and then the Ghoul.
- The Ghoul takes a short automatic turn and moves one hex towards the nearest hero.
- Use WASD or the arrow keys to pan the camera.
- Use the mouse wheel to zoom.

The first editor command creates the shared prototype material and scene. Commit those generated assets after verifying the scene.
