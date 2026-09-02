# Mark as Junk

A RimWorld 1.6 mod for sorting loot you do not care about: mark any haulable
item as **junk** with a toggle button (next to allow/forbidden), flag any
stockpile zone or storage building as a **junk dump**, and haulers keep junk
out of normal storage and carry it to the dumps instead.

## How to use

1. Select one or many items (loot, corpses, minified furniture - anything
   haulable, from any mod or DLC) and hit **Mark as junk**. The toggle sits
   next to the vanilla allow/forbidden button and behaves like it: one click
   per selection (in mixed selections only the items matching the clicked
   button's state toggle, exactly like allow/forbidden), split-off stack
   pieces keep the mark, junk and non-junk stacks of the same item never
   merge (so marks cannot be silently absorbed), and the state is shown in
   the item's inspect line. An optional hotkey lives under
   *Options -> Keyboard shortcuts -> Miscellaneous*.
2. Select a stockpile zone or storage building and hit **Junk dump** (next to
   the copy/paste storage gizmos). A dump keeps working as normal storage:
   everything its filter whitelists is stored exactly as before, and the flag
   additionally lets junk-marked things in (the filter applies to junk too,
   so "steel only" dumps take junk steel only). Grouped shelves flag the
   whole group, and copy/pasting storage settings carries the dump flag
   along.
3. Optional: hit **Mark contents as junk** on a stockpile or storage to
   auto-mark everything stored there. Everything already inside is marked
   immediately, every item dropped in later is marked on arrival, and since
   normal storage rejects junk while a dump exists this turns the stockpile
   into a conveyor: stuff goes in, gets marked, haulers carry it on to the
   dump. Marks are permanent until items are unmarked individually - they
   survive leaving the stockpile and switching the toggle off.
4. While at least one dump exists on a map, every *other* storage there
   rejects junk-marked things, so pawns actively re-sort junk out of your
   main stockpiles into the dumps - and only the dumps ever hold junk. Delete
   the last dump and junk items simply flow back into normal storage
   (configurable in mod settings - see below).
5. Electric smelters get two extra bills: **smelt/destroy junk** (smeltable
   things return part of their resources, everything else is destroyed -
   exactly like vanilla's smelt/destroy recipe, priced the same: 1600 work to
   smelt, 400 to destroy) and **destroy junk** (fast, 60 work, whole stacks,
   no resources). Both accept only junk-marked items, take entire stacks per
   bill, and can be hidden in mod settings.

Nothing is marked by default; the player decides item by item, dump by dump
(or stockpile by stockpile with the auto-mark toggle).

## Mod settings

- **Enabled** - master switch. While off, routing and gizmos are disabled but
  every mark and dump flag is kept and reapplied on re-enable. Junk/non-junk
  stacks still refuse to merge while disabled, deliberately, so marks cannot
  be lost during a disabled period.
- **Junk needs an existing dump** (default on) - when on, junk items are kept
  out of normal storage only while a dump exists somewhere on their map, so
  marking can never strand items with nowhere to go. Turn it off to always
  exclude junk from normal storage.
- **Add smelter junk bills** (default on) - toggles the smelter recipes
  described above; already-created bills keep working when hidden.
- **Debug logging** - cycles Off / Basic / Verbose (see below).
- **Log junk overview** - writes one block per loaded map to the log: every
  dump and auto-mark storage (with priority) and the count of junk-marked
  items.

## Debug logging

- *Basic* logs state changes: marks/unmarks (including auto-marks on
  arrival), dump and junkify flags, patch application, routing recomputes.
- *Verbose* additionally logs every storage-acceptance override decision
  ("normal storage X rejected junk Y") and every auto-mark arrival. This is
  the hot path - expect log spam; use it only to hunt a specific routing
  question.

## How it works (technical)

- `CompJunkMark` (a `ThingComp`, the junk analogue of `CompForbiddable`) is
  injected at runtime into every thing whose `def.EverHaulable` is true by a
  Harmony postfix on `ThingWithComps.InitializeComps`. No def, category, DLC
  or mod list is hardcoded - if vanilla can haul it, it can be marked. The
  flag is saved in the item's XML (`markedJunk`) and survives stack splits.
- The dump flag lives on `StorageSettings` in a weak table and is persisted by
  a postfix on `StorageSettings.ExposeData` (`markedAsJunkDump` and
  `markedAsJunkify`) - not on zones or buildings, so stockpiles, shelves,
  storage groups and modded storages all behave identically. Both flags
  travel with copy/pasted storage settings (`StorageSettings.CopyFrom`).
- "Mark contents as junk" is a second flag on the same `StorageSettings`.
  Turning it on marks the current contents (slot groups are resolved by
  settings identity, so storage groups are covered); while it is on, items
  are marked when they arrive: a postfix on `Thing.SpawnSetup` resolves the
  slot group at the spawn cell (the same lookup vanilla does for
  `Notify_ReceivedThing`), a postfix on `SlotGroup.Notify_AddedCell` marks
  items under cells added to a junk stockpile zone, and a postfix on
  `StorageGroupUtility.SetStorageGroup` marks a building's contents when it
  joins a flagged storage group. Turning the toggle off never unmarks -
  unmarking stays a per-item decision.
- All routing goes through one Harmony postfix on
  `StorageSettings.AllowedToAccept(Thing)`, the single check that
  `Zone_Stockpile.Accepts`, `Building_Storage.Accepts` and virtually every
  modded `IHaulDestination` already funnel through:
  - junk thing + normal storage -> rejected while a dump exists on the map;
  - dump-flagged storage -> untouched: the vanilla filter decides for junk
    and non-junk alike, so the dump keeps its regular whitelist use.
  The postfix deliberately ignores settings with a null `owner`: vanilla
  re-enters this same method for parent/fixed filters (def-level fixed
  storage settings, `EverStorableFixedSettings`), and those must stay pure
  filter checks or dumps would reject junk via the parent chain.
  Everything else - filters, priorities, forbidding, factions - stays vanilla.
- "A dump exists on this map" is cached per map and invalidated by flag
  toggles, storage copy/paste and haul-destination registration changes.
- The smelter feature adds two `RecipeDef`s that attach to the vanilla
  `ElectricSmelter` through `recipeUsers` (data-driven; nothing is patched
  into the building). Their ingredient filter is a tiny `ThingFilter`
  subclass (`ThingFilter_JunkOnly`) declared in XML via the `Class` attribute:
  1.6's special filters only enforce their disallow side, so a positive
  "marked as junk" gate has to live in a filter override. A small postfix on
  `RecipeDef.WorkAmountTotal` mirrors vanilla's smelting-work special case
  (which is hardcoded to the vanilla recipe) for the junk variant.
- Toggling a mark or a dump re-runs the vanilla haulables check for the
  affected cells (all slot groups for a dump toggle), so pawns pick the work
  up immediately instead of waiting for the slow re-scan tick.

### Compatibility

- Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).
- Works with all DLC and mods by design: nothing reads specific defs, and all
  hooks sit on shared base-class methods. Modded storage buildings that
  inherit `Building_Storage` or route acceptance through `StorageSettings`
  are covered automatically; exotic storage that bypasses
  `StorageSettings.AllowedToAccept` simply ignores junk routing.
- Safe to add and remove at any time. Removing the mod leaves three ignored
  bools (`markedJunk`, `markedAsJunkDump`, `markedAsJunkify`) in the save
  file and nothing else.

## Files

```
About/                 mod metadata
Defs/                  optional hotkey def (Misc category, unbound)
Languages/English/     translations
Textures/UI/Commands   toggle icons (32x32)
Source/MarkAsJunk      C# source + csproj; Source/make_icon*.py regenerate the icons
Assemblies/            compiled MarkAsJunk.dll
```

## Building

Requires the .NET SDK. The csproj defaults to
`E:\SteamLibrary\steamapps\common\RimWorld`; override with your install path:

```
cd Source/MarkAsJunk
dotnet build -p:RimWorldDir="C:\Path\To\RimWorld" [-p:HarmonyDir="C:\Path\To\Harmony"]
```

Output lands in `Assemblies/MarkAsJunk.dll`. The whole `MarkAsJunk` folder can
be symlinked/copied into the game's `Mods` directory.

## Known limitations

- Plain `Thing`-class haulables (no comp support) cannot be marked - the same
  items vanilla cannot forbid either.
- Junk routing intentionally does not affect eating, bill ingredients, trade
  or caravan loading - it only steers *storage* (the smelter recipes are
  regular bills and therefore pull marked items deliberately).
- The smelter recipes are only added to the vanilla `ElectricSmelter`; other
  work tables or modded smelters do not get them automatically.
- When every dump on a map is full, junk items wait like any vanilla item
  with no valid storage cell.
- There is no "junk-only" storage mode: a dump always doubles as regular
  storage for its filter whitelist. To approximate a dedicated junkyard,
  restrict the dump's filter and/or give it a low storage priority - junk
  still only ever routes into dumps.
