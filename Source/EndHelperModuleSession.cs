using System.Collections.Generic;

namespace Celeste.Mod.EndHelper;

public class EndHelperModuleSession : EverestModuleSession
{
    // The code for this is freaking dumb because I added the dictionaries retroactively to allow for multiple sets of grids
    public Dictionary<string, bool> allowTriggerEffect { get; set; } = new Dictionary<string, bool> { };
    public Dictionary<string, int> roomSwapRow { get; set; } = new Dictionary<string, int> { };
    public Dictionary<string, int> roomSwapColumn { get; set; } = new Dictionary<string, int> { };
    public Dictionary<string, string> roomSwapPrefix { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, string> roomTemplatePrefix { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, float> roomTransitionTime { get; set; } = new Dictionary<string, float> { };
    public Dictionary<string, string> activateSoundEvent1 { get; set; } = new Dictionary<string, string> { };
    public Dictionary<string, string> activateSoundEvent2 { get; set; } = new Dictionary<string, string> { };

    //2D list containing template room names.
    //The index matches up with the swap room locations.
    public Dictionary<string, List<List<string>>> roomSwapOrderList { get; set; } = new Dictionary<string, List<List<string>>> { };

    public Dictionary<string, int> roomMapLevel { get; set; } = new Dictionary<string, int> { };

    public List<int> entityPreventReappearIDs = new List<int> { };
}