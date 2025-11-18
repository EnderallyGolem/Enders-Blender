# EndHelper
Calling it a blender was funnier. Too lazy to change the internal names though.  
[Source code](https://github.com/EnderallyGolem/Enders-Blender)
[Gamebanana page](https://gamebanana.com/mods/568903)

# Features
### Player Tools:
###### Room Stat Tracker
- GUI Display
    - Overlay which display the number of deaths, time spent (in-game or/and real-time), or/and number of strawberries obtained in a room (can be customised)
    - Timer can be set to freeze on pause/afk/in cutscene.
- Stats Menu
    - Menu which shows information for all rooms
    - Rooms can be fused together, segmented, or rearranged
    - Total first clear stats and one other clear's stats are saved and are viewable in the journal
    - Stats can be copied onto clipboard

###### Gameplay Tweaks
These are shown in the end-screen. Some of these can be overridden per-map with a Gameplay Tweaks Override Trigger.
- Grab Recast
    - Keybind which can act as a toggle grab keybind, or modify the grab key to be invert/toggle.
- Prevent Down Dash Redirects
    - Prevents down dashes from being redirected out of dashing down, aka prevent manual demos (except upward ones), by forcing them to be down or down diagonal
    - Does not affect demo keys.
- Neutral Drop Keybind
- Backboost Keybind

###### QOL Tweaks
- Quick-Retry Keybind
    - Instant Retry. Disabled when carrying a golden.
- Disable Quick-Restart Key
- Disable Frequent Screen Shakes
    - Disables screen shake from dashes, boosters, springs, (vanilla) refills, and the death animation
- Restart/Quit Map Cooldown
    - Disables quit/restart map in the menu for a short while to prevent accidental clicking

###### Misc
- Freeze Level Timer
    - Option to freeze the level timer (+ journal timer) on pause and/or while afk. 
    - Does not affect the file timer.
- [Portable Multi-room Watchtower](#multi-room-watchtower)

### Mapping Tools:
###### Room-Swap
- Create a grid of rooms that can swap positions with each other (baring some limitations: no collectables and FG/BG tiles).
- You can check out how they work in my [Crossroads Contest map](https://www.youtube.com/watch?v=xB6RLAKZC0g).
	- Setup a grid with Room-Swap Controller (ensure it is loaded before entering the grid)
	- Create template rooms (with names matching the controller) and actual rooms of the same size. Actual rooms are empty, template rooms have the actual room.
	- Add Room-Swap Respawn Force Same Room Triggers in each template room.
	- Use Updating Change Respawn Triggers instead of the regular trigger.
	- Change room order using Room-Swap Breaker Box or Room-Swap Modify Room Trigger.
	- Create a map with Room-Swap Map. Implement map upgrades with Room-Swap Map Upgrade.

###### Cassette Entity/Triggers
- Cassette Beat Gates
    - Blocks that move along nodes in accordance to cassette beats
    - Can be dependent on bar progress or full track progress, as well as flags
    - Option to move entities/triggers/decals within it instead
- Cassette Manager Trigger
    - Set varying tempo speeds (speed multiplies at predefined beats, either set or multiply existing)
    - Change the current beat
    - Can be different depending on entering/exiting trigger, flags, or if within beat range
- Both of these have support for Quantum Mechanic's wonky cassettes.

###### Multi-Room Watchtower
- Watchtower that can view multiple rooms (blocked by Lookout Blocker as per usual). Works both as normal and with a node path, with modifiable scroll speed.
- Keybind to use anywhere

###### Tile Entity
- Foreground tile entity that allows customising: 
    - Entity depth
    - Connections to the same/different tile entities or the edge of the room, 
    - Whether/what direction it renders off-screen in (Note: done via an invisible tileset)
    - If the seed depends on the relative position in the room
- Option to be breakable

###### Conditional Bird Tutorial
- Tutorial bird which flies in when certain conditions are met:
	- Certain time in room / part of room (total or at once)
	- Certain number of deaths in room / part of room
	- Flag enabled (either as a seperate condition, or required for the above 2 conditions to increment)
	- If on screen

###### Misc
- Incremental Flag Trigger
	- Increments a counter when you reach the trigger with a specific value, which then sets a flag
- Flag Killbox

## 1.1.16 changelog:
- Room stat tracker crash fix

## 1.1.15 changelog:
- Room Stats Tracker:
	- New tracked stat: RTA Timer. (Thanks hyper for the suggestion!)
		- Uses your system time to track the time, hence is unaffected by freeze frames, slowdown, the game-speedup from speedrun tools fast respawn, etc. This is tracked seperately from the regular timer.
		- Display can be toggled for both the hud and menu
		- (Note that for maps/rooms played before this update, RTA timer will be 0)
	- Fixed crash in 7a credits, and a room-rename crash

## 1.1.14 changelog:
- Hopefully a crash-fix for room stats? At this point I have zero clue why they are happening and just adding an empty room name check.

## 1.1.13 changelog:
- Room Stats Tracker:
	- Changed Save Clear default to save if valid clear (old behaviour) so it doesn't interrupt you if you aren't using the blender for stats
	- The ask menu now properly pauses the game when ending the map with a complete area trigger. This might also possibly fix a crash?????????
- The test map has also been removed from the mod (if you want to view it, download from the github.)
			
## 1.1.12 changelog:
- Potential crash fix with cassette manager trigger with wonky cassettes
- fixed 1.1.11 crashing oops
			
## 1.1.10 changelog:
- New Additions:
	- Mapping Tools:
		- Connectable Outline - Outline indicator which can be (visually) connceted to each other and attached.

- Room Stats Tracker:
	- The Previous Clear stats (viewed in journal) is renamed to Saved Clear.
		- In settings, you can configure whether if beating a map overrides the previous Saved Clear.
		- By default, it prompts you if you would like to override the saved stats upon a clear.
		- If there isn't already a Saved Clear, the stats will always save.

- QOL Tweaks:
	- Autosave can now also occur during load state.

- Gameplay Tweaks:
	- Seemless Respawn: Fixed spinner flicker during death for spinners with custom hues.

- Mapping Tools:
	- Conditional Bird Tutorial
		- Modified behaviour of Only Fulfill Condition Once being disabled together with Only Once Fly In enabled.
			- If the condition is still met upon death, the bird does not fly in. Otherwise, it still does.
			- Previously Only Once Fly In did nothing together with Only Fulfill Condition Once disabled (the bird would fly in regardless).
		- Added option - Require Flag For Increment.
			- If specified, the second and death conditions will not increment unless this flag is set.
	- Room-Swap Map: HUD option now moves it to the Sub-HUD layer.

- Misc:
	- Multi-room Watchtower: Fixed issues when you die while using it. You also now just can't die when using it.

## 1.1.9 changelog:
- New Additions:
	- QOL Tweaks:
		- Autosave. Automatically saves the game every few minutes.
	- Mapping Tools:
		- Incremental Flag Trigger. Flag trigger that makes requiring flags to be triggered in a specific order easier.
		- Flag Killbox
	
- Gameplay Tweaks:
	- Fixed Seemless Respawn bugging out when clicking retry in the menu
	
- Mapping Tools:
	- Conditional Bird Tutorial: Added option - Only Fulfill Condition Once, which means once the bird flew down once, it can fly down again without fulfilling the condition. (This was previously the default behaviour.)
	- Tile Entities: 
		- Added options: colour, collidable, and background tiles.
		- Hopefully improved the accuracy of the connections shown in Loenn.
	
- Misc:
	- Freeze Timer When Pause/AFK: Icons now only show up when game is paused.
	- Fixed a crash that can happen on the first clear of 3a (or whenever a map ends on a room transition).

## 1.1.8 changelog:
	- Fixed crash when trying to enter an unloaded map
	- Fixed crash from having tracker storage size set to 0, then increased mid-level
	- Fixed crash from checking an empty nonexistent room (i don't really know what causes this but i know how to fix it yay)
	- Fixed the issue of a previous session room sometimes showing up in the stats tracker

## 1.1.5 changelog:
- Updated to NET 8.0

- Gameplay Tweaks:
	- Tweaks are now tracked individually, and each used gameplay tweak is shown in the endscreen.
	- Added a Backboost keybind.
	- Modified Neutral Drop keybind so it sets MoveY to 1 and throws, so it should be more akin to pressing down for 1 frame and throwing.
		
## 1.1.4 changelog:
- patch for viewing journal stats in some maps
		
## 1.1.3 changelog:
- seemless respawn crash patch

## 1.1.1 changelog:
- New Additions:
    - Gameplay Tweaks:
        - Seemless Respawns: Changes respawns to be more seemless by reloading the room without a wipe. This is kind of buggy and only meant for maps (enabled with gameplay tweak trigger) for now.

- Room Stats:
	- You can name a room dialog %skip to prevent it from showing up (previous room stats will be incremented instead)
	- Made custom room names for SJ Heartsides
	- Menu no longer freezes if viewed from the journal, in the world map, without imguihelper

## 1.1 changelog:
- New Additions: 
    - QOL Tweaks:
        - Disable Frequent Screen Shakes: Disables screen shake from dashes, boosters, springs, (vanilla) refills, and the death animation, while keeping the rest because they look cool
        - Restart/Quit Map Cooldown: Option to disable quit/restart map buttons for a short while
    - Gameplay Tweaks:
        - Neutral Drop Keybind. it does a neutral drop. yay.
        - Blender Gameplay Tweaks Override Trigger: Lets you temporarily enable some gameplay tweaks (currently only the down dash redirect) in a map.
    - Mapping Tools:
        - Conditional Bird Tutorial: An everest tutorial bird which starts off off-screen, and flies in when conditions (flag, time in region, deaths in region) are met.
- Room Stats:
    + Room stats are now tracked and can be viewed in the journal! The first clear total stats (TOTAL stats when map is first cleared) and previous clear's stats (only if it's a valid clear, from the start) are saved.
    + New Room Stat GUI option: Alive Timer. This is largely meant for estimating the length of a room when map making, and has nothing to do with room stats whatsoever.
    + New Room Stat GUI option: Hide If Golden.
    + Added ability to change room order and fuse rooms together (this also means segmenting can be undone)
    - Replaced Custom Name Storage Size setting with Tracker Storage Size
    - Removed the room stat counters, they don't make any sense to have when they are affected by settings and with the different stat types
    - Refixed death counts being spammed with speedruntool bino (+ possible other situations?). Apparently I accidentally undid the fix at some point. Yay.
- Room-Swaps:
    - Room-Swaps now work properly with debug and save & quit + restart game.
    - Room-Swaps Box/Trigger: The Set modification type now lets you exclude rooms from being changed.
    - Flag checks uses the same checks as Cassette Beat Trigger/Blocks (checks for multiple and negation). Flag toggles are specified seperately (and you can list multiple), and only toggle if swapping is successful.

- Multi-room Watchtower:
    - Watchtower now retains your respawn point after viewing it (rather than changing it to the closest point after viewing).
    - Watchtower no longer shifts the player position to the viewed room. This should avoid it somehow triggering stuff unintentionally. Possibly.

- Cassette Beat Trigger/Blocks:
    - Fixed cassette beat triggers crashing without Quantum Mechanics mod.
    - Cassette Beat Triggers/Blocks: Updated flag checks to allow checking for multiple checks

- Misc:
    - Rearranged the settings a bit. You might have to reconfiger some settings!
    - Using gameplay tweaks (grab recast, down dash redirect, neutral drop key) now causes the end screen to show that variant mode is used


### 1.0.5 changelog:
- New Additions:
    - Cassette Beat Gates: Blocks that move along nodes in accordance to cassette beats.
    - Cassette Manager Trigger: Trigger that lets you set varying tempo speeds and changing beats for cassette themes.
- Room-Swap:
    - Room-Swap maps have a HUD layer toggle, which lets them be shrunken without becoming pixelised.
    - Room-Swap Change Respawn Trigger now has an option to ignore solid checks.
    - Room-Swap Set no longer crashes if modifying from outside the grid
- Tile Entities:
    - Tile Entities additional options: Can set a SurfaceSoundIndex and turn into a Dash Block
    - Tile Entities now show connections in loenn... not that accurately. (I can't figure out how to make it work properly)
- Misc:
    - Fixed Multiroom Bino not working properly for zooms, and having blockers sometimes not block room transitions
    - Maybe possibly fixed room stats resetting upon save & quit if your game is drunk????
    - Replaced speedruntool hook with interop