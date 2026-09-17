# Unity 6000.3.15f1 — Blacksmith Salvage Setup Guide

This guide finishes the Editor-side setup required by the salvage runtime code. Salvage is a **tab in the existing Blacksmith window**. It uses the same `UITab` prefab and `ToggleGroup` pattern as the Merchant; do not create a second window or a second NPC provider.

## Before editing prefabs

1. Open the project in **Unity 6000.3.15f1** and wait for script import to finish.
2. Open **Window > General > Console**, clear it, and confirm there are no compiler errors. Do not continue with missing-script errors.
3. Make a source-control checkpoint. The following shared prefabs will be edited:
   - `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/Global/__CANVAS__.prefab`
   - `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Inventory Item.prefab`
   - `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/NPCs/Blacksmith.prefab`
4. In **Edit > Project Settings > Editor**, keep **Asset Serialization > Mode** set to **Force Text**.

## Part 1 — Create salvage data assets

Create this optional organization first:

```text
Assets/PLAYER TWO/ARPG Project/Examples/Data/Salvage/
├── Materials/
├── Recipes/
└── Settings/
```

### 1.1 Create materials

For each material, right-click `Materials` and choose **Create > PLAYER TWO > ARPG Project > Salvage > Material**.

Create at least:

| Asset name | Display Name | Icon |
| --- | --- | --- |
| `Metal.asset` | `Metal` | Any existing metal/ore icon, or None temporarily |
| `Hide.asset` | `Hide` | Any existing hide icon, or None temporarily |
| `Essence.asset` | `Essence` | Any existing magical material icon, or None temporarily |
| `Core.asset` | `Core` | Any existing high-tier material icon, or None temporarily |

Select each asset once. Confirm its serialized **Id** is non-empty. Unity generates this stable ID in `OnValidate`; do not edit it or duplicate a configured material asset to create a different material. Rename/move the asset normally—the ID must remain unchanged.

### 1.2 Create recipes

Right-click `Recipes` and choose **Create > PLAYER TWO > ARPG Project > Salvage > Recipe**. A recipe must have at least one reward, every material reference must be assigned, and every quantity must be greater than zero.

For a minimal smoke-test configuration, create `Fallback Equipment.asset`:

| Rewards element | Material | Quantity |
| --- | --- | ---: |
| 0 | Metal | 1 |

For the guide's deterministic example, additionally create:

| Recipe | Reward rows |
| --- | --- |
| `Rare Weapon.asset` | Metal 6; Essence 2 |
| `Magic Armor.asset` | Hide 4; Essence 1 |
| `Legendary Weapon.asset` | Metal 8; Essence 3; Core 1 |

Select each recipe once and verify its **Id** is populated. Do not place duplicate material IDs in one recipe.

### 1.3 Create Salvage Settings

1. Right-click `Settings` and choose **Create > PLAYER TWO > ARPG Project > Salvage > Settings**.
2. Name it `Default Blacksmith Salvage Settings.asset`.
3. Set:
   - **Configuration Version**: `1`
   - **Material Cap**: `999999`
   - **Fallback Recipe**: `Fallback Equipment` for the first smoke test.
4. The project currently resolves rarity by its zero-based position in `GameDatabase.itemRarities`. Open `Assets/PLAYER TWO/ARPG Project/Examples/Data/Game/Template Game Data.asset`, expand **Item Rarities**, and record the index beside each actual rarity asset. Do not infer the index from a filename.
5. Add routing rules as needed:
   - **Scope**: choose one or more actual `ItemScope` flags.
   - **Rarity Id**: enter the recorded zero-based database index; `-2` means any rarity and `-1` means an item with no rarity.
   - **Priority**: use a higher number for a more specific rule.
   - **Recipe**: assign the corresponding recipe.
6. Example rule setup:
   - Weapon + the project's chosen Rare index → priority `100` → `Rare Weapon`.
   - Armor + the project's chosen Magic index → priority `100` → `Magic Armor`.
   - Weapon + the project's chosen Legendary/high-value index → priority `100` → `Legendary Weapon`.
7. Add the Legendary/high-value rarity index to **High Value Rarity Ids**.
8. Avoid two rules with the same highest priority that both match the same scope/rarity. Such an item is intentionally ineligible.
9. After changing recipes, routing, cap, or protection policy after play has begun, increment **Configuration Version** to invalidate open previews.

Once all supported equipment has explicit rules, remove the fallback if unsupported items should be blocked instead of yielding one Metal.

## Part 2 — Build the Salvage section prefab

The runtime expects legacy **UnityEngine.UI** controls (`Text`, `Button`, `Dropdown`, `Toggle`), matching the existing project. Do not substitute TextMeshPro fields unless the scripts are changed accordingly.

1. Open `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/Global/__CANVAS__.prefab` in Prefab Mode.
2. Find **Blacksmith Window**.
3. Under it, create **UI > Panel** named `Salvage Section`.
4. Set its `RectTransform` anchors to stretch horizontally and vertically across the content area beneath the future tab row. Match the margins and background styling of the existing repair controls.
5. Create the following children. The hierarchy names are recommendations; component types are required:

```text
Salvage Section                         (Image optional; section root)
├── Heading                             (Text: "Equipment Salvage")
├── Selected Count                      (Text: "Selected: 0")
├── Rewards Heading                     (Text: "Guaranteed Materials")
├── Rewards Text                        (Text: "No rewards")
├── Socketables Heading                 (Text: "Returned Socketables")
├── Returned Socketables Text           (Text: "No socketables returned")
├── Message Text                        (Text; blank initially)
├── Rarity Dropdown                     (Dropdown)
├── Select Junk Button                  (Button + child Text: "Select Junk")
├── Select Rarity Button                (Button + child Text: "Select Rarity")
├── Select All Eligible Button          (Button + child Text: "Select All Eligible")
├── Clear Button                        (Button + child Text: "Clear")
└── Confirm Button                      (Button + child Text: "Salvage Selected")
```

6. Recommended layout:
   - Put the selection buttons and dropdown in a top or left `Horizontal Layout Group`/`Vertical Layout Group`.
   - Give `Rewards Text`, `Returned Socketables Text`, and `Message Text` enough height for multiple lines.
   - Place **Confirm Button** at the bottom and make it visually destructive/confirming.
   - Leave **Confirm Button > Interactable** enabled in the prefab; runtime code disables it until a valid preview exists.
7. Configure **Rarity Dropdown > Options** in this exact order:
   - Element 0: `None`
   - Element 1: the display name of `GameDatabase.itemRarities[0]`
   - Element 2: the display name of `GameDatabase.itemRarities[1]`
   - Continue in exact database order.

   This ordering is required because the runtime maps dropdown value `N` to rarity index `N - 1`.
8. Set `Salvage Section` inactive after wiring. The selected `UITab` controls section visibility at runtime.
9. To make this reusable, drag `Salvage Section` from the Hierarchy into `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/` and name the resulting prefab `Blacksmith Salvage Section.prefab`. Keep the instance nested under **Blacksmith Window** and apply its overrides.

## Part 3 — Convert the existing Blacksmith window to Merchant-style tabs

### 3.1 Prepare the existing Repair section

1. Still in `__CANVAS__.prefab` Prefab Mode, under **Blacksmith Window**, create an empty UI object named `Repair Section`.
2. Preserve every existing reference and layout while moving the current repair/socket-removal controls under `Repair Section`:
   - Blacksmith item slot
   - Repair action/button and price
   - Repair All action/button and price
   - Remove Sockets action/button and price
3. Do not delete or recreate referenced controls. Reparent the existing objects so their serialized `GUIBlacksmith` references stay intact.
4. Use **RectTransform > Anchor Presets** and keep their visual positions unchanged. Set `Repair Section` active.

### 3.2 Create the tab row

1. Under **Blacksmith Window**, create **UI > Empty (Rect Transform)** named `Tabs`.
2. Anchor it across the top of the window and give it enough height for the existing Merchant tabs (approximately the same height as the Merchant's `Tabs` container).
3. Add a **Horizontal Layout Group**:
   - Child Alignment: Middle Left
   - Control Child Size Width/Height: enabled as appropriate for the existing `Tab.prefab`
   - Child Force Expand Width: optional; match Merchant styling
4. Add a **Toggle Group** to `Tabs`:
   - **Allow Switch Off**: disabled
5. Do not manually add Repair or Salvage buttons. `GUIBlacksmith` instantiates two copies of the shared Merchant tab prefab at runtime.

### 3.3 Assign every GUIBlacksmith field

Select **Blacksmith Window**, locate `GUIBlacksmith`, and preserve all existing assignments. Assign new fields as follows:

| GUIBlacksmith field | Exact assignment |
| --- | --- |
| **Tab Prefab** | `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Tab.prefab` (`UITab`) |
| **Toggle Group** | `Tabs` object's `ToggleGroup` |
| **Tabs Container** | `Tabs` object's `RectTransform` |
| **Repair Tab Panel** | `Repair Section` root GameObject |
| **Salvage Tab Panel** | `Blacksmith Salvage Section` instance/root GameObject |
| **Switch Tab Clip** | The same clip assigned to `GUIMerchant.switchTabClip`, or another UI switch clip |
| **Salvage Selected Count Text** | `Salvage Section/Selected Count` Text |
| **Salvage Rewards Text** | `Salvage Section/Rewards Text` Text |
| **Salvage Returned Socketables Text** | `Salvage Section/Returned Socketables Text` Text |
| **Salvage Message Text** | `Salvage Section/Message Text` Text |
| **Salvage Rarity Dropdown** | `Salvage Section/Rarity Dropdown` Dropdown |
| **Salvage Confirm Button** | `Salvage Section/Confirm Button` Button |
| **Salvage Select Junk Button** | `Salvage Section/Select Junk Button` Button |
| **Salvage Select Rarity Button** | `Salvage Section/Select Rarity Button` Button |
| **Salvage Select All Eligible Button** | `Salvage Section/Select All Eligible Button` Button |
| **Salvage Clear Button** | `Salvage Section/Clear Button` Button |

Do **not** add persistent `On Click` events to these buttons or tab toggles. `GUIBlacksmith.Start`, like `GUIMerchant`, registers them in code. Duplicate Inspector callbacks would execute actions twice.

The pre-existing fields must remain assigned to the original objects:

- **Slot**
- **Repair Button**
- **Repair All Button**
- **Repair Cost Text**
- **Repair All Cost Text**
- **Remove Sockets Button**
- **Remove Sockets Cost Text**
- Existing repair/socket audio and confirmation fields

6. Confirm **Blacksmith Window** remains inactive by default.
7. Save Prefab Mode and apply changes.
8. Select the scene/global canvas instance and confirm `GUIWindowsManager.blacksmith` still references this same **Blacksmith Window**. There is no salvage-window field to assign.

## Part 4 — Add per-item selection to the existing Inventory Item prefab

Bulk actions work from the Salvage section, but individual selection requires a toggle on each carried inventory item.

1. Open `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Inventory Item.prefab` in Prefab Mode.
2. On its root `Inventory Item` object, add **GUI Blacksmith Salvage Toggle**. The root already has the required `GUIItem` component.
3. Add **UI > Toggle** as a root child named `Salvage Toggle`.
4. Position it in the upper-right corner:
   - Anchor Min/Max: `(1, 1)`
   - Pivot: `(1, 1)`
   - Anchored Position: about `(-2, -2)`
   - Size: about `18 x 18`
5. Style its background/checkmark to remain readable over item rarity colors. Ensure its `Target Graphic` and `Graphic` references are assigned as Unity creates them.
6. Assign the child `Salvage Toggle` component to **GUI Blacksmith Salvage Toggle > Toggle**.
7. Set the child toggle GameObject inactive in the prefab. The adapter shows it only while the existing Blacksmith window is open on its Salvage tab and hides it everywhere else, including Merchant/Stash UI.
8. Do not configure a persistent `On Value Changed` event; the adapter registers it in code.
9. Save and apply the prefab.
10. Return to `__CANVAS__.prefab`, select its `GUI` component, and confirm **Item Prefab** still points to this edited `Inventory Item.prefab`.

## Part 5 — Configure the existing Blacksmith NPC prefab

1. Open `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/NPCs/Blacksmith.prefab`.
2. Select the root **Blacksmith** object and its existing `Blacksmith` component. Do not add another provider component.
3. Assign:

| Blacksmith field | Value |
| --- | --- |
| **Salvage Settings** | `Default Blacksmith Salvage Settings.asset` |
| **Salvage Provider Id** | A stable identifier such as `tutorial-town-blacksmith` |
| **Salvage Commit Distance** | `4` (or the actual interaction distance used by the scene) |

4. Preserve existing **Min Price**, **Max Price**, socket-removal settings, collider, animation, and interaction configuration.
5. The provider ID must be unique for concurrently reachable Blacksmith providers and must not change between saves/builds without intent.
6. Save and apply the prefab. Apply the override to existing scene instances if Unity does not propagate it automatically.
7. If the scene uses `ARPGInteractable`, keep its **Linked Interactive** pointed to this existing `Blacksmith` component. Its prompt can remain `Blacksmith` because both Repair and Salvage are offered in one window.

## Part 6 — First functional test

1. Enter Play Mode in a scene containing:
   - the global `__CANVAS__` setup,
   - the configured Blacksmith NPC,
   - the player and inventory.
2. Give the player at least two carried equippable items. Do not equip them.
3. Interact with the Blacksmith. Verify:
   - the existing Blacksmith window opens,
   - the existing player inventory opens,
   - **Repair** and **Salvage** tabs use the same visual prefab as Merchant tabs,
   - Repair is selected first,
   - switching tabs plays the configured switch sound once per configured toggle callback and changes the visible section.
4. Open **Salvage**. Verify selection toggles appear on carried inventory items.
5. Select an eligible item. Verify Selected Count and guaranteed rewards update, but the inventory item is not removed.
6. Clear and test **Select Junk**, **Select Rarity**, and **Select All Eligible**.
7. Confirm salvage. Verify the selected equipment disappears, materials increase, and socketables appear in the inventory.
8. Close and reopen the Blacksmith. Verify selection is clear and Repair is the initial tab.
9. Save, exit Play Mode, reload the save, and verify the item remains consumed and material balance remains present.

## Part 7 — Acceptance checklist

Use this checklist before considering setup complete:

- [ ] Console has zero compile errors and zero missing-script warnings.
- [ ] Blacksmith tabs are instantiated from the existing Merchant `Tab.prefab`.
- [ ] Both tabs belong to one `ToggleGroup`; **Allow Switch Off** is disabled.
- [ ] Only one of Repair Section or Salvage Section is active at a time.
- [ ] Existing repair, repair-all, and socket-removal behavior still works.
- [ ] Inventory selection toggles appear only on the open Salvage tab.
- [ ] Ineligible/favorite/locked/non-equipment items cannot be selected.
- [ ] Select All Eligible skips configured high-value rarities.
- [ ] Explicit high-value selection opens confirmation.
- [ ] Rare weapon + Magic armor rules produce exactly 6 Metal, 4 Hide, and 3 Essence.
- [ ] A socketed item returns its actual socketables; insufficient post-removal space consumes nothing.
- [ ] Material-cap failure consumes nothing.
- [ ] Closing/reopening does not duplicate callbacks or preserve a stale selection.
- [ ] Moving out of `Salvage Commit Distance` prevents confirmation.
- [ ] Save/reload preserves item IDs, protection flags, wallet balances, and receipts.

## Troubleshooting

### No tabs appear

Check `GUIBlacksmith.tabPrefab`, `toggleGroup`, and `tabsContainer`. All three are required. Confirm `Tab.prefab` contains `UITab`, a `Toggle`, and its label `Text`.

### Both sections appear together

Confirm both generated tabs use the same `ToggleGroup`, **Allow Switch Off** is disabled, and the section roots assigned to `repairTabPanel`/`salvageTabPanel` are different GameObjects.

### Clicking a salvage control does nothing

Remove manual persistent button callbacks, then confirm the corresponding `GUIBlacksmith` field is assigned. Runtime callbacks are registered in `Start`.

### Individual item toggles do not appear

Confirm `Inventory Item.prefab` has `GUIBlacksmithSalvageToggle` on the same root as `GUIItem`, its **Toggle** reference is assigned, and `GUI.itemPrefab` points to that prefab.

### Every item says no recipe is configured

Assign a valid fallback for the smoke test or add a matching scope/rarity routing rule. Verify all recipe material references and positive quantities.

### "No script asset for SalvageRecipe" appears while creating an asset

Update to a revision containing the separate `SalvageRecipe.cs`,
`SalvageMaterialDefinition.cs`, and `SalvageSettings.cs` files, then allow Unity to
finish recompiling. Each `ScriptableObject` now lives in a file with the exact same
name Unity requires. Delete any broken recipe/material asset produced before the
recompile and create it again from **Create > PLAYER TWO > ARPG Project > Salvage**.
If the warning remains, clear the Console and resolve the first compiler error; a
compile failure elsewhere also prevents Unity from loading these script assets.

### Salvage immediately becomes stale

Do not alter favorite/junk/lock/socket state between preview and confirmation. If configuration changed, increment **Configuration Version**, close the Blacksmith, and reopen it.

### Commit says the Blacksmith is unavailable

Confirm the NPC's `salvageSettings` and non-empty `salvageProviderId`, and ensure the player is within `salvageCommitDistance` when pressing Confirm.
