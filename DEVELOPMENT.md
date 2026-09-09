# Gothic Tactics development

## Complete skirmish: The Ashen Bell

1. Open this project in **Unity 6000.3.23f1** and wait for import to finish.
2. From Unity's top menu, choose **Gothic Tactics > Play Complete Skirmish**.
3. Save any current scene when prompted. The command creates `Assets/Scenes/AshenBell.unity` and enters Play Mode.

Choose a local party of 1–4 unique heroes and customise their decks before entering a ruined sanctuary. Kill every enemy while keeping at least one hero alive. Enemy count and HP scale with party size. This is a standalone encounter; online networking, expedition events and loot progression are not implemented yet.


### Hero and deck prototype

The opening screen has party slots, a hero picker, the selected deck, and its legal shared-card pool. Remove a shared card before adding a replacement. Decks require exactly 10 cards, exactly one matching signature, and at most two copies of each shared card. Restore Starter Deck resets the selected slot. Each hero may appear once in a party.

| Hero | Affinity | Resource and signature |
| --- | --- | --- |
| Voss, Iron Warden | Might / crimson | HP damage received builds Resolve; Iron Reckoning spends it for damage. |
| Mara, Graveblade | Agility / emerald | Moving 2+ hexes in one action builds Momentum; Grave Ambush spends it for damage. |
| Cinder, Ash Sorcerer | Intellect / azure | Intellect plays build Heat; Cinder Release spends it for damage. |
| Sister Ash, Ancestor Keeper | Spirit / ivory | Actual healing builds Grace; Ancestral Communion spends it for shield. |
| Aldric, Oathbound Paladin | Might + Spirit / brass | Healing or shielding another hero builds Conviction; Oathkeeper's Verdict spends it for damage. |

All hero resources cap at 3 and persist until spent. Neutral cards are available to every hero. Dual-affinity cards require BOTH affinities. Signature cards belong exclusively to their named hero. The catalogue contains 23 deck cards plus 5 innates, with effects for damage, shield, healing, draw and movement. Reactions, summons and persistent powers are future extensions.

- Each hero starts with a shuffled 10-card deck and draws 5. The hand limit is 8.
- Select a card, then click a highlighted unit or hex on the battlefield. Self cards also require a click on the selected hero. Click the same card again or press Escape to cancel. Selecting another hero cancels targeting.
- A card spends its printed AP cost and goes to discard. Invalid targets and insufficient AP consume nothing. A red banner explains rejected plays for 4.5 seconds, while the selected card stays armed for another target. Range is printed on each card. Valid damage previews account for Guard and shield. Clicks on characters use opaque sprite pixels instead of snapping to nearby units.
- Unplayed cards discard when the party ends its turn. Each living hero draws 5 at the start of the next party turn. Empty draw piles reshuffle their discard piles.
- Movement cards cost their printed AP, not one AP per hex. Ordinary movement and equipment attacks remain available without cards.
- Each hero has an innate costing 1 AP, usable once per turn without drawing it.
- Shield absorbs damage after Guard mitigation and expires at the start of the owner's next turn. A fully absorbed attack does not build Resolve.
- Draw/discard counters, shield (SH), hero resource, card text and legal target highlights are shown in the HUD.

Use **Gothic Tactics > Run Hero and Card Checks** for deck legality, opening hands, card conservation/reshuffling, invalid-play atomicity, shield timing, affinities, signatures, hero resources, innates and 1–4 hero spawning. These checks are provided for execution in Unity; only C# syntax and source-level review could be performed in the authoring environment.

Suggested Play Mode check: change the Paladin's shared cards, attempt to launch with 9 cards, restore to 10, launch, play Protect on Voss, move Cinder into range and cast Ember Bolt, inspect resource gains, end the turn and check the new hands. Repeat with a solo Graveblade and a four-hero party, then resize the Game view and scroll the card library/hand.


### Equipment and backpack prototype

Open **Equipment & Bag** next to the deck tab during preparation. Each hero has main-hand, off-hand, armour and trinket slots, plus a six-item backpack. The prototype armoury provides unlimited test items: add an item to the backpack, click it to equip it, or use Store to return it. Two-handed weapons occupy both hands; displaced items return to the backpack. A swap that would overflow the backpack is rejected without moving anything. Equipment may only be changed during preparation. Builds persist for the current Play session; loot drops, item trading, save files and Diablo-style item footprints are future work.

| Equipment | Slot | Repeatable action / bonus |
| --- | --- | --- |
| Iron Dagger | Main hand | Stab: 1 AP, 2 damage, range 1 |
| Arming Sword | Main hand | Slash: 2 AP, 4 damage, range 1 |
| Double Axe | Both hands | Rending Chop: 3 AP, 7 damage, range 1; applies Bleed after HP damage |
| Hunting Bow | Both hands | Loose Arrow: 2 AP, 3 damage, range 4 |
| Ash Staff | Both hands | Arcane Spark: 2 AP, 3 damage, range 3; enables arcane spells |
| Worn Tome | Off hand | Runic Bolt: 2 AP, 2 damage, range 3; enables arcane spells |
| Iron Shield | Off hand | Raise Shield: 1 AP, 3 shield to self; enables shield cards |
| Leather Armour | Armour | +3 maximum HP |
| Plate Armour | Armour | +5 maximum HP |
| Scholar's Robes | Armour | +1 damage to Intellect damage cards and staff/tome attacks |
| Warding Charm | Trinket | +1 shield granted by cards, innates or equipment |

Equipment actions appear below the selected hero's innate. Select an action, then its target; repeat it while AP remains, or press Escape to cancel. These actions do not consume cards or use the innate's once-per-turn allowance. Clicking an enemy without an action selected uses the main-hand weapon. An empty main hand provides Punch (1 AP, 1 damage, range 1). Invalid actions preserve AP and show the rejection reason.

Bleed causes 1 HP loss at the start of the affected unit's next two turns, bypassing Guard and shield. Reapplying it refreshes the duration to two turns rather than stacking damage. Hits fully absorbed by shield do not apply Bleed. Bleed deaths count towards victory/defeat and remove the unit from occupancy normally.

Crushing Blow requires a melee weapon. Precise Shot requires a bow. Bulwark and the new Shield Wall / Shield Bash cards require an equipped shield. Ember Bolt, Arcane Study, Arcane Barrier and Cinder Release require an equipped staff OR tome. Carrying an item in the backpack does not qualify. Hero affinities still apply. The editor displays requirements and blocks launch if the chosen equipment cannot support the deck; runtime validation checks again before spending a card. Spirit prayers and hero innates remain inherent hero abilities in this version.

**Try it:** give Voss a dagger and keep his shield for cheap repeatable attacks plus shield cards. To try the double axe, remove the two Bulwark cards and replace them with legal shared cards, since the axe takes both hands. Give Cinder a dagger and tome to combine a cheap melee action with arcane cards. Add robes or a charm to test bonuses.

Run **Gothic Tactics > Run Equipment Checks** for inventory capacity, two-handed swaps, independent battle inventory, card requirements, repeatable attacks with an empty hand, rejected gear actions, equipment bonuses and Bleed timing. Also rerun the existing hero/card and skirmish checks. C# syntax was checked during authoring; Unity compilation, execution of these checks and Play Mode remain local validation steps.

### Retro dungeon art pass

The skirmish now uses original code-drawn pixel sprites, a fixed 2:1 isometric camera, textured stone floors and tombs, warm flickering torchlight, and a charcoal/brass HUD inspired by 1990s gothic dungeon crawlers. No external asset packs are needed. Characters have single-frame pixel sprites with the existing movement/lunge motions; directional walk and attack sprite animations are a future art pass.

The battlefield renders to a small point-filtered texture (up to 240 pixels high) with integer enlargement. The HUD stays at display resolution for readable text. Picking and unit labels map through the same viewport; resizing the Game view recreates the render target. WASD pans relative to the screen axes.

After pulling this update, stop Play Mode and run **Gothic Tactics > Play Complete Skirmish** again to recreate the scene with the new lighting. Check unit selection, path highlighting, camera zoom, Game-view resizing and restart locally. Unity rendering cannot be verified in the authoring environment.

- Click a hunter or its squad card to select it; Tab cycles living hunters.
- Click a sage-green hex to move along the gold previewed path. Each hex costs 1 AP.
- Click an enemy on a red hex for the equipped main-hand attack. Its AP cost, damage and range come from the weapon. Ruins block movement and ranged sight; units block movement.

- Equipped shields grant a repeatable **Raise Shield** action. Shield points absorb damage and expire at the start of the owner's next turn. Enemy Guard still reduces incoming attack damage by 2, minimum 1 before shield absorption.
- Healing is available through cards and hero innates.
- **End Turn** or Space lets enemies move and attack. All living units on the incoming team regain 6 AP. Unspent AP does not carry over.
- WASD / arrows pan; scroll zooms. The bottom bar previews movement costs and valid attacks.
- After victory or defeat, **Return to Heroes & Decks** lets you edit the party and fight again. Deck edits persist during the current Play session only.

The original Hex Prototype and its menu command remain available. The skirmish uses its own rules and presentation classes in `Assets/_Project/Scripts/Skirmish`.

### Validation

Choose **Gothic Tactics > Run Skirmish Rules Checks** to verify geometry, movement costs, occupancy, attack legality, guard duration, healing, death, outcomes and bounded AI actions. Success prints `All Ashen Bell rules checks passed.` in the Console; failure throws with the check name.

Play Mode smoke test: select each hunter, move around a ruin, verify the AP preview, attack an enemy, raise a shield, use a healing card after taking damage, end a turn, finish the encounter and restart. Also test a smaller Game view and camera pan/zoom. Unity Play Mode and rendering must be verified locally; this authoring environment does not contain Unity.

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

### Invalid-target regression checks

`Run Hero and Card Checks` now includes legal cards aimed beyond range, a signature with stored resources, blocked line of sight within range, an occupied movement destination, an overlong movement path, wrong-team targets, healing beyond range and clicks outside the board. Each rejection compares hand/draw/discard order, AP, HP, shield, resources, innate state, guard, positions and turn state before and after. It also verifies that the same card succeeds exactly once on the next valid target. These checks require Unity to execute.
