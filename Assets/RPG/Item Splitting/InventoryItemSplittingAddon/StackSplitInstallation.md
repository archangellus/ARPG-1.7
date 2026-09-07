# Inventory Item Splitting Add-on

This add-on was extracted from the project's integrated stack-splitting implementation and adapted so the pasted core scripts do **not** need to be replaced.

## Why this is separate

The project versions contain stack splitting mixed together with other changes (pet inventory, inspector changes, drag/drop changes, etc.), while the pasted scripts contain newer socket-aware behavior. Replacing the pasted scripts with the project scripts would therefore remove or conflict with unrelated functionality.

The add-on keeps the pasted versions of these files untouched:

- `GUI.cs`
- `GUIItem.cs`
- `GUIInventory.cs`
- `GUIItemSlot.cs`
- `ItemInstance.cs`
- `ItemAttributes.cs`

## Files

- `GUIStackSplitFeature.cs` — detects source/target clicks and performs the initial one-item split without modifying the core GUI classes.
- `GUIStackSplitMenuAddon.cs` — generated/default split quantity UI, with optional custom prefab/slider support.
- `ItemInstanceStackSplitExtensions.cs` — copies an `ItemInstance` while preserving data, durability, rarity, affixes, attributes, and sockets.

## Installation

1. Copy the three `.cs` files into your Unity project, for example under `Assets/Scripts/Inventory/StackSplit/`.
2. Let Unity compile.
3. Select the GameObject that already contains the `GUI` component.
4. Add the `GUI Stack Split Feature` component to the same GameObject.
5. Optional: assign a custom `GUIStackSplitMenuAddon` prefab and/or a menu container. If left empty, the menu is generated automatically.

No changes are required in the pasted `GUI`, `GUIItem`, `GUIInventory`, `GUIItemSlot`, `ItemInstance`, or `ItemAttributes` files.

## Controls

1. Left-click a stack to pick it up normally.
2. Hold either Shift key.
3. Left-click one of these targets:
   - an empty valid inventory location;
   - a compatible existing stack with at least one free stack slot;
   - a compatible `GUIItemSlot`.
4. One item is moved immediately and the split menu opens.
5. Use `+`, `-`, or an assigned slider to adjust the moved quantity, then press Confirm.

The source stack is always kept at one or more items.

## Behavior retained from the project implementation

- Shift is the split modifier.
- The initial split moves one item.
- The quantity menu edits the source and destination stacks live.
- The source stack cannot be reduced below one.
- Destination stack capacity is respected.
- Splitting back onto the original occupied source area is rejected.
- A source-cell preview can be shown while Shift is held.

## Compatibility note

This version targets the pasted scripts supplied with this request, including the socket-aware `ItemInstance` constructor that accepts the `sockets` array. The copy helper deliberately preserves socket/affix/attribute data instead of using the older project's simpler copy constructor.

The add-on handles desktop/editor/WebGL Shift-click splitting. The pasted mobile code has no Shift-key interaction, so no touch-specific split gesture is introduced here.
