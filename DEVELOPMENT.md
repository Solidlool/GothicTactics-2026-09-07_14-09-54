# Gothic Tactics development

## Complete skirmish: The Ashen Bell

1. Open this project in **Unity 6000.3.23f1** and wait for import to finish.
2. From Unity's top menu, choose **Gothic Tactics > Play Complete Skirmish**.
3. Save any current scene when prompted. The command creates `Assets/Scenes/AshenBell.unity` and enters Play Mode.

Choose a local party of 1–4 unique heroes and customise their decks before entering a ruined sanctuary. Kill every enemy while keeping at least one hero alive. Enemy count and HP scale with party size. This is a standalone encounter; online networking, expedition events and inventory progression are not implemented yet.


### Hero and deck prototype

The opening screen has party slots, a hero picker, the selected deck, and its legal shared-card pool. Remove a shared card before adding a replacement. Decks require exactly 10 cards, exactly one matching signature, and at most two copies of each shared card. Restore Starter Deck resets the selected slot. Each hero may appear once in a party.

| Hero | Affinity | Resource and signature |
| --- | --- | --- |
| Voss, Iron Warden | Might / crimson | HP damage received builds Resolve; Iron Reckoning spends it for damage. |
| Mara, Graveblade | Agility / emerald | Moving 2+ hexes in one action builds Momentum; Grave Ambush spends it for damage. |
| Cinder, Ash Sorcerer | Intellect / azure | Intellect plays build Heat; Cinder Release spends it for damage. |
| Sister Ash, Ancestor Keeper | Spirit / ivory | Actual healing builds Grace; Ancestral Communion spends it for shield. |
| Aldric, Oathbound Paladin | Might + Spirit / brass | Healing or shielding another hero builds Conviction; Oathkeeper's Verdict spends it for damage. |

All hero resources cap at 3 and persist until spent. Neutral cards are available to every hero. Dual-affinity cards require BOTH affinities. Signature cards belong exclusively to their named hero. The catalogue contains 21 deck cards plus 5 innates, with effects for damage, shield, healing, draw and movement. Reactions, summons and persistent powers are future extensions.

- Each hero starts with a shuffled 10-card deck and draws 5. The hand limit is 8.
- Select a card, then click a highlighted unit or hex on the battlefield. Self cards also require a click on the selected hero. Click the same card again or press Escape to cancel. Selecting another hero cancels targeting.
- A card spends its printed AP cost and goes to discard. Invalid targets and insufficient AP consume nothing.
- Unplayed cards discard when the party ends its turn. Each living hero draws 5 at the start of the next party turn. Empty draw piles reshuffle their discard piles.
- Movement cards cost their printed AP, not one AP per hex. Ordinary movement and basic attacks remain available without cards.
- Each hero has an innate costing 1 AP, usable once per turn without drawing it.
- Shield absorbs damage after Guard mitigation and expires at the start of the owner's next turn. A fully absorbed attack does not build Resolve.
- Draw/discard counters, shield (SH), hero resource, card text and legal target highlights are shown in the HUD.

Use **Gothic Tactics > Run Hero and Card Checks** for deck legality, opening hands, card conservation/reshuffling, invalid-play atomicity, shield timing, affinities, signatures, hero resources, innates and 1–4 hero spawning. These checks are provided for execution in Unity; only C# syntax and source-level review could be performed in the authoring environment.

Suggested Play Mode check: change the Paladin's shared cards, attempt to launch with 9 cards, restore to 10, launch, play Protect on Voss, move Cinder into range and cast Ember Bolt, inspect resource gains, end the turn and check the new hands. Repeat with a solo Graveblade and a four-hero party, then resize the Game view and scroll the card library/hand.

### Retro dungeon art pass

The skirmish now uses original code-drawn pixel sprites, a fixed 2:1 isometric camera, textured stone floors and tombs, warm flickering torchlight, and a charcoal/brass HUD inspired by 1990s gothic dungeon crawlers. No external asset packs are needed. Characters have single-frame pixel sprites with the existing movement/lunge motions; directional walk and attack sprite animations are a future art pass.

The battlefield renders to a small point-filtered texture (up to 240 pixels high) with integer enlargement. The HUD stays at display resolution for readable text. Picking and unit labels map through the same viewport; resizing the Game view recreates the render target. WASD pans relative to the screen axes.

After pulling this update, stop Play Mode and run **Gothic Tactics > Play Complete Skirmish** again to recreate the scene with the new lighting. Check unit selection, path highlighting, camera zoom, Game-view resizing and restart locally. Unity rendering cannot be verified in the authoring environment.

- Click a hunter or its squad card to select it; Tab cycles living hunters.
- Click a sage-green hex to move along the gold previewed path. Each hex costs 1 AP.
- Click an enemy on a red hex to attack for 2 AP. Damage is deterministic. Ruins block movement and ranged sight; units block movement.

- **Guard** spends all remaining AP (at least 1) to reduce incoming damage by 2, minimum 1, until that hunter's next turn.
- Healing is available through cards and hero innates.
- **End Turn** or Space lets enemies move and attack. All living units on the incoming team regain 6 AP. Unspent AP does not carry over.
- WASD / arrows pan; scroll zooms. The bottom bar previews movement costs and valid attacks.
- After victory or defeat, **Return to Heroes & Decks** lets you edit the party and fight again. Deck edits persist during the current Play session only.

The original Hex Prototype and its menu command remain available. The skirmish uses its own rules and presentation classes in `Assets/_Project/Scripts/Skirmish`.

### Validation

Choose **Gothic Tactics > Run Skirmish Rules Checks** to verify geometry, movement costs, occupancy, attack legality, guard duration, healing, death, outcomes and bounded AI actions. Success prints `All Ashen Bell rules checks passed.` in the Console; failure throws with the check name.

Play Mode smoke test: select each hunter, move around a ruin, verify the AP preview, attack an enemy, guard, use a healing card after taking damage, end a turn, finish the encounter and restart. Also test a smaller Game view and camera pan/zoom. Unity Play Mode and rendering must be verified locally; this authoring environment does not contain Unity.

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
