# Mark as Junk

A RimWorld 1.6 mod for sorting loot you don't care about. Mark any haulable
item as **junk**, designate junk dumps, and your colonists keep junk out of
normal storage and carry it to the dumps instead. Optional extras: a
stockpile toggle that auto-marks everything stored in it, auto-marking of the
ragged apparel colonists discard, and smelter bills that smelt or destroy
junk.

Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).
Works with all DLC and mods by design, and is safe to add or remove at any
time.

## How to use

1. **Mark items** - select anything haulable (loot, corpses, minified
   furniture - any mod or DLC) and hit **Mark as junk**, the toggle next to
   the vanilla allow/forbidden button. It behaves like forbidding: stack
   splits keep the mark, junk never merges into non-junk stacks, and the
   inspect line shows the state. An optional hotkey is available under
   *Options -> Keyboard shortcuts*.
2. **Flag a junk dump** - select a stockpile zone or storage building and
   hit **Junk dump**. A dump keeps working as normal storage for everything
   its filter allows, and additionally accepts junk. Copy/paste of storage
   settings carries the flag, and grouped shelves flag the whole group.
3. **Auto-mark a stockpile (optional)** - hit **Mark contents as junk** and
   everything stored there is marked automatically: current contents right
   away, anything dropped in later on arrival. Since normal storage rejects
   junk, this turns the stockpile into a conveyor that feeds your dumps.
   Marks stay until items are unmarked individually.
4. **Smelt or destroy junk** - electric smelters gain two bills, both
   consuming whole stacks: **smelt/destroy junk** (smeltable things return
   part of their resources, everything else is destroyed) and **destroy
   junk** (much faster, no resources).
5. **Let colonists toss their own rags (optional)** - with a restrictive
   apparel policy (e.g. hit points above 50%), a colonist taking off apparel
   that no longer passes it drops it straight to the junk flow. Apparel
   swapped for a better piece is never touched.

While a dump exists on a map, all other storage there rejects junk, so pawns
actively sort junk out of your main stockpiles. Remove the last dump and junk
simply flows back into normal storage. Nothing is marked by default.

## Mod settings

- **Enabled** - master switch. Marks and flags are kept while disabled.
- **Junk needs an existing dump** (default on) - junk only avoids normal
  storage while a dump exists somewhere on its map, so items can never be
  stranded. Turn off to always exclude junk from normal storage.
- **Add smelter junk bills** (default on) - show/hide the smelter recipes.
- **Auto-mark discarded apparel** (default on) - apparel taken off because it
  fails the wearer's apparel policy is marked as junk; pieces swapped for
  better ones never are.
- **Debug logging** - Off / Basic / Verbose, plus a one-click **junk
  overview** that lists all dumps, auto-mark storages and marked items.

## Known limitations

- Items that vanilla cannot forbid (no comp support) cannot be marked.
- Junk avoidance applies anywhere the game consults storage acceptance, not
  just hauling: while a dump exists, growth vats and biosculpter pods refuse
  junk-marked nutrition and turrets refuse junk-marked shells (a loaded one
  is extracted). Eating, trading, caravan loading and regular bills ignore
  junk (the smelter junk bills pull marked items on purpose).
- Smelter bills are only added to the vanilla electric smelter.
- A junk dump is always also normal storage for its filter; restrict its
  filter and priority to approximate a dedicated junkyard.

## Technical notes

For modders and the curious - no def, DLC or mod lists are hardcoded:

- The junk mark is a `ThingComp` (the junk analogue of `CompForbiddable`),
  injected at runtime into every haulable thing via a Harmony postfix on
  `ThingWithComps.InitializeComps`, and saved as `markedJunk` on the thing.
- Both storage flags live on `StorageSettings` - so stockpile zones, storage
  buildings, storage groups and modded storages behave identically. They are
  persisted by a postfix on `StorageSettings.ExposeData` and travel with
  copy/pasted settings.
- Junk routing is a single postfix on `StorageSettings.AllowedToAccept`, the
  check that `Zone_Stockpile.Accepts` and `Building_Storage.Accepts` already
  funnel through. Dump-flagged storage is left untouched; every other
  storage rejects junk while a dump exists. Filters, priorities, forbidding
  and factions stay vanilla.
- Auto-marking hooks item arrival at the shared choke points
  `Thing.SpawnSetup`, `SlotGroup.Notify_AddedCell` and
  `StorageGroupUtility.SetStorageGroup`; discarded apparel hooks the one
  overload all worn-apparel ground drops funnel through
  (`Pawn_ApparelTracker.TryDrop`), deciding junk vs. upgrade with the pawn's
  own apparel policy filter.
- The smelter bills are two `RecipeDef`s attached through `recipeUsers`.
  Their ingredient filter is a small `ThingFilter` subclass
  (`ThingFilter_JunkOnly`), because "marked as junk" is per-thing state that
  no def-level filter can express.
- Debug logging (Basic/Verbose) covers marks, flags, routing decisions and
  patch application.

## Build from source

Requires the .NET SDK and a RimWorld 1.6 install. Build the Release configuration for the dll you
ship - a plain `dotnet build` defaults to Debug:

```
cd Source/MarkAsJunk
dotnet build -c Release -p:RimWorldDir="C:\Path\To\RimWorld"
```

The output lands in `Assemblies/MarkAsJunk.dll`. Copy or symlink the whole
`MarkAsJunk` folder into the game's `Mods` directory to try it.
