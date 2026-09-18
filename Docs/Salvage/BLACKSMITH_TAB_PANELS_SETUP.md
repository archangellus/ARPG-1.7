# Blacksmith `repairTabPanel` / `salvageTabPanel` setup guide

This is a click-by-click Unity Editor guide for building the two content panels that
`GUIBlacksmith` (`Assets/PLAYER TWO/ARPG Project/Core/GUI/GUIBlacksmith.cs`) switches
between: `repairTabPanel` (the existing repair/socket-removal controls) and
`salvageTabPanel` (the new Salvage tab). Neither panel exists in a scene/prefab yet —
this fills the "prefab wiring required" gap noted in
`Docs/Salvage/IMPLEMENTATION_STATUS.md`.

Every field name below is taken directly from `GUIBlacksmith.cs`. Wire the Inspector
references exactly as named; nothing here needs a renamed field or a code change.

## 0. What you're building

```
Blacksmith Window (GUIWindow + GUIBlacksmith)
├── Tabs Container (tabsContainer)         ← Tab.prefab instances go here at runtime
├── Repair Tab Panel  (repairTabPanel)
│   ├── Repair Slot                        → slot
│   ├── Repair Button                      → repairButton
│   ├── Repair Cost Text                   → repairCostText
│   ├── Repair All Button                  → repairAllButton
│   ├── Repair All Cost Text               → repairAllCostText
│   ├── Remove Sockets Button              → removeSocketsButton
│   └── Remove Sockets Cost Text           → removeSocketsCostText
└── Salvage Tab Panel (salvageTabPanel)
    ├── Selected Count Text                → salvageSelectedCountText
    ├── Rewards Text                       → salvageRewardsText
    ├── Returned Socketables Text          → salvageReturnedSocketablesText
    ├── Message Text                       → salvageMessageText
    ├── Rarity Dropdown                    → salvageRarityDropdown
    ├── Select Junk Button                 → salvageSelectJunkButton
    ├── Select Rarity Button               → salvageSelectRarityButton
    ├── Select All Eligible Button         → salvageSelectAllEligibleButton
    ├── Clear Button                       → salvageClearButton
    └── Confirm Button                     → salvageConfirmButton
```

`repairTabPanel` and `salvageTabPanel` are plain `GameObject` references on
`GUIBlacksmith` (not prefabs), so they must live as children of the same window this
component sits on. `GUIBlacksmith.InitializeTabs()` activates one and deactivates the
other at runtime (`ConfigureTab`) — don't hand-set either one's active state; whatever
you leave active in the editor gets overridden on `Start()`.

**Do not wire any `OnClick()`/`OnValueChanged()` events in the Inspector for these
controls.** `GUIBlacksmith.InitializeCallbacks()` and `InitializeTabs()` add every
listener in code on `Start()`. Inspector-wired duplicates will double-fire.

## 1. Prerequisites

- The scene's main Canvas already hosting the other GUI windows (Inventory, Merchant,
  Stash, etc.) — put the Blacksmith window here as a sibling, the same way those exist.
- `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Tab.prefab` — reused as-is for
  `tabPrefab` (same prefab `GUIMerchant` uses for its own tabs).
- `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Socket Slot.prefab` or
  `Inventory Slot.prefab` — duplicated as the base for the Repair Slot (see step 3.1).
- `GUIWindowsManager` in the scene, so its `blacksmith` field can be assigned once the
  window exists (step 6).

## 2. Create the Blacksmith window root

1. Under the Canvas, right-click → **Create Empty**, name it `Blacksmith Window`.
2. Add components: **Rect Transform** (added automatically), **Canvas Renderer** is not
   needed at this level; add **Image** if you want a background panel, then add
   **GUI Window** (`GUIWindow`) and **GUI Blacksmith** (`GUIBlacksmith`).
3. Size and anchor it like the other windows (e.g. centered, stretch or fixed size to
   match your other panels — Merchant/Stash windows are good references for scale).
4. Leave the window's `GameObject` **active** in the hierarchy (`GUIWindow.Show()`
   toggles `gameObject.SetActive`, but the object must start enabled so `Awake`/`Start`
   run) — `Blacksmith.OnInteract` calls `m_blacksmithWindow.Show(this)`, which expects
   the object to already exist in the hierarchy.

## 3. Tabs row

1. Under `Blacksmith Window`, create an empty child `Tabs Container` with a
   **Horizontal Layout Group** (matches the Merchant tab row) and a
   **Content Size Fitter** if you want it to hug its children.
2. Add a **Toggle Group** component to `Blacksmith Window` itself (or a dedicated
   `Toggle Group` object) — this is what keeps only one tab toggled on.
3. Assign on `GUIBlacksmith`:
   - `tabPrefab` → `Tab.prefab`
   - `toggleGroup` → the Toggle Group from step 3.2
   - `tabsContainer` → `Tabs Container`'s `RectTransform`
4. Leave `Tabs Container` empty in the editor — `InitializeTabs()` destroys any
   existing children and instantiates exactly two `Tab.prefab` copies ("Repair" and
   "Salvage") into it at `Start()`.
5. Optional: assign `switchTabClip` to an audio clip for the tab-switch sound.

## 4. Build `repairTabPanel`

1. Under `Blacksmith Window`, create an empty child `Repair Tab Panel`. This is the
   `GameObject` you'll assign to `GUIBlacksmith.repairTabPanel`.
2. **Repair Slot** (assign to `slot`, type `GUIBlacksmithSlot`):
   - Duplicate `Socket Slot.prefab` (or `Inventory Slot.prefab`) into the scene as a
     child of `Repair Tab Panel`; rename it `Repair Slot`.
   - `GUIItemSlot` requires an `Image` component on the same object — the duplicated
     prefab already has one, keep it as the drop-target background.
   - Replace whatever slot script the duplicated prefab carries with
     **GUI Blacksmith Slot** (`GUIBlacksmithSlot`). Remove the old slot script first
     if Unity won't let two slot scripts coexist on the same `Image`.
   - Keep (or add) a child image for showing the equipped item's icon, matching the
     pattern used by the other slot prefabs.
   - Assign this object to `GUIBlacksmith.slot`.
3. **Repair controls** — for each, create a UI **Button** (with a child **Text**) or
   plain **Text** as noted, parented under `Repair Tab Panel`:
   - `Repair Button` (Button) → `repairButton`
   - `Repair Cost Text` (Text) → `repairCostText`
   - `Repair All Button` (Button) → `repairAllButton`
   - `Repair All Cost Text` (Text) → `repairAllCostText`
4. **Socket removal controls**:
   - `Remove Sockets Button` (Button) → `removeSocketsButton`
   - `Remove Sockets Cost Text` (Text) → `removeSocketsCostText`
5. Optional audio: assign `repairAudio` and `removeSocketsAudio` on `GUIBlacksmith`.
6. Optional: set `regularColor` (used to color an unrared item's name in the socket
   removal confirmation message) and edit `removeSocketsConfirmationMessage` if you
   want different wording than the default (`{0}` is replaced with the item's colored
   display name).
7. Assign `Repair Tab Panel` to `GUIBlacksmith.repairTabPanel`.

Layout tip: group the slot and its buttons with a **Vertical Layout Group** (or your
own layout) on `Repair Tab Panel` so the panel resizes cleanly; nothing in the code
depends on a specific layout component.

## 5. Build `salvageTabPanel`

1. Under `Blacksmith Window`, create an empty child `Salvage Tab Panel`. This is the
   `GameObject` you'll assign to `GUIBlacksmith.salvageTabPanel`.
2. **Status texts** (plain UI **Text** objects):
   - `Selected Count Text` → `salvageSelectedCountText` (shows `"Selected: N"`)
   - `Rewards Text` → `salvageRewardsText` (one `"<Item name>: <quantity>"` line per
     material, or `"No rewards"`)
   - `Returned Socketables Text` → `salvageReturnedSocketablesText` (one line per
     returned socketable's item name, or `"No socketables returned"`)
   - `Message Text` → `salvageMessageText` (eligibility/error/success feedback)
3. **Rarity Dropdown** (UI **Dropdown**) → `salvageRarityDropdown`:
   - Populate its options in code-consistent order: index `0` = `"None"` (maps to
     `rarityId -1`, i.e. plain/no-rarity items), then one option per entry of
     `GameDatabase.instance.itemRarities` in index order (index `1` → rarity `0`,
     index `2` → rarity `1`, …). `GUIBlacksmith.SelectSalvageRarity()` reads
     `salvageRarityDropdown.value - 1` directly as the `rarityId` to match, so the
     option order must line up exactly with this offset.
   - You can populate the options list statically in the editor if your rarity list is
     stable, or populate it at runtime from `GameDatabase.instance.itemRarities` before
     the Blacksmith window is first shown — `GUIBlacksmith` does not populate this
     dropdown itself.
4. **Selection buttons** (UI **Button**, with a child **Text** label), all under
   `Salvage Tab Panel`:
   - `Select Junk Button` → `salvageSelectJunkButton`
   - `Select Rarity Button` → `salvageSelectRarityButton` (uses the dropdown's current
     value)
   - `Select All Eligible Button` → `salvageSelectAllEligibleButton`
   - `Clear Button` → `salvageClearButton`
5. **Confirm button**:
   - `Confirm Button` → `salvageConfirmButton`. Its `interactable` state is driven by
     `RefreshSalvagePreview()` (disabled with an empty/invalid preview or an invalid
     Blacksmith context) — don't gate it with a `CanvasGroup` or other Inspector-only
     condition that could disagree with that logic.
6. Assign `Salvage Tab Panel` to `GUIBlacksmith.salvageTabPanel`.

Layout tip: a **Vertical Layout Group** works well here too — status texts up top,
selection buttons in a row or grid, Confirm at the bottom.

## 6. Make equipment rows selectable for salvage

The Salvage tab itself has no item list — selection happens on the existing Inventory
window's item rows while the Salvage tab is open, via a separate component that must
be added to the inventory row prefab:

1. Open `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Inventory Item.prefab`
   (the `GUIItem` row prefab used by the player's inventory grid).
2. Add a child UI **Toggle** to it (e.g. a small checkbox in a corner), named
   `Salvage Toggle`.
3. On the prefab's **root** object (the one already carrying `GUIItem` — required
   because `GUIBlacksmithSalvageToggle` has `[RequireComponent(typeof(GUIItem))]`), add
   **GUI Blacksmith Salvage Toggle** (`GUIBlacksmithSalvageToggle`).
4. Assign the child Toggle from step 6.2 to `GUIBlacksmithSalvageToggle.toggle`.
5. Leave the toggle's `onValueChanged` unwired in the Inspector — the script wires it
   in `Awake()`.

This component already handles visibility (it only shows the toggle while
`GUIWindowsManager.instance.blacksmith.isShowingSalvage` is true) and syncs its checked
state from `GUIBlacksmith.IsSalvageSelected`, so no extra wiring is needed once it's on
the row prefab.

## 7. Wire the window into `GUIWindowsManager`

Select the `GUIWindowsManager` instance in the scene and assign `Blacksmith Window`
(the object carrying `GUIBlacksmith`, from step 2) to its `blacksmith` field. This is
what `Blacksmith.m_blacksmithWindow` (`GUIWindowsManager.instance.blacksmith`) resolves
to when an NPC with the `Blacksmith` component is interacted with.

## 8. Blacksmith NPC assignment

On the `Blacksmith` component (the NPC-side script, not the GUI), assign:

- `salvageSettings` — your `SalvageSettings` asset.
- `salvageProviderId` — a scene-unique stable string (e.g. `"town-blacksmith"`, the
  default).
- `salvageCommitDistance` — how far the player may be from the NPC when confirming.

## 9. Manual verification checklist

Once wired, in Play Mode:

- [ ] Interacting with the Blacksmith opens the window on the **Repair** tab.
- [ ] Clicking the **Salvage** tab switches panels and plays `switchTabClip` (if set).
- [ ] With the Salvage tab open, inventory equipment rows show the salvage toggle;
      other windows' rows do not.
- [ ] Toggling an eligible item on updates `Selected Count`, `Rewards`, and enables
      `Confirm` once a valid preview exists.
- [ ] Toggling an ineligible item (favorite/locked/no recipe) shows the eligibility
      reason in `Message Text` and does not select it.
- [ ] `Select Junk`, `Select Rarity` (using the dropdown), `Select All Eligible`, and
      `Clear` all update the selection and refresh the preview.
- [ ] Confirming a normal batch salvages the items and shows the receipt summary in
      `Message Text`.
- [ ] Confirming a batch containing a configured high-value rarity shows the
      confirmation dialog before committing.
- [ ] Closing the window (or the player moving out of `salvageCommitDistance`) clears
      the selection and disables `Confirm` appropriately.
- [ ] Repair, Repair All, and Remove Sockets still work unchanged on the Repair tab.
