# Blacksmith repair/salvage panel setup guide

This is a click-by-click Unity Editor guide for building the Blacksmith window out of
its three components:

- **`GUIBlacksmith`** (`Assets/PLAYER TWO/ARPG Project/Core/GUI/GUIBlacksmith.cs`) — the
  window itself: lifecycle, tab switching, and binding the active Blacksmith NPC to the
  two panels below. Holds no repair- or salvage-specific fields.
- **`GUIBlacksmithRepairPanel`** (`GUIBlacksmithRepairPanel.cs`) — the Repair tab's own
  component: repair slot, repair/repair-all, socket removal.
- **`GUIBlacksmithSalvagePanel`** (`GUIBlacksmithSalvagePanel.cs`) — the Salvage tab's
  own component: "Directly in Inventory" picking mode, auto-generated rarity category
  buttons, and the salvaged-materials icon row.

None of these panels exist in a scene/prefab yet — this fills the "prefab wiring
required" gap noted in `Docs/Salvage/IMPLEMENTATION_STATUS.md`.

Every field name below is taken directly from the four scripts involved (the three
above plus `GUIBlacksmithMaterialIcon`). Wire the Inspector references exactly as
named; nothing here needs a renamed field or a code change.

## 0. What you're building

```
Blacksmith Window (GUIWindow + GUIBlacksmith)
├── Tabs Container (tabsContainer)              ← Tab.prefab instances go here at runtime
└── Panels Container (panelsContainer)           ← organizational parent, mirrors GUIMerchant's sectionsContainer
    ├── Repair Panel  (GUIBlacksmithRepairPanel) → GUIBlacksmith.repairPanel
    │   ├── Repair Slot                          → slot
    │   ├── Repair Button                        → repairButton
    │   ├── Repair Cost Text                     → repairCostText
    │   ├── Repair All Button                    → repairAllButton
    │   ├── Repair All Cost Text                 → repairAllCostText
    │   ├── Remove Sockets Button                → removeSocketsButton
    │   └── Remove Sockets Cost Text              → removeSocketsCostText
    └── Salvage Panel (GUIBlacksmithSalvagePanel) → GUIBlacksmith.salvagePanel
        ├── Directly In Inventory Toggle         → pickingToggle
        ├── Picking Cursor Icon                  → pickingCursorIcon (optional)
        ├── Categories Container                 → categoriesContainer  ← rarity buttons instantiated here at runtime
        ├── Rewards Container                    → salvageRewardsContainer ← material icons instantiated here after a salvage
        ├── Returned Socketables Text             → salvageReturnedSocketablesText
        └── Message Text                         → salvageMessageText
```

`GUIBlacksmith.InitializeTabs()` activates one panel's `GameObject` and deactivates the
other at runtime (`ConfigureTab`) — don't hand-set either panel's active state in the
editor; whatever you leave active gets overridden as soon as tabs initialize.
`InitializeTabs()` is idempotent and runs from both `Start()` and `Show()`, whichever
happens first, so the window shows its `defaultTab` correctly even if `Show()` is
called before the window's own `Start()` has run (e.g. a window whose `GameObject`
someone left disabled in the hierarchy despite step 2.4 below).

`defaultTab` (`GUIBlacksmith.BlacksmithTab`, `Repair` or `Salvage`) picks which tab is
active the moment the window opens — set it in the Inspector instead of relying on
whichever tab happened to init first.

`panelsContainer` is optional and purely organizational: if assigned,
`InitializeTabs()` reparents both panels under it (matching `GUIMerchant`'s
`sectionsContainer`/`tabsContainer` split). If left unassigned, the two panels can live
anywhere under the window and everything still works; you just lose that hierarchy
grouping.

**Do not wire any `OnClick()`/`OnValueChanged()` events in the Inspector for any of
these controls.** Each panel wires its own listeners in code on `Start()`, and
`GUIBlacksmith.InitializeTabs()` wires the tab toggles. Inspector-wired duplicates will
double-fire. The one exception is the rarity category buttons and material icons, which
don't exist in the editor at all — they're entirely instantiated at runtime from the
prefabs you assign (steps 5.3 and 5.4).

## 1. Prerequisites

- The scene's main Canvas already hosting the other GUI windows (Inventory, Merchant,
  Stash, etc.) — put the Blacksmith window here as a sibling, the same way those exist.
- `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Tab.prefab` — reused as-is for
  `tabPrefab` (same prefab `GUIMerchant` uses for its own tabs).
- `Assets/PLAYER TWO/ARPG Project/Examples/Prefabs/GUI/Socket Slot.prefab` or
  `Inventory Slot.prefab` — duplicated as the base for the Repair Slot (see step 4.2).
- `GUIWindowsManager` in the scene, so its `blacksmith` field can be assigned once the
  window exists (step 7).

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

## 3. Tabs row and panels container

1. Under `Blacksmith Window`, create an empty child `Tabs Container` with a
   **Horizontal Layout Group** (matches the Merchant tab row) and a
   **Content Size Fitter** if you want it to hug its children.
2. Create a second empty child, `Panels Container` — this is where the two panel
   GameObjects from steps 4 and 5 will end up (either by parenting them here yourself,
   or by assigning `panelsContainer` and letting `InitializeTabs()` reparent them for
   you at `Start()`).
3. Add a **Toggle Group** component to `Blacksmith Window` itself (or a dedicated
   `Toggle Group` object) — this is what keeps only one tab toggled on.
4. Assign on `GUIBlacksmith`:
   - `tabPrefab` → `Tab.prefab`
   - `toggleGroup` → the Toggle Group from step 3.3
   - `tabsContainer` → `Tabs Container`'s `RectTransform`
   - `panelsContainer` → `Panels Container`'s `RectTransform` (optional, see step 0)
5. Leave `Tabs Container` empty in the editor — `InitializeTabs()` destroys any
   existing children and instantiates exactly two `Tab.prefab` copies ("Repair" and
   "Salvage") into it at `Start()`.
6. Optional: assign `switchTabClip` to an audio clip for the tab-switch sound.
7. Optional: set `defaultTab` (defaults to `Repair`) if you want the window to open on
   the Salvage tab instead.

## 4. Build the Repair panel

1. Under `Panels Container` (or anywhere under the window, if you skipped
   `panelsContainer`), create an empty child `Repair Panel`. Add **GUI Blacksmith
   Repair Panel** (`GUIBlacksmithRepairPanel`) to it.
2. **Repair Slot** (assign to the panel's `slot`, type `GUIBlacksmithSlot`):
   - Duplicate `Socket Slot.prefab` (or `Inventory Slot.prefab`) into the scene as a
     child of `Repair Panel`; rename it `Repair Slot`.
   - `GUIItemSlot` requires an `Image` component on the same object — the duplicated
     prefab already has one, keep it as the drop-target background.
   - Replace whatever slot script the duplicated prefab carries with
     **GUI Blacksmith Slot** (`GUIBlacksmithSlot`). Remove the old slot script first
     if Unity won't let two slot scripts coexist on the same `Image`.
   - Keep (or add) a child image for showing the equipped item's icon, matching the
     pattern used by the other slot prefabs.
   - Assign this object to `Repair Panel`'s `slot` field.
3. **Repair controls** — for each, create a UI **Button** (with a child **Text**) or
   plain **Text** as noted, parented under `Repair Panel`, and assign to the matching
   field on `GUIBlacksmithRepairPanel`:
   - `Repair Button` (Button) → `repairButton`
   - `Repair Cost Text` (Text) → `repairCostText`
   - `Repair All Button` (Button) → `repairAllButton`
   - `Repair All Cost Text` (Text) → `repairAllCostText`
4. **Socket removal controls**:
   - `Remove Sockets Button` (Button) → `removeSocketsButton`
   - `Remove Sockets Cost Text` (Text) → `removeSocketsCostText`
5. Optional audio: assign `repairAudio` and `removeSocketsAudio` on
   `GUIBlacksmithRepairPanel`.
6. Optional: set `regularColor` and edit `removeSocketsConfirmationMessage` if you want
   different wording than the default (`{0}` is replaced with the item's colored
   display name).
7. On `GUIBlacksmith`, assign `Repair Panel` to `repairPanel`.

Layout tip: group the slot and its buttons with a **Vertical Layout Group** (or your
own layout) on `Repair Panel` so the panel resizes cleanly; nothing in the code depends
on a specific layout component.

## 5. Build the Salvage panel

The Salvage panel has no fixed item list or Confirm button. There are two independent
ways to salvage, and both act immediately (prompting for confirmation only when a
configured high-value rarity is involved):

- **Directly in Inventory**: toggle it on, then left-click any carried equipment Item
  to salvage it on the spot. Stays on for consecutive picks until toggled off,
  right-clicked away, or the tab/window closes.
- **Salvage by rarity**: a row of buttons, one per configured rarity plus "All Items",
  auto-generated at runtime from `GameDatabase.instance.itemRarities` — no dropdown, no
  hand-authored button list, nothing to keep in sync by hand.

1. Under `Panels Container` (or anywhere under the window), create an empty child
   `Salvage Panel`. Add **GUI Blacksmith Salvage Panel** (`GUIBlacksmithSalvagePanel`)
   to it.
2. **Directly in Inventory**:
   - Create a UI **Toggle** (e.g. a button-styled toggle with a hammer/pickaxe icon,
     matching the reference mock) → `pickingToggle`. Its `isOn` visual state is what
     shows the mode is active; you don't need to script anything else for it.
   - Optional: create a small UI **Image** (the "cursor" icon shown while picking is
     active) anywhere under the Canvas so it renders above everything else → assign to
     `pickingCursorIcon`. Leave it **inactive** by default — `GUIBlacksmithSalvagePanel`
     activates/deactivates and repositions it automatically while picking mode is on.
     If you skip this field, picking mode still works; you just get no cursor visual.
3. **Salvage by rarity**:
   - Create an empty child `Categories Container` (with a **Horizontal** or
     **Vertical Layout Group**, matching the reference's stacked buttons) →
     `categoriesContainer`.
   - Create a plain UI **Button** prefab with a child **Text** label (anywhere in your
     Project, e.g. reuse the style of `Tab.prefab` or make a new one) →
     `categoryButtonPrefab`. Leave `Categories Container` empty in the editor —
     `GUIBlacksmithSalvagePanel.Start()` destroys any existing children and
     instantiates one button per rarity (labeled from `ItemRarity.displayName`) plus a
     trailing "All Items" button, each already wired to salvage that category
     immediately.
4. **Salvaged materials (icons)**:
   - Create an empty child `Rewards Container` (with a **Horizontal Layout Group**) →
     `salvageRewardsContainer`.
   - Create a small prefab with an **Image** (the material's icon) and a child **Text**
     (its quantity), and add **GUI Blacksmith Material Icon**
     (`GUIBlacksmithMaterialIcon`) to its root, assigning that `Image` to `icon` and
     that `Text` to `quantityText` → assign the prefab to `materialIconPrefab`. After
     each salvage, the panel clears and repopulates this container with one icon per
     distinct material granted, using the material `Item`'s own `image` sprite.
5. **Status texts**:
   - `Returned Socketables Text` (Text) → `salvageReturnedSocketablesText` (one line
     per returned socketable's item name, or "No socketables returned")
   - `Message Text` (Text) → `salvageMessageText` (eligibility/error/success feedback)
6. On `GUIBlacksmith`, assign `Salvage Panel` to `salvagePanel`.

Layout tip: a **Vertical Layout Group** works well for the whole panel — the picking
toggle up top, category buttons in a column, results (socketables text + material
icons) at the bottom, matching the reference mock's layout.

## 6. Wire the window into `GUIWindowsManager`

Select the `GUIWindowsManager` instance in the scene and assign `Blacksmith Window`
(the object carrying `GUIBlacksmith`, from step 2) to its `blacksmith` field. This is
what `Blacksmith.m_blacksmithWindow` (`GUIWindowsManager.instance.blacksmith`) resolves
to when an NPC with the `Blacksmith` component is interacted with. It's also what
`GUIEquipmentSlot` and `GUIItem` use for their `m_blacksmith.slot` right-click/drag
handling, and what `GUIItem` now checks on every left/right click to know whether
picking mode is active — none of that needs extra wiring beyond this one field.

## 7. Blacksmith NPC assignment

On the `Blacksmith` component (the NPC-side script, not the GUI), assign:

- `salvageSettings` — your `SalvageSettings` asset. **This is the single most common
  setup gap**: every salvage action (picking mode and every category button) checks
  this field and shows "Salvage settings are not assigned on the Blacksmith" in
  `Message Text` if it's missing, even when every GUI reference above is wired
  correctly.
- `salvageProviderId` — a scene-unique stable string (e.g. `"town-blacksmith"`, the
  default).
- `salvageCommitDistance` — how far the player may be from the NPC when salvaging.

## 8. Manual verification checklist

Once wired, in Play Mode:

- [ ] Interacting with the Blacksmith opens the window on the **Repair** tab.
- [ ] Clicking the **Salvage** tab switches panels and plays `switchTabClip` (if set).
- [ ] Toggling **Directly in Inventory** on shows the picking cursor (if assigned) and
      following the mouse; left-clicking an eligible carried item salvages it
      immediately and updates `Message Text` and the material icon row; the toggle
      stays on for a second consecutive pick.
- [ ] Left-clicking an ineligible item (favorite/locked/equipped/no configured rewards) while
      picking shows the eligibility reason in `Message Text` without salvaging it, and
      picking mode stays on.
- [ ] Right-clicking while picking mode is on cancels it without acting on the item
      underneath.
- [ ] Each rarity category button salvages every eligible carried item of that rarity
      immediately; "All Items" salvages everything eligible except configured
      high-value rarities.
- [ ] Salvaging a batch containing a configured high-value rarity shows the
      confirmation dialog before committing.
- [ ] Switching to the Repair tab, or closing the window, turns picking mode off.
- [ ] Repair, Repair All, and Remove Sockets still work unchanged on the Repair tab.
- [ ] Right-clicking/dragging an equippable item while the Blacksmith window is open
      (and picking mode is off) still places it into the Repair Slot, on both tabs.
