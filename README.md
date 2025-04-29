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
    - Stats can be copied onto clipboard

###### Gameplay Tweaks
These are marked as using variations in the end-screen. Some of these can be overridden per-map with a Gameplay Tweaks Override Trigger.
- Grab Recast
    - Keybind which can act as a toggle grab keybind, or modify the grab key to be invert/toggle.
- Prevent Down Dash Redirects
    - Prevents down dashes from being redirected out of dashing down, aka prevent manual demos (except upward ones), by forcing them to be down or down diagonal
    - Does not affect demo keys.

###### QOL Tweaks
- Quick-Retry Keybind
    - Instant Retry. Disabled when carrying a golden.
- Disable Quick-Restart Key

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
