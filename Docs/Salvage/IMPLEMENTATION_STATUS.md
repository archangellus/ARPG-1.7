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
2. **Complete (code/configuration types)** — `SalvageMaterialDefinition`, `SalvageRecipe`, and `SalvageSettings` each live in a same-named source file so Unity can create their ScriptableObject assets. Routing rules, overrides, fallback, version fingerprint, caps, and high-value rarities are authorable. Rules select override first, then unique highest priority, then fallback; malformed recipes are rejected.
3. **Complete** — stable GUID identity and favorite/junk/lock metadata are serialized on each item. Old saves lazily receive an ID. Wallet balances and operation receipts are character-scoped and serialized in the same character snapshot.
4. **Complete** — `SalvageService.Evaluate` is shared by manual and bulk UI paths. Only carried equipment with a valid recipe is eligible. Favorite/lock protections are enforced; favorite and junk are mutually exclusive.
5. **Complete** — previews bind item IDs, revisions, provider, rule fingerprint, operation ID, deterministic material totals, returned socket instances, and high-value confirmation.
6. **Complete** — commit removes all selected equipment before inserting exact attached socket instances, so newly freed grid cells count. Any insertion failure rolls the runtime draft back.
7. **Complete, awaiting Editor failure-injection verification** — commits are guarded and idempotent with saved receipts. Inventory, wallet, returned sockets, and receipt enter one `GameSerializer` save. A pre-save exception rolls runtime state back; filesystem JSON/binary saves use temp-and-replace.
8. **Complete (existing-window runtime integration); prefab wiring required** — the existing `GUIBlacksmith` now instantiates Repair and Salvage `UITab` toggles exactly like `GUIMerchant`. Its Salvage tab supports manual row calls, junk, rarity, broad eligible, clear, preview, confirmation, and close. It displays deterministic totals and returned socketables.
9. **Complete (existing-provider integration); content setup required** — the existing `Blacksmith` owns salvage settings/provider identity, receives the player through its existing interaction, checks distance/provider state at commit, and invalidates previews when its window closes.
10. **Code checks complete; Unity checks pending** — repository whitespace validation passed. No Unity executable or generated C# solution is installed in this environment, so compilation, EditMode/PlayMode tests, prefab validation, save/reload, and gameplay acceptance remain Editor checks and are not claimed as passed.

## Inspector and content setup

For a complete Unity 6000.3.15f1 click-by-click prefab, asset, and Inspector procedure, follow `Docs/Salvage/UNITY_6000_SETUP_GUIDE.md`.

1. Create material assets with **Create > PLAYER TWO > ARPG Project > Salvage > Material**. Set display name/icon; the stable ID is generated once and remains serialized.
2. Create recipe assets with **... > Salvage > Recipe** and add positive material rows.
3. Create one **Salvage Settings** asset. Add item overrides and/or rules. `rarityId = -2` means any rarity, `-1` means plain. Higher rule priority wins; equal highest matches are deliberately invalid. Assign high-value rarity indexes and a material cap. Increment `configurationVersion` whenever a runtime-relevant policy changes.
4. On the **existing Blacksmith window**, keep `GUIBlacksmith`. Assign the same `UITab` prefab used by `GUIMerchant` to `tabPrefab`, a `ToggleGroup` to `toggleGroup`, and an empty tab-row `RectTransform` to `tabsContainer`. Assign `repairTabPanel` to the existing repair/socket controls and `salvageTabPanel` to the new Salvage content root. Optionally assign the same style of `switchTabClip` used by the Merchant. `GUIBlacksmith` instantiates the Repair and Salvage tabs at runtime using the Merchant pattern; do **not** create separate tab Buttons or another `GUIWindow`.
5. In the Salvage panel assign `salvageSelectedCountText`, `salvageRewardsText`, `salvageReturnedSocketablesText`, `salvageMessageText`, `salvageRarityDropdown`, `salvageConfirmButton`, `salvageSelectJunkButton`, `salvageSelectRarityButton`, `salvageSelectAllEligibleButton`, and `salvageClearButton`. Populate the dropdown with `None` first, then `GameDatabase.itemRarities` in index order.
6. Existing inventory item rows can call `GUIWindowsManager.instance.blacksmith.SetSalvageSelected(GUIItem.item, bool)` from their selection toggle while the Blacksmith is open. This is the individual-selection boundary; rows never remove items directly.
7. On the **existing Blacksmith NPC**, assign `salvageSettings`, a scene-unique stable `salvageProviderId`, and `salvageCommitDistance`. No new provider component is needed. The existing Blacksmith interaction opens the same window on its Repair tab; the player presses the Salvage tab to switch. Material balances are available at `Game.instance.currentCharacter.salvage.Get(material.id)`.

## Selection/protection defaults

- Manual selection accepts eligible high-value items; confirmation is mandatory.
- Select Junk and rarity selection may include eligible high-value items and therefore trigger confirmation.
- Select All Eligible deliberately skips configured high-value rarities.
- Favorite, locked, non-equipment, equipped/non-carried, and missing/invalid-recipe items are blocked. Quest item definitions are non-equipment in the current model. No gold or upgrade refund is involved.
- Stash and consumables are outside scope.

## Verification record / remaining Editor acceptance

Programmatic checks run here: `git diff --check` passed. Static inspection confirms all authoritative reward mutation is in `SalvageService.TryCommit`; UI only requests preview/commit.

Run in Unity before release:

- Force script reimport and confirm zero Console compiler errors.
- Wire a Rare weapon recipe (6 Metal + 2 Essence) and Magic armor recipe (4 Hide + 1 Essence); confirm combined 6/4/3 output and reload persistence.
- Exercise every acceptance row in the implementation guide, especially duplicate request replay, full inventory with socket returns, cap overflow, a deliberate `GameSave.Save` failure, post-save listener exception, old-save migration, provider loss, profile switch, and repeated open/close/respawn.
- Regression-check equip, sell, drop, inventory sort, blacksmith socket removal, and old Binary/JSON/PlayerPrefs save loading.
- Add and serialize the shared Merchant-style tab prefab, ToggleGroup, tab container, Salvage section, and references on the existing Blacksmith window prefab and assign salvage settings on the existing Blacksmith NPC. No separate salvage window/provider should be created. The prefab was not modified automatically because UI layout/reference choices require Unity Editor serialization.

## Known boundaries

- There is no pre-existing favorite/junk/loadout system or quest-equipment subtype; the added item-instance metadata is the authoritative protection store.
- Existing socket insertion intentionally reduces a socketable to a fresh instance of its definition. Salvage preserves the exact state that actually exists in `sockets`; changing socket insertion/state persistence is outside salvage scope.
- Runtime notification events from the underlying inventory fire while the draft is installed. The existing project has no mutation/event batching API; the salvage window refreshes only after commit. Other listeners must not autosave independently during `TryCommit`.
- Step 11 random rewards, refunds, transmog, and achievements are not implemented.
