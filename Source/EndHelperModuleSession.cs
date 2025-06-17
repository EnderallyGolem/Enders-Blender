using System.Collections.Generic;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using Celeste.Mod.EndHelper.Entities.Misc;
using System.Collections.Specialized;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.GameplayTweaks;

namespace Celeste.Mod.EndHelper;

public class EndHelperModuleSession : EverestModuleSession
{
    // --- Room-Swap Stored Info ---
    // The code for this is freaking dumb because I added the dictionaries retroactively to allow for multiple sets of grids
    public Dictionary<string, bool> allowTriggerEffect { get; set; } = new Dictionary<string, bool> { };
    public Dictionary<string, int> roomSwapRow { get; set; } = new Dictionary<string, int> { };
    public Dictionary<string, int> roomSwapColumn { get; set; } = new Dictionary<string, int> { };
    public Dictionary<string, string> roomSwapPrefix { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, string> roomTemplatePrefix { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, float> roomTransitionTime { get; set; } = new Dictionary<string, float> { };
    public Dictionary<string, string> activateSoundEvent1 { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, string> activateSoundEvent2 { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, int> roomMapLevel { get; set; } = new Dictionary<string, int> { };

    public bool enableRoomSwapFuncs;

    // 2D list containing template room names.
    // The index matches up with the swap room locations.
    public Dictionary<string, List<List<string>>> roomSwapOrderList { get; set; } = new Dictionary<string, List<List<string>>> { };


    // -------------------------------
    // roomStatDicts. Stored here so it retains when resuming session.
    // I tried creating a class for these at first but ran into issues where session will just fail to retain between save & quits =/
    // To be honest there only really needs 1 OrderedDictionary but oh well. roomStatDict_customName will not be one because of difficulties making
    // it work in another OrderedDictionary
    public Dictionary<string, string> roomStatDict_customName = new Dictionary<string, string> { }; // <string, string>
    public OrderedDictionary roomStatDict_death = new OrderedDictionary { }; // <string, int>
    public OrderedDictionary roomStatDict_timer = new OrderedDictionary { }; // <string, long>
    public OrderedDictionary roomStatDict_strawberries = new OrderedDictionary { }; // <string, int>
    public OrderedDictionary roomStatDict_colorIndex = new OrderedDictionary { }; // <string, int>

    public Dictionary<string, bool> pauseTypeDict = new Dictionary<string, bool> { }; // Stores the type of pauses used

    public Dictionary<string, string> roomStatDict_fuseRoomRedirect = new Dictionary<string, string> { };  // <string, string?> room redirects. null if none.


    // Frames since respawn, frozen if paused or just respawned.
    public int framesSinceRespawn = 0;

    // If false, screen transitions do not move the player. Used in multi-room binos
    internal bool allowScreenTransitionMovement = true;


    // Gameplay Tweaks

    // Grabby. Here mostly so they are affected by states tbh
    public bool toggleifyEnabled = false;
    public bool GrabFakeTogglePressPressed = false;
    public bool ToggleGrabRanNothing = false;

    // Track if gameplay tweaks were used at any point. These track what tweaks are used and show them in the endscreen.
    // dashredirect grabrecast seemlessrespawn_minor seemlessrespawn_keepstate | backboost neutraldrop
    public Dictionary<string, bool> usedGameplayTweaks = new Dictionary<string, bool> {
        ["dashredirect"] = false,
        ["grabrecast"] = false,
        ["seemlessrespawn_minor"] = false,
        ["seemlessrespawn_keepstate"] = false,
        ["backboost"] = false,
        ["neutraldrop"] = false
    };

    // Override Gameplay Tweaks - With triggers
    public ConvertDemoEnum? GameplayTweaksOverride_ConvertDemo = null;
}