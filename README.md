# EndHelper
Calling it a blender was funnier. Too lazy to change the internal names though.  
[Source code](https://github.com/EnderallyGolem/Enders-Blender)
[Gamebanana page](https://gamebanana.com/mods/568903)

# Features
### Player Tools:
###### Room Stat Tracker
- GUI Display
    - Overlay which display the number of deaths, time spent, or/and number of strawberries obtained in a room (can be customised)
    - Timer can be set to freeze on pause/afk/in cutscene.
- Stats Menu
    - Menu which shows information for all rooms
    - Rooms can be fused together, segmented, or rearranged
    - Total first clear stats and last clear stats are saved and are viewable in the journal
    - Stats can be copied onto clipboard

###### Gameplay Tweaks
These are marked as using variations in the end-screen. Some of these can be overridden per-map with a Gameplay Tweaks Override Trigger.
- Grab Recast
    - Keybind which can act as a toggle grab keybind, or modify the grab key to be invert/toggle.
- Prevent Down Dash Redirects
    - Prevents down dashes from being redirected out of dashing down, aka prevent manual demos (except upward ones), by forcing them to be down or down diagonal
    - Does not affect demo keys.
- Neutral Drop Keybind

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



## 1.2 changelog:
- New Additions:
	- Death Manager:
		- !!
		
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

- Multi-room watchtower:
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