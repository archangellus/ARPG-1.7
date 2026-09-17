# Unity ARPG Salvage System - Codex Implementation Guide

Prepared for Angel

## 1. What this guide builds

A Diablo IV-inspired equipment salvage system for an existing Unity ARPG project. The player visits a salvage provider, selects unwanted equipment, previews the materials and confirms the conversion. The system consumes the selected item instances and grants their rewards together.

This is a proposed implementation specification, based on the salvage sections of the accompanying Diablo IV document. The names, sample yields and policies below are project design choices. They are not Blizzard's internal code or a reproduction of a particular Diablo IV patch.

**The deliverable from Codex should be working project integration:** runtime code, configuration assets, a connected UI, persistence changes where necessary and setup instructions. This file provides the implementation sequence; it does not contain compiled or project-tested Unity scripts.

Your ARPG project has used the `PLAYERTWO.ARPGProject` namespace and types such as `Item`, `ItemInstance`, `GUIItem` and `GUIWindow`. These are discovery hints. Codex must inspect the actual repository and verify their current names and APIs before implementing anything.

## 2. How to use this with Codex

1. Make this Markdown file available in the Codex session that has access to your Unity project.
2. Paste the starter prompt below.
3. Let Codex inspect the project and execute Steps 1-10 in order. Each step has a completion gate.
4. If you prefer separate sessions, give Codex the full guide and the individual prompt for the next unfinished step. Keep the implementation status file in the project so progress survives a new session.
5. Step 11 covers optional extensions. Enable them only when their corresponding project systems exist or when you explicitly request those additional systems.

### Starter prompt

```text
Implement the attached Salvage_System_Codex_Implementation.md in this Unity
project. Complete Steps 1-10 in order, using the behavior contract in the guide.

Start by inspecting the actual repository, its applicable AGENTS.md files,
ProjectSettings/ProjectVersion.txt, package manifest, assembly definitions and
the inventory, equipment, UI, interaction and save systems.

Reuse existing project types and services. Treat suggested names in the guide
as new components only where equivalent functionality does not already exist.
Do not invent members on Item, ItemInstance, GUIItem or other existing types.
If required source files are unavailable, identify the exact missing files or
types and ask me to provide them. Do not search the internet for project code.

Preserve existing inventory behavior, serialized asset references and old saves.
Keep the implementation focused on equipment salvage. Use deterministic rewards
for the first complete implementation. Follow the transaction requirements:
item removal, materials, returned socketables and the completion receipt must
persist as one outcome.

Maintain Docs/Salvage/IMPLEMENTATION_STATUS.md with actual integration paths,
completed steps, verification results and remaining work. Follow the project's
existing documentation convention if it uses a different location.

Continue through the required steps without asking me to approve routine
implementation choices. Report genuine blockers precisely. If Unity or a
required test runner cannot run here, distinguish checks you performed from
the remaining Editor checks. Never claim an unrun check passed.

At completion, provide changed files, exact Inspector assignments, how to open
the salvage UI, how to configure yields and the results of the acceptance checks.
```

## 3. Behavior contract

These defaults remove ambiguity during implementation. Make configurable policies explicit rather than spreading them across UI callbacks.

| Area | Required initial behavior |
| --- | --- |
| Access | Salvage is available through a configured NPC or interactable provider using the project's interaction system. |
| Input | Carried equipment instances. Equipped items must be unequipped before selection. Stash contents and consumable stacks are outside the initial scope. |
| Output | Deterministic material quantities, plus returned socketables when supported. No gold charge and no gold reward. |
| Protection | Favorite, locked, quest-protected or explicitly non-salvageable items are blocked in both UI and service code. Respect existing loadout protection if the project has it. |
| Item binding | Account-bound or character-bound does not automatically mean non-salvageable. Apply the project's actual restrictions. |
| Selection | Individual selection, Select Junk and selection by configured rarity. Selection only prepares a batch; it does not destroy items. |
| Broad selection | Select All Eligible skips configured high-value rarities by default. Explicit selection or Select Junk can include them, with clear confirmation. |
| Identity | Track stable item instance IDs. Inventory positions, names, definition IDs and Unity object instance IDs cannot identify an owned copy. |
| Batch | One confirmation operates on one exact set of instances. If a selected item becomes invalid, reject the whole batch and refresh the preview. |
| Rewards | One base salvage recipe per item, with its material rows aggregated. Explicit extension providers can add other outputs later. |
| Missing recipe | The item is ineligible with a reason. Do not remove it for zero rewards. |
| Materials | Prefer an existing material wallet. Otherwise add a saved material balance separate from the equipment grid. |
| Overflow | Reject the transaction if its complete result cannot be stored. Never silently clamp rewards or drop them into the world. |
| Sockets | Return their actual contents without losing state. Until the return adapter works, reject socketed items with an explanation. |
| Upgrade refunds | Disabled initially. Enable only through a documented refund policy and reliable cost history. |
| Random bonuses | Disabled initially. They are an optional extension after the deterministic system works. |
| Persistence | A successful result includes item consumption and every reward in the same durable outcome. |

**Important distinction:** favorite and junk are item-instance metadata. A flag on a shared item definition would affect every copy of that item.

## 4. Implementation map

| Step | Result | Depends on |
| --- | --- | --- |
| 1 | Project integration map and baseline | Actual project files |
| 2 | Salvage definitions and validated configuration | Step 1 |
| 3 | Instance identity, material storage and persistence foundation | Steps 1-2 |
| 4 | Shared eligibility and selection rules | Steps 2-3 |
| 5 | Deterministic resolver and immutable preview | Steps 2-4 |
| 6 | Socket return planning | Steps 3-5 |
| 7 | Complete, durable salvage transaction | Steps 3-6 |
| 8 | Connected salvage window | Steps 4-7 |
| 9 | Provider, player lifecycle and sample setup | Steps 7-8 |
| 10 | Acceptance verification and handoff | Steps 1-9 |
| 11 | Optional refund, collection and random-reward extensions | Verified base implementation |

Suggested component names below describe responsibilities. Merge or rename them to fit existing project conventions.

| Responsibility | Suggested component |
| --- | --- |
| Authorable material metadata, if absent | `MaterialDefinition` |
| Authorable salvage outputs | `SalvageRecipe` |
| Recipe routing and protection policies | `SalvageSettings` |
| Actual project inventory integration | `SalvageInventoryAdapter` |
| Eligibility and blocked reasons | `SalvageEligibilityEvaluator` |
| Pure reward calculation | `SalvageRewardResolver` |
| Ownership of preview and commit | `SalvageService` |
| Persistent quantities, if absent | `MaterialWallet` |
| Visible interface | `GUISalvageWindow` |
| World interaction entry point | `SalvageProvider` |

Avoid creating an interface for every class. Put adapters at real boundaries, particularly inventory and persistence, where the implementation must accommodate existing code.

---

## Step 1 - Inspect the actual project

**Goal:** discover how the current game represents, removes, saves and displays an owned item.

### Codex prompt

```text
Perform Step 1 of the salvage guide. Inspect the repository before changing
gameplay code. Locate the actual item definitions, item instances, inventory,
equipment, sockets, rarity data, material storage, save/load, UI windows,
item rows, input bindings, NPC interactions and player-spawn lifecycle.

Record the actual file paths and methods that future salvage code should use.
Inspect inventory removal side effects, automatic saves and emitted events.
Determine whether multi-item changes can be staged and committed safely.

Create the integration map and implementation status document. Record baseline
compiler or test failures separately from salvage work. Then continue to Step 2
if the required dependencies are available.
```

### Required findings

- Does each owned item already have a persistent unique ID? Is it stable through save/load and inventory sorting?
- Is an item instance a reference type or a value type? Could copying it accidentally duplicate identity or discard later modifications?
- Which code removes, equips, sells, drops, splits or transfers items?
- Which operations trigger callbacks or save immediately? Can those effects be batched?
- How are socket contents represented: complete item instances, serialized records or definition references?
- Which save owns inventory and materials? Are they separate files, separate stores or one combined state?
- How does the UI obtain the active player when the player is created after scene load?

**Completion gate:** every required integration has a real path or an explicitly identified missing dependency. A guessed `inventory.RemoveItem()` call is not an integration plan.

## Step 2 - Define materials, recipes and settings

**Goal:** make salvage behavior authorable in the Inspector.

### Codex prompt

```text
Perform Step 2. Reuse existing material definitions and identifiers when suitable.
Otherwise add material definition assets with stable serialized IDs, display names
and icons. Add salvage recipe assets and settings using the actual project item
categories and rarity representation.

Implement deterministic recipe routing: an explicit per-item-definition override
first, then the highest-priority matching category/rarity rule, then an explicitly
configured fallback. Exactly one base recipe wins. Reject ambiguous equal-priority
matches, missing material references, invalid quantities and duplicate IDs.

Expose high-value rarity protection and provider requirements in settings.
Provide actionable validation messages. Reuse the project's assembly and namespace
conventions. Do not write runtime balances into ScriptableObject assets.
```

### Minimum data

| Data | Fields or meaning |
| --- | --- |
| Material | Stable serialized ID, display name and icon; reuse existing fields where present. |
| Material amount | Material reference and positive whole-number quantity. |
| Recipe | Stable recipe ID and one or more material amounts. |
| Routing rule | Explicit conditions using real item data, priority and recipe reference. |
| Configuration version | A revision or fingerprint that invalidates previews when relevant rules change. |
| Protection settings | High-value rarity set and supported bulk-selection policies. |

Generate stable IDs once. Preserve them when assets are renamed. Detect copied assets with duplicate IDs rather than silently accepting both or regenerating every ID during validation.

**Completion gate:** designers can assign recipes to real equipment. Invalid or ambiguous configuration makes affected items ineligible without deleting them.

## Step 3 - Establish identity, storage and save ownership

**Goal:** give the conversion a reliable destination and a safe persistence boundary.

### Codex prompt

```text
Perform Step 3. Reuse the project's persistent item IDs. If missing, add stable
instance identity and a migration that assigns IDs to old saved items exactly once.
Freshly created copies need new IDs; loading or moving an existing copy preserves
its ID. Detect duplicates across relevant owned containers.

Reuse a material wallet if it exists. Otherwise add material balances to the same
save ownership scope as inventory, with a serializable representation compatible
with the project's save system and a runtime lookup by material ID.

Implement or adapt revision tracking for every relevant inventory mutation,
equipment change, protection change, socket change and material update. Identify
how a composite salvage state can be saved as one complete outcome. Build the
persistence adapter needed for Step 7 before enabling destructive salvage.
```

### Required persistence decisions

- Use the current character/profile scope by default. Do not introduce account-wide material sharing unless the project already has it or it is requested.
- Preserve old saves with defaults for new fields. Use explicit schema migration when necessary.
- Use the save system's supported representation. For example, a runtime dictionary can be serialized as ID/quantity records if the existing serializer needs that.
- Perform arithmetic with checked bounds. Reject overflow and reward-cap violations; never lose excess material silently.
- A C# lock, `PlayerPrefs.Save()` call or pair of independent save calls is not proof of a complete inventory-and-wallet transaction.
- Prefer one authoritative saved snapshot. If the project requires multiple stores, use a recoverable transaction journal or an equivalent existing facility. Document recovery and write-failure behavior.

**Completion gate:** item IDs and material counts survive a reload. Old saves load correctly. The planned commit can persist all affected state together.

## Step 4 - Implement eligibility, protection and selection

**Goal:** make the service and UI agree on what can be salvaged.

### Codex prompt

```text
Perform Step 4. Implement one eligibility evaluator shared by UI, preview and
commit. Return a structured result with an eligible flag and a readable blocked
reason. Evaluate actual ownership, location, equipment state, quest/lock/favorite
protection, loadout restrictions, supported item type and recipe availability.

Reuse junk/favorite metadata if present; otherwise add it to item instances or an
existing saved instance-metadata store. Favorite and junk must be mutually
exclusive. Favoriting clears junk. A junk toggle on a favorite should be refused
until the player explicitly removes favorite protection.

Implement manual, junk, rarity and broad eligible selection using stable instance
IDs. Every selection mode must use the same protection rules. Broad selection
skips high-value rarities by default. Explicit high-value selection requires
confirmation later. Invalid candidates must have readable exclusion reasons.
```

Selection operates on the active player's carried equipment. The initial UI does not reach into the stash or another character's inventory.

If the panel later gains search or display filters, define whether bulk actions use all carried equipment or only visible equipment and label that scope clearly. Do not leave the behavior implicit.

**Completion gate:** a favorite, equipped or quest item cannot enter a valid batch even when requested directly through the service. Inventory sorting cannot change the selected owned copies.

## Step 5 - Build deterministic rewards and a preview ticket

**Goal:** confirm a specific conversion rather than whatever happens to occupy a slot later.

### Codex prompt

```text
Perform Step 5. Build a pure deterministic resolver that reads immutable item
snapshots and the selected recipes, then aggregates material amounts with checked
arithmetic. Reject empty selections, duplicate item IDs and missing or invalid
reward definitions.
Do not remove items, grant rewards, save data or play effects during resolution.

Add preview creation through SalvageService. The service owns the immutable
preview ticket, selected instance IDs, owner/profile identity, relevant revisions,
configuration fingerprint, required confirmation flags and exact reward result.
The UI displays this data and cannot supply authoritative reward quantities.

Invalidate the ticket if relevant state changes. A new preview must not silently
replace a changed item with another copy of the same definition. Use a short-lived
ticket valid only in the current service/profile session. Previewing and closing
the window must leave saved gameplay state unchanged.
```

### Preview contents

| Field | Purpose |
| --- | --- |
| Ticket ID and operation ID | Identify the exact proposed conversion and its eventual receipt. |
| Owner/profile and provider context | Prevent reuse against a different player or invalid interaction. |
| Item IDs and relevant state revisions | Detect stale or changed inputs. |
| Rule fingerprint | Detect changed recipes or policies. |
| Items to consume | Show the exact selection and count. |
| Material totals | Show the guaranteed deterministic result. |
| Returned socketables | Added by Step 6, including their preserved state. |
| Confirmation requirements | Identify high-value selected items and irreversible consumption. |

**Completion gate:** two previews of unchanged items give identical results. A stale preview is refused rather than adapted behind the player's back.

## Step 6 - Plan socket returns and capacity

**Goal:** recover socket contents without duplicating them or losing their properties.

### Codex prompt

```text
Perform Step 6 using the actual socket implementation. Add socket contents to the
proposed reward result while preserving their type, quality, modifiers, quantity
and existing instance identity where the project's model defines one.

Simulate the final inventory after removing every selected equipment item and
returning every socketable. Respect real stack limits, grid placement, weight or
other capacity rules. Include slots freed by the consumed equipment. Combine
compatible returned stacks through existing stacking rules.

If all socketables cannot be returned, reject the entire batch with a clear
capacity message. Do not strip sockets during preview, create base-definition
copies that lose rolled state or drop overflow items on the ground.

If this project has no socket system, record that the adapter is inapplicable.
If sockets exist but required source is unavailable, keep those items blocked
and identify the missing dependency before claiming full integration.
```

**Completion gate:** a socketed item produces its expected materials and exact socket contents in the draft result. The result accounts for newly freed space. Capacity failure consumes nothing.

## Step 7 - Commit the whole conversion once

**Goal:** make the saved result and runtime result agree, including failures and retries.

### Codex prompt

```text
Perform Step 7. Implement commit through the service and the persistence boundary
established in Step 3. Serialize relevant mutations for the active inventory/profile
and coordinate with autosave. Respect Unity main-thread requirements for runtime
objects and UI. A disabled button alone is not a concurrency guard.

Resolve and validate the owning profile context before accessing its scoped
receipt store. Then check whether the operation already has a committed receipt.
Replays return that receipt without applying rewards or emitting another success
event. Reject reuse of an operation ID with different bound request data.

Under the mutation gate, revalidate the immutable ticket, ownership, provider,
revisions, rules, protections and final capacity. Build a complete next-state
snapshot containing item removals, material balances, returned socketables,
updated revisions and the completion receipt.

Persist that complete outcome using the project-supported durable commit.
Then install or reconcile the committed runtime state, consume the ticket and
publish one committed-result notification. Buffer intermediate inventory events
so listeners and autosave cannot observe half the conversion.

If persistence fails before commit, leave the prior authoritative state unchanged
and publish no success. If persistence succeeded but a UI callback or later runtime
notification fails, retain the committed result and reconcile from it. Do not
undo a committed save or report that nothing happened.
```

### Transaction order

1. Validate the owning profile context, then check its completed-operation receipt before treating consumed input items as an error.
2. Enter the inventory/profile mutation gate.
3. Recheck the receipt, ticket and all relevant state under that gate.
4. Build and validate a separate proposed next state.
5. Persist the complete next state, including the receipt.
6. Install or reconcile the runtime state and mark the ticket consumed.
7. Leave the mutation gate.
8. Notify the UI and effects once.

Pending previews expire on session/profile changes or reload. They are not restored as new opportunities to apply old requests. If old receipts are pruned, unknown expired tickets must still be rejected without mutation. Never reuse ticket or operation IDs.

If persistence is asynchronous, reserve the transaction until completion and route conflicting inventory changes through the same coordinator. Closing the window does not cancel a conversion that has already crossed the durable commit point.

**Completion gate:** before-commit failure leaves the old complete state. After-commit reload produces the new complete state. No outcome contains the consumed item alongside its salvage rewards.

## Step 8 - Connect the salvage window

**Goal:** expose the service through the project's existing UI conventions.

### Codex prompt

```text
Perform Step 8. Create or adapt a salvage window using the project's existing
window framework, item rendering, tooltips, localization and text component type.
Reuse GUIItem/GUIWindow only after confirming their current interfaces.

Connect individual selection, Select Junk, rarity selection, Select All Eligible,
Clear Selection, Confirm and Close. Display selected-item count, exact material
totals, returned socketables and blocked reasons. Visually distinguish favorites,
junk and high-value items using existing project conventions.

Confirmation must consume the service-owned preview ticket. Keep Confirm disabled
for an empty selection, stale preview, invalid provider, capacity failure or active
operation. High-value items must be named or clearly listed in the confirmation.

Refresh from actual inventory and wallet events. Register and unregister listeners
with the existing lifecycle. Preserve selection by instance ID where still valid.
Use project input actions for keyboard/controller support and prevent UI input
from simultaneously triggering gameplay actions.

Create and wire the prefab if the available project tools support doing so
reliably. Otherwise provide exact Inspector assignments and identify the manual
setup still required; do not claim the scene integration is complete.
```

### Minimum Inspector wiring

| Reference | Connect to |
| --- | --- |
| Service/context | The active player's salvage context supplied by the provider or existing dependency mechanism. |
| Equipment row prefab and container | Existing compatible item-row rendering or a small adapter prefab. |
| Material reward row prefab and container | Material icon, name and quantity presentation. |
| Returned socketable container | Rows showing the socketables that will be recovered. |
| Count and message text | Selected count and operation/blocked feedback. |
| Selection controls | Junk, rarity, broad eligible selection and clear. |
| Confirmation controls | Confirm and the high-value confirmation panel. |
| Close control | Existing window close behavior. |

Keep progress messages about the game operation: for example, "Salvaged 3 items" or "Make space for returned gems." Avoid exposing transaction IDs or save implementation details to the player.

**Completion gate:** UI actions invoke the service. They never remove equipment or increment balances directly. Reopening the window does not accumulate listeners or duplicate callbacks.

## Step 9 - Integrate the provider and prepare sample content

**Goal:** open the feature from a real gameplay interaction and make it configurable.

### Codex prompt

```text
Perform Step 9. Integrate SalvageProvider with the project's actual NPC/interactable
system. On interaction, supply the active player, provider identity and settings to
the window. Support a player spawned after scene load. Avoid per-frame global
object searches as the binding strategy.

Apply existing distance, alive-state and interaction rules at opening and commit.
Close or invalidate pending previews when the player leaves, the provider is
destroyed, the profile changes or a scene transition occurs. Already committed
outcomes remain committed even if the UI disappears.

Create a small sample provider and recipes using existing suitable project assets.
Use clearly labeled test materials only if no suitable materials already exist.
Provide exact prefab, scene and Inspector setup instructions. Keep the setup
reusable: rerunning it must not create duplicate assets or duplicate NPCs.
```

### Example yields for a test configuration

These are invented test values. Map the labels to the project's actual rarity and category data. Unsupported rarities remain blocked until an explicit rule is configured.

| Rarity | Weapon | Armor | Jewelry |
| --- | --- | --- | --- |
| Common | 2 Metal | 2 Hide | 1 Metal |
| Magic | 4 Metal + 1 Essence | 4 Hide + 1 Essence | 2 Metal + 1 Essence |
| Rare | 6 Metal + 2 Essence | 6 Hide + 2 Essence | 2 Metal + 3 Essence |
| Legendary | 8 Metal + 3 Essence + 1 Core | 8 Hide + 3 Essence + 1 Core | 4 Metal + 4 Essence + 1 Core |

Example: salvaging one Rare weapon and one Magic armor yields **6 Metal, 4 Hide and 3 Essence**. A Legendary adds the configured high-value confirmation requirement. These values should live in assets, not switch statements hidden inside the UI.

**Completion gate:** a player can approach the provider, open the window, inspect a deterministic preview, salvage and observe the saved results after reload.

## Step 10 - Verify behavior and hand over the feature

**Goal:** prove the conversion works through its public boundary and real UI.

### Codex prompt

```text
Perform Step 10. Verify the acceptance cases in the guide. Use focused automated
tests for resolver, eligibility and transaction failures where the project supports
them, plus actual Unity integration checks for UI, prefabs and save/load.

Tests should exercise behavior through service boundaries, including deliberate
save failure and duplicate requests. Do not add tests that only repeat field
assignments or implementation details. Verify baseline behavior still works for
equipping, selling, dropping, sorting and loading old saves.

Run the relevant compile/test workflow available in this repository. Record exact
results and remaining manual Editor checks. Finish the setup and configuration
instructions, update the implementation status and remove temporary debug output.
```

### Acceptance cases

| Case | Expected result |
| --- | --- |
| Rare weapon + Magic armor with sample recipes | Exactly 6 Metal, 4 Hide and 3 Essence; both selected instances consumed. |
| Two copies of one item definition | Only the selected instance IDs are consumed. |
| Favorite, equipped, locked or quest item | Ineligible in the UI and rejected by a direct service request. |
| Junk plus favorite operations | Favoriting clears junk; a favorite cannot become junk until explicitly unfavorited. |
| High-value item | Broad selection skips it; explicit selection shows the required confirmation. |
| No recipe or invalid material reference | Clear configuration/eligibility error and no state changes. |
| Preview then sort inventory | Selection remains bound to the same IDs; refresh if the revision policy requires it. |
| Preview then sell, equip, modify or favorite one selected item | Entire old batch is rejected; nothing else is silently salvaged. |
| Socket return fits only after equipment removal | Transaction succeeds using the actual resulting free space. |
| Socket return cannot fit | Entire transaction rejected; original item and its socket contents remain intact. |
| Material cap or numeric overflow | Entire transaction rejected without truncating rewards. |
| Repeated click or same committed request replay | One removal and one reward grant; replay returns the saved receipt without another success event. |
| Save failure before commit | Old inventory, wallet and socket state remain authoritative. |
| Reload after a successful save, before UI notification | Load the complete committed result, including the receipt. |
| Success listener throws after durable commit | Rewards remain committed; reconcile the UI without rerunning the grant. |
| Provider loss or profile switch before commit | Pending preview becomes invalid and grants nothing. |
| Repeated open/close and player respawn | Correct player binding with no duplicated event subscriptions. |
| Old save migration | Stable IDs assigned once; existing equipment, materials and unrelated progress preserved. |

### Final handoff from Codex

- Actual changed and created files with a short explanation of each integration.
- Configuration asset locations and how to add a recipe.
- Exact prefab/Inspector assignments and how to place a provider.
- Supported selection modes and the chosen protection defaults.
- Save schema changes, migration behavior and transaction recovery behavior.
- Verification results, existing unrelated failures and any checks that could not run.
- Status of optional extensions, with no unsupported feature described as complete.

**Completion gate:** required acceptance cases pass in the actual project. If a required Unity check cannot run, list it explicitly and mark the integration as awaiting that verification.

---

## Step 11 - Optional extensions from the Diablo IV reference

The base equipment-to-material conversion is complete after Step 10. These features depend on other game systems and should be separate follow-up tasks. Any enabled extension must contribute to the same preview, capacity check and durable transaction.

### A. Refund previously invested materials

```text
Extend the existing salvage service with an explicit upgrade-material refund
provider. Use persisted investment history or another verified authoritative
record. Do not estimate historical spending from current recipe prices or item
level. Define which investments qualify, the refund percentage, integer rounding
and any excluded attempts or fees.

Show refunds separately from base salvage yields in the preview, then aggregate
them for the wallet grant. Reject invalid negative or overflowing results.
For migrated items without trustworthy history, apply the documented zero-refund
fallback and explain that policy in setup instructions.
```

Example policy: eligible recorded investment of 9 units, 50% refund, round down -> 4 units. This is a suggested rule, not a Diablo IV refund rate. Recipe prices that change later must not create unearned refunds.

### B. Unlock appearances

```text
Integrate salvage with the existing appearance collection. Resolve eligible
appearance IDs from the actual item definition and add missing IDs to the draft
collection state. Aggregate duplicate appearances as a set. Preview only newly
unlocked looks. Persist collection changes with item removal and materials.

If no usable appearance collection or wardrobe exists, leave this provider disabled
and report the dependency instead of building an unrelated cosmetic subsystem.
```

### C. Improve a Codex-style power collection

```text
Integrate with an existing reusable power collection if the project has one.
Use explicit transferable-power metadata and normalized ranks. Compare the stored
rank with the best eligible incoming rank for each power ID across the entire
batch. Never downgrade the collection and never treat every duplicate as one
automatic rank increment.

Use rank data rather than amplified display text. Exclude nontransferable Unique
powers by explicit metadata. Preview new or improved entries and persist them in
the same transaction. Keep this provider disabled if the destination power system
does not exist; identify that dependency clearly.
```

Example: stored rank 3 plus incoming ranks 6 and 2 produces stored rank 6. Materials still follow the normal recipes for both consumed items.

### D. Add random bonus materials

```text
Add random bonus outputs only after the deterministic system passes its acceptance
checks. Keep guaranteed amounts separate from possible bonus ranges in the UI.
Resolve the actual roll once for an accepted operation and bind it to the pending
transaction state used by persistence and recovery. Capacity checks must use that
same result.

Define retry and failure behavior so reopening previews, forcing capacity failures
or retrying a save cannot repeatedly reroll the same attempted conversion. A new
random seed on every preview is not acceptable. Use project-supported persisted
operation state or a documented stable per-item roll policy. Test that policy
explicitly before enabling the feature.
```

## Definition of done

The player can salvage unwanted carried equipment through a provider, understand the preview and receive the complete saved result. Protected items survive every selection path. Returned socketables retain their state. A stale preview, failed save or repeated request cannot create item loss or duplicated rewards. Codex supplies the actual integration and verifies it against the available project, with unrun checks identified plainly.
