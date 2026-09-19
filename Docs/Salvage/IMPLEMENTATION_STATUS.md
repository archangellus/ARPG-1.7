# Salvage system implementation status

## Environment and integration map

- Unity: `6000.3.15f1`; Input System `1.19.0`; uGUI `2.0.0`; Test Framework `1.6.0`.
- Owned items are reference-type `ItemInstance` objects held by `Inventory.items` (`Dictionary<ItemInstance, InventoryCell>`). `Inventory.TryRemoveItem` clears all occupied grid cells and emits `onItemRemoved`; sorting clears/reinserts the same references.
- Equipment definitions derive from `ItemEquippable`; type routing uses the existing `ItemInstance.GetItemScope()` and rarity uses `rarityId` / `GameDatabase.itemRarities`.
- Equipped items live in `CharacterEquipments` / `EntityItemManager`, not the carried inventory, so carried-inventory ownership is also the equipped-item exclusion boundary.
- Socket contents are `ItemInstance[] ItemInstance.sockets`; `GetOccupiedSockets()` returns the attached runtime instances. The existing socket insertion model creates a definition-only socket instance. Salvage returns those exact attached instances.
- Character inventory and equipment are serialized by `CharacterSerializer` through `InventorySerializer`, `EquipmentsSerializer`, and `ItemSerializer`. New item identity/protection metadata and character-scoped salvage state use this same snapshot.
- `GameSave.Save()` is the existing save boundary. Binary and JSON writes now use a temporary file and atomic replacement. PlayerPrefs remains limited to Unity's single saved string operation.
- Salvage is a tab on the existing `GUIBlacksmith`/`GUIWindow`; no separate salvage window or provider is introduced. World access continues through the existing `Blacksmith : Interactive`, and `ARPGInteractable` can keep linking to that component.
- The active player is supplied by `Blacksmith.OnInteract`; the same Blacksmith instance and player are rebound into every preview and checked again at commit.

## Steps 1–10

1. **Complete** — inspected project version/packages, item/inventory/equipment/socket, UI, interaction, spawn, and save paths; no asmdef encloses the core runtime scripts.
2. **Complete (code/configuration types)** — `SalvageSettings` lives in a same-named source file so Unity can create its ScriptableObject asset; reward lists (`SalvageMaterialAmount`) are plain data, not a separate asset type — see "Salvage rewards: overrides, fallback, drop chance" below. Salvage materials are ordinary `Item` assets (stackable, non-equippable) rather than a bespoke material type — see "Materials are inventory items" below. Item overrides, fallback rewards, per-reward-line drop chance, version fingerprint, and high-value rarities are authorable. Overrides beat fallback; malformed reward lists are rejected.
3. **Complete** — stable GUID identity and favorite/lock metadata are serialized on each item. Old saves lazily receive an ID. Salvage materials are granted directly into the carried inventory (serialized by the existing `InventorySerializer`); `CharacterSalvageState` now only holds idempotency receipts, character-scoped and serialized in the same character snapshot. There is no junk flag — see "Salvage interaction model" below.
4. **Complete** — `SalvageService.Evaluate` is shared by manual and bulk UI paths. Only carried equipment with resolvable salvage rewards is eligible. Favorite/lock protections are enforced.
5. **Complete** — previews bind item IDs, revisions, provider, rule fingerprint, operation ID, deterministic material totals, returned socket instances, and high-value confirmation.
6. **Complete** — commit removes all selected equipment before inserting exact attached socket instances, so newly freed grid cells count. Any insertion failure rolls the runtime draft back.
7. **Complete, awaiting Editor failure-injection verification** — commits are guarded and idempotent with saved receipts. Inventory removals, granted material items, returned sockets, and the receipt enter one `GameSerializer` save. A pre-save exception rolls runtime state back (including removing any material/socket items already inserted into the inventory); filesystem JSON/binary saves use temp-and-replace.
8. **Complete (existing-window runtime integration); prefab wiring required** — `GUIBlacksmith` is now a thin window/tab orchestrator; the repair and salvage controls live on their own `GUIBlacksmithRepairPanel`/`GUIBlacksmithSalvagePanel` components (assigned to `GUIBlacksmith.repairPanel`/`salvagePanel`), matching `GUIMerchant`'s tabs/sections split. It instantiates Repair and Salvage `UITab` toggles exactly like `GUIMerchant`. The Salvage panel has no persistent multi-select/confirm flow: "Directly in Inventory" picking mode salvages one clicked carried item immediately, and an auto-generated (from `GameDatabase.itemRarities`) row of rarity category buttons salvages an entire rarity immediately — both preview-then-commit in one step, prompting only for high-value confirmation. Salvaged materials render as icons (`GUIBlacksmithMaterialIcon`, one per distinct material) instead of a text list.
9. **Complete (existing-provider integration); content setup required** — the existing `Blacksmith` owns salvage settings/provider identity, receives the player through its existing interaction, checks distance/provider state at commit, and invalidates previews when its window closes.
10. **Code checks complete; Unity checks pending** — repository whitespace validation passed. No Unity executable or generated C# solution is installed in this environment, so compilation, EditMode/PlayMode tests, prefab validation, save/reload, and gameplay acceptance remain Editor checks and are not claimed as passed.

## Inspector and content setup

For a click-by-click procedure to build the Blacksmith window's `GUIBlacksmithRepairPanel` and `GUIBlacksmithSalvagePanel` hierarchies and wire every field listed below, follow `Docs/Salvage/BLACKSMITH_TAB_PANELS_SETUP.md`.

1. Create material assets as ordinary **Item** assets (`Create > PLAYER TWO > ARPG Project > Item`, or any project item-creation menu). Enable `canStack` and set a positive `stackCapacity`; leave them non-equippable (base `Item`, not `ItemEquippable`) so they can't be worn. No separate material asset type exists — any stackable `Item` can be used as a salvage reward.
2. Create one **Salvage Settings** asset (`Create > PLAYER TWO > ARPG Project > Salvage > Settings`). Each of the following is a `rewards` list of material `Item` + `quantity` + `dropChance` (0–1; 1 = always drops, e.g. 0.1 = 10% chance to grant the full `quantity`, rolled independently per reward line per salvaged item), resolved most-specific-first: `itemOverrides` (one entry per specific equipment `Item` definition), then `rarityOverrides` (one entry per `rarityId` — `-1` for plain/no-rarity equipment — covering every item of that rarity regardless of type), then `fallbackRewards` (everything else). Assign high-value rarity indexes. Increment `configurationVersion` whenever a runtime-relevant policy changes. This is all configured on this one asset — `ItemRarity` assets have no salvage fields.
3. On the **existing Blacksmith window**, keep `GUIBlacksmith` as the orchestrator only. Assign the same `UITab` prefab used by `GUIMerchant` to `tabPrefab`, a `ToggleGroup` to `toggleGroup`, and an empty tab-row `RectTransform` to `tabsContainer`. Optionally assign a `panelsContainer` `RectTransform` — purely organizational, mirroring `GUIMerchant`'s `sectionsContainer`; if set, both panels are reparented under it at `Start()`. Optionally assign the same style of `switchTabClip` used by the Merchant. `GUIBlacksmith` instantiates the Repair and Salvage tabs at runtime using the Merchant pattern; do **not** create separate tab Buttons or another `GUIWindow`.
4. Create a `GameObject` with `GUIBlacksmithRepairPanel` for the Repair tab (assign `slot`, `repairButton`, `repairAllButton`, `repairCostText`, `repairAllCostText`, `removeSocketsButton`, `removeSocketsCostText`, and optionally `repairAudio`/`removeSocketsAudio`/`regularColor`/`removeSocketsConfirmationMessage` — these live on this component, not `GUIBlacksmith`), and a second `GameObject` with `GUIBlacksmithSalvagePanel` for the Salvage tab (assign `pickingToggle`, optionally `pickingCursorIcon`, `categoriesContainer` + `categoryButtonPrefab`, `salvageRewardsContainer` + `materialIconPrefab`, `salvageReturnedSocketablesText`, `salvageMessageText`). There is no dropdown, no Select Junk/Rarity/All Eligible/Clear/Confirm buttons to wire by hand, and no per-row selection toggle component — see "Salvage interaction model" below. Assign both `GameObject`s to `GUIBlacksmith.repairPanel`/`salvagePanel`. Full click-by-click steps: `Docs/Salvage/BLACKSMITH_TAB_PANELS_SETUP.md`.
5. `GUIItem`'s click handling checks `GUIBlacksmith.IsPickingForSalvage` first on every left/right click — no per-row component or wiring is needed for this; it works the moment `salvagePanel` is assigned on `GUIBlacksmith` (step 4). `GUIEquipmentSlot`/`GUIItem` likewise keep addressing the repair slot as `GUIBlacksmith.slot`, a read-only pass-through to `repairPanel.slot`.
6. On the **existing Blacksmith NPC**, assign `salvageSettings`, a scene-unique stable `salvageProviderId`, and `salvageCommitDistance`. No new provider component is needed. The existing Blacksmith interaction opens the same window on its Repair tab; the player presses the Salvage tab to switch. Granted materials land directly in the player's carried inventory as regular stacked items — there is no separate wallet query. `salvageSettings` unassigned here is the most common "salvage buttons do nothing" cause — every salvage action reports it through `Message Text` rather than failing silently.

## Salvage interaction model

There is no persistent multi-select-then-confirm flow. Both paths preview and commit
in one step, prompting only when the batch includes a configured high-value rarity:

- **Directly in Inventory** (`GUIBlacksmithSalvagePanel.pickingToggle`): toggling it on
  sets `GUIBlacksmith.IsPickingForSalvage`, which `GUIItem.HandleLeftClick`/`HandleRightClick`
  check first, ahead of every other click behavior (merchant buy, equip, stash, sell).
  While on, left-clicking any carried equipment Item salvages it immediately via
  `GUIBlacksmithSalvagePanel.TrySalvageItem`; right-clicking cancels picking mode
  instead of running its usual handler. Picking mode turns off automatically when the
  Salvage tab loses focus or the window closes (`GUIBlacksmith.CancelSalvagePicking`).
- **Salvage by rarity**: `GUIBlacksmithSalvagePanel.InitializeCategories()` instantiates
  one button per `GameDatabase.instance.itemRarities` entry (labeled from
  `ItemRarity.displayName`) plus a trailing "All Items" button, into
  `categoriesContainer`, at `Start()` — nothing to hand-author or keep in sync.
  Clicking a rarity button salvages every eligible carried item of that rarity,
  including high-value ones (with confirmation). "All Items" deliberately skips
  configured high-value rarities, since it's a broad/blanket action rather than an
  explicit choice of a specific rarity.
- Favorite, locked, non-equipment, equipped/non-carried, and items with no resolvable
  salvage rewards are blocked in both paths via the shared `SalvageService.Evaluate`. Quest item
  definitions are non-equipment in the current model. No gold or upgrade refund is
  involved. Stash and consumables are outside scope.
- The junk item-instance flag is gone entirely: `ItemInstance.isJunk`/`TrySetJunk` and
  `ItemSerializer.isJunk` were removed (favorite/lock metadata remain). Old saves still
  carrying an `isJunk` field in their JSON load fine — `JsonUtility` silently ignores
  fields that no longer exist on the target type.
- `SalvageService.TryCommit` only mutates the data-level `Inventory`/`ItemInstance`
  model; it has no `GUIItem`/GUI awareness by design. Granted materials and returned
  socketables get their `GUIItem`s automatically, since `Inventory.TryAddItem` fires
  `onItemAdded`, which `GUIInventory` already listens to. Consumed items don't have an
  equivalent reactive path — `GUIInventory` never subscribes to `onItemRemoved` — so
  `GUIBlacksmithSalvagePanel.Commit` explicitly looks up and destroys each salvaged
  item's `GUIItem` after a successful commit, via the new `GUIInventory.FindGUIItem`,
  mirroring the `Destroy(guiItem.gameObject)` pattern already used elsewhere (e.g.
  `GUIInventory.RemoveStack`, `GUIBlacksmithRepairPanel`'s socket-removal-with-break
  path) for "this item is now gone" rather than moved/equipped elsewhere.

## Salvage rewards: item override, rarity override, fallback, drop chance

There is no `SalvageRecipe` asset type and no priority/rule list. A reward list
(`List<SalvageMaterialAmount>`, each a material `Item` + `quantity` + `dropChance`) is
plain data, defined inline in exactly three places on `SalvageSettings`, resolved in
this order by `TryGetRewards(ItemInstance, out rewards, out reason)`:

1. `itemOverrides[].rewards` — an explicit per-`Item`-definition reward list. Most
   specific; wins over everything else for that exact `Item` definition.
2. `rarityOverrides[].rewards` — a `SalvageRarityOverride { rarity, rewards }` entry
   covering every item of that rarity, regardless of item type/scope. `rarity` is a
   direct `ItemRarity` asset reference (drag-and-drop in the Inspector, not a raw
   index), matched against `ItemInstance.GetRarity()`; leave `rarity` unassigned to
   match plain/no-rarity equipment. Falls back to here when no item override matched.
3. `fallbackRewards` — used when neither of the above matched.

- **`SalvageSettings.defaultRarity`** (optional `ItemRarity`): a plain/unrolled item
  (`ItemInstance.GetRarity() == null`, e.g. starting gear or a loot roll that didn't
  assign a rarity) is treated as this rarity when matching `rarityOverrides`, instead
  of only matching an override left with an empty `rarity` field. Leave it unassigned
  to keep the old behavior (plain gear only matches an override with `rarity == null`).

Each candidate list is validated via `ValidateRewards` (non-empty, no duplicate
materials, a stackable material needs a positive `stackCapacity`, `dropChance` within
`[0, 1]`) once it's the one that would actually be used.

- **`SalvageMaterialAmount.dropChance`** (`[Range(0,1)]`, default `1`): the chance this
  reward line is granted at all, rolled once per reward line per salvaged item, in
  `SalvageService.TryCreatePreview` (`UnityEngine.Random.value > dropChance` skips it).
  It's all-or-nothing per line — on success the full `quantity` is granted, on failure
  none of it is; there's no partial/scaled grant. The roll happens once, at preview
  creation, and is baked into `SalvagePreview.materials`, so a later high-value
  confirmation commits exactly what the preview showed rather than re-rolling. An item
  can legitimately yield zero materials if every one of its reward lines misses its
  roll — the item is still consumed.
- This routing lived on `ItemRarity` itself for one PR (`salvageMaterialsByType`,
  scoped per item type) before moving here as a flat per-rarity list with no type
  breakdown — everything salvage-related is configured on `SalvageSettings` now, not
  spread across `ItemRarity` assets.

## Materials are inventory items

Salvage rewards are granted as ordinary carried `Item` instances instead of a separate material wallet:

- `SalvageRewardTotal.material` (on the preview) is a plain `Item` reference. There is no `SalvageMaterialDefinition` type and no `CharacterSalvageState.materials`/`Get`/`TryCredit` wallet API — `CharacterSalvageState` now only stores idempotency receipts.
- `SalvageSettings.ValidateRewards` rejects a reward whose material is stackable but has `stackCapacity <= 0`, since that configuration can't be granted.
- `SalvageService.TryCommit` grants each material total via a private `TryGrantMaterial` helper: it inserts full-capacity stacks (`Item.stackCapacity`) via `Inventory.TryAddItem` when the material is stackable, or one instance per unit otherwise. Every inserted instance is tracked and removed again if the commit later fails, the same rollback pattern already used for returned socketables.
- There is no numeric material cap anymore (`SalvageSettings.materialCap` was removed). The natural limit is carried-inventory grid space: a commit that runs out of room fails with "Make space for the `<Item>` reward" and rolls back everything, exactly like an insufficient-space socketable return.
- Content implication: author salvage-reward `Item` assets as stackable (`canStack = true`, a real `stackCapacity`) and non-equippable. A non-stackable reward material will consume one grid cell per unit, which is almost certainly not what's wanted for a common salvage byproduct.

## Verification record / remaining Editor acceptance

Programmatic checks run here: `git diff --check` passed. Static inspection confirms all authoritative reward mutation is in `SalvageService.TryCommit`; UI only requests preview/commit.

Run in Unity before release:

- Force script reimport and confirm zero Console compiler errors.
- Add an `itemOverrides` entry for a Weapon `Item` (6 Metal + 2 Essence, both `dropChance = 1`) and an Armor `Item` (4 Hide + 1 Essence), using stackable Item assets for Metal/Essence; confirm combined 6/4/3 output lands in the carried inventory as stacked items and survives reload. Separately, set one reward line's `dropChance` below 1 and confirm across repeated salvages that it's sometimes granted and sometimes not, all-or-nothing, never a partial quantity. Also add a `rarityOverrides` entry for the Rare rarity and confirm a Rare item with no item override picks it up, while a Rare item that *does* have an item override still uses that instead.
- Exercise every acceptance row in the implementation guide, especially duplicate request replay, full inventory with socket returns, an inventory too full to receive the granted materials, a deliberate `GameSave.Save` failure, post-save listener exception, old-save migration, provider loss, profile switch, and repeated open/close/respawn.
- Regression-check equip, sell, drop, inventory sort, blacksmith socket removal, and old Binary/JSON/PlayerPrefs save loading.
- Add and serialize the shared Merchant-style tab prefab, ToggleGroup, tab container, Salvage section, and references on the existing Blacksmith window prefab and assign salvage settings on the existing Blacksmith NPC. No separate salvage window/provider should be created. The prefab was not modified automatically because UI layout/reference choices require Unity Editor serialization.

## Known boundaries

- There is no pre-existing favorite/loadout system or quest-equipment subtype; the added item-instance metadata (favorite, locked) is the authoritative protection store.
- Existing socket insertion intentionally reduces a socketable to a fresh instance of its definition. Salvage preserves the exact state that actually exists in `sockets`; changing socket insertion/state persistence is outside salvage scope.
- Runtime notification events from the underlying inventory fire while the draft is installed. The existing project has no mutation/event batching API; the salvage window refreshes only after commit. Other listeners must not autosave independently during `TryCommit`.
- Materials now compete with equipment for carried-inventory grid space (see "Materials are inventory items" above); there is no reserved, uncapped-by-grid storage for them anymore.
- Step 11 random rewards, refunds, transmog, and achievements are not implemented.
