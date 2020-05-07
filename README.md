# Rimworld Snowshoes Mod

This mod adds snowshoes to travel faster on snow, crampons to travel faster on ice and spiked snowshoes for both.
It also reworks how path cost it's calculated when dealing with snow.

Requires Harmony

## Stats
### CountersSnowPenalty:
Modifier applied to snow pathcost. Values float 0 to 1 (hide at 0)

Depending on quality:
- Awful 0.7
- Poor 0.75
- Normal 0.8
- Good 0.85
- Excellent 0.9
- Masterwork 0.95
- Legendary 1

### CountersIcePenalty:
Modifier applied to terrain pathcost when ice. Values float 0 to 1 (hide at 0)

Depending on quality:
- Awful 0.6
- Poor 0.65
- Normal 0.7
- Good 0.75
- Excellent 0.8
- Masterwork 0.85
- Legendary 9

## Pathcost rework
### Original:
- Get terrain pathcost
- If items on tile have bigger pathcost, this is the new pathcost
- If snow on tile has bigger pathcost, this is the new pathcost
- If door on tile add fixed pathcost

### Reworked:
- If snow it's medium or thick, ignore terrain and use snow pathcost
- Otherwise get the biggest pathcost between terrain and snow (applying new stats to counter the penalty)
- If items on tile have bigger pathcost, this is the new pathcost
- If door on tile add fixed pathcost

## Apparel:
### Snowshoes:
* TechLevel: Neolithic
* Slot: Legs Shell
* Cost: 20 Wood, 20 Leather (patchwork)
* Workshop: TailoringBench, CraftingSpot
* Skills: Min 4 crafting
* Work: 5000
* When equiped: -0.05 MoveSpeed
* CountersSnowPenalty: 1

### Crampons:
* TechLevel: Industrial
* Slot: Legs Shell
* Cost: 20 Steel, 20 Leather (patchwork)
* Workshop: Smithy
* Skills: Min 5 crafting
* Work: 7000
* When equiped: -0.03 MoveSpeed
* CountersIcePenalty: 1

### Spiked Snowshoes:
* TechLevel: Industrial
* Slot: Legs Shell
* Cost: 30 Steel, 20 Leather (patchwork)
* Workshop: Smithy
* Skills: Min 6 crafting
* Work: 9000
* When equiped: -0.07 MoveSpeed
* CountersSnowPenalty: 0.9
* CountersIcePenalty: 0.9
