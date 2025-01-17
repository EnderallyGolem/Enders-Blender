using System.Collections.Generic;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using Celeste.Mod.EndHelper.Entities.Misc;
using System.Collections.Specialized;

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

    // 2D list containing template room names.
    // The index matches up with the swap room locations.
    public Dictionary<string, List<List<string>>> roomSwapOrderList { get; set; } = new Dictionary<string, List<List<string>>> { };

    // -------------------------------
    // Prevents the same entity from reappearing multiple times. Only used for multiroom watchtower for now.
    public List<int> entityPreventReappearIDs = new List<int> { };


    // -------------------------------
    // roomStatDicts. Stored here so it retains when resuming session.
    // I tried creating a class for these at first but ran into issues where session will just fail to retain between save & quits =/
    public OrderedDictionary roomStatDict_death = new OrderedDictionary { }; // <string, int>
    public OrderedDictionary roomStatDict_timer = new OrderedDictionary { }; // <string, long>
    public OrderedDictionary roomStatDict_strawberries = new OrderedDictionary { }; // <string, int>
    public OrderedDictionary roomStatDict_colorIndex = new OrderedDictionary { }; // <string, int>
}