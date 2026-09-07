# Gothic Tactics development

## Complete skirmish: The Ashen Bell

1. Open this project in **Unity 6000.3.23f1** and wait for import to finish.
2. From Unity's top menu, choose **Gothic Tactics > Play Complete Skirmish**.
3. Save any current scene when prompted. The command creates `Assets/Scenes/AshenBell.unity` and enters Play Mode.

Three hunters face four revenants in a ruined sanctuary. Kill every enemy while keeping at least one hunter alive. This is a standalone encounter with procedural placeholder characters and scenery.

### Retro dungeon art pass

The skirmish now uses original code-drawn pixel sprites, a fixed 2:1 isometric camera, textured stone floors and tombs, warm flickering torchlight, and a charcoal/brass HUD inspired by 1990s gothic dungeon crawlers. No external asset packs are needed. Characters have single-frame pixel sprites with the existing movement/lunge motions; directional walk and attack sprite animations are a future art pass.

The battlefield renders to a small point-filtered texture (up to 240 pixels high) with integer enlargement. The HUD stays at display resolution for readable text. Picking and unit labels map through the same viewport; resizing the Game view recreates the render target. WASD pans relative to the screen axes.

After pulling this update, stop Play Mode and run **Gothic Tactics > Play Complete Skirmish** again to recreate the scene with the new lighting. Check unit selection, path highlighting, camera zoom, Game-view resizing and restart locally. Unity rendering cannot be verified in the authoring environment.

- Click a hunter or its squad card to select it; Tab cycles living hunters.
- Click a sage-green hex to move along the gold previewed path. Each hex costs 1 AP.
- Click an enemy on a red hex to attack for 2 AP. Damage is deterministic. Ruins block movement and ranged sight; units block movement.
- **Warden:** 16 HP, 4 damage, melee. **Arbalist:** 10 HP, 3 damage, range 4. **Hexblade:** 12 HP, 3 damage, range 2.
- **Guard** spends all remaining AP (at least 1) to reduce incoming damage by 2, minimum 1, until that hunter's next turn.
- Each hunter has one **tonic**: restore up to 6 HP for 2 AP.
- **End Turn** or Space lets enemies move and attack. All living units on the incoming team regain 6 AP. Unspent AP does not carry over.
- WASD / arrows pan; scroll zooms. The bottom bar previews movement costs and valid attacks.
- **Play Again** resets the encounter after victory or defeat. Stop and re-enter Play Mode to restart at any time.

The original Hex Prototype and its menu command remain available. The skirmish uses its own rules and presentation classes in `Assets/_Project/Scripts/Skirmish`.

### Validation

Choose **Gothic Tactics > Run Skirmish Rules Checks** to verify geometry, movement costs, occupancy, attack legality, guard duration, healing, death, outcomes and bounded AI actions. Success prints `All Ashen Bell rules checks passed.` in the Console; failure throws with the check name.

Play Mode smoke test: select each hunter, move around a ruin, verify the AP preview, attack an enemy, guard, use a tonic after taking damage, end a turn, finish the encounter and restart. Also test a smaller Game view and camera pan/zoom. Unity Play Mode and rendering must be verified locally; this authoring environment does not contain Unity.

## Hex prototype

1. Open the project in Unity 6.3.23f1.
2. Wait for scripts and packages to finish importing.
3. Select **Gothic Tactics > Setup Hex Prototype Scene**.
4. Open `Assets/Scenes/HexPrototype.unity` if Unity does not open it automatically.
5. Enter Play mode.

### Controls

- Hover a tile to highlight it.
- Left-click a tile to select it.
- Left-click the red prototype unit to select it and show reachable tiles.
- Left-click a green reachable tile to move along the calculated path; movement costs one AP per hex.
- Dark tiles with cubes are blocked and movement routes around them.
- Use WASD or the arrow keys to pan the camera.
- Use the mouse wheel to zoom.

The first editor command creates the shared prototype material and scene. Commit those generated assets after verifying the scene.
