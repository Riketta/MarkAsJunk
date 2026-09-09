# Steam Workshop description

Paste the text below into the Workshop item's description field when publishing
(the BBCode renders on Steam, but not in-game - `About/About.xml` carries its own
plain-text description).

```
[h3]Mark as Junk[/h3]
Mark any item as junk and let your colonists do the sorting: haulers keep junk out of your stockpiles and carry it to dumps instead. Nothing is marked by default - you decide what counts as junk.

[h3]What it does[/h3]
[b]Junk marking[/b]
[list][*]Every haulable thing gains an extra toggle right next to the vanilla allow/forbidden button - loot, corpses, minified furniture, anything from any mod or DLC.
[*]It behaves exactly like forbidding: stack splits keep the mark, junk never merges into non-junk stacks, and the inspect line shows the state. Marked items also display a small junk icon in the world, just like forbidden items do. An optional hotkey is available under Options -> Keyboard shortcuts.
[*]Unmark an item to return it to normal storage.[/list]

[b]Junk dumps[/b]
[list][*]Select a stockpile zone or storage building and toggle "Junk dump": it keeps working as normal storage for everything its filter allows, and additionally accepts junk.
[*]While a dump exists on a map, all other storage there rejects junk, so pawns actively sort it out of your main stockpiles. Remove the last dump and junk simply flows back.
[*]Both storage flags travel with copy/pasted storage settings, and grouped shelves flag the whole group.[/list]

[b]Auto-marking (optional)[/b]
[list][*]"Mark contents as junk" turns a stockpile into a conveyor: current contents are marked right away, anything stored later on arrival. Since normal storage rejects junk, everything flows on into your dumps.
[*]Apparel a colonist takes off because it no longer passes their outfit policy (e.g. tattered clothes) is marked as junk automatically - pieces swapped out for a better one are never touched.[/list]

[b]Smelter bills[/b]
[list][*]Electric smelters gain "smelt/destroy junk" (smeltable things return part of their resources, everything else is destroyed) and "destroy junk" (much faster, no resources). Both bills consume whole stacks and feed exclusively on marked items.
[*]Junk-marked corpses cascade the mark (optional, off by default): destroying one drops the apparel and weapon it still holds as junk instead of silently destroying them with the corpse, and its inventory drops as normal unmarked loot.[/list]

[h3]Settings[/h3]
Master switch (marks and flags are kept while disabled), "junk needs an existing dump" so items can never be stranded, toggles for the smelter bills and for auto-marking discarded apparel, plus debug logging (Off / Basic / Verbose) and a one-click junk overview that lists every dump, auto-mark storage and marked item.

[h3]Things to keep in mind[/h3]
[list][*]Junk avoidance applies anywhere the game consults storage acceptance: while a dump exists, growth vats and biosculpter pods refuse junk-marked nutrition and turrets refuse junk-marked shells. Eating, trading, caravan loading and regular bills ignore junk - the smelter junk bills pull marked items on purpose.
[*]A junk dump is always also normal storage for its filter; restrict its filter and priority to approximate a dedicated junkyard.
[*]Auto-marks are permanent until items are unmarked individually, and apparel auto-marking needs a restrictive outfit policy - with the default "Anything" policy nothing ever fails the filter.
[*]Items that vanilla cannot forbid (no comp support) cannot be marked.[/list]

[h3]Compatibility[/h3]
Requires RimWorld 1.6 and [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url]; all DLCs are optional.
Works with all DLC and mods by design - the junk marker is attached to every haulable thing at runtime, nothing is hardcoded to specific items, and the smelter recipes attach through the vanilla recipeUsers mechanism. Safe to add or remove at any time.

Source code and details: [url]https://github.com/Riketta/MarkAsJunk[/url]
```
